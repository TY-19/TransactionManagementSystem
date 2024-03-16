using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Helpers;

public class CsvParser(
    ITimeZoneService timeZoneService,
    ITimeZoneHelper timeZoneHelper
    ) : ICsvParser
{
    private List<string> Errors = [];

    public async Task<CustomResponse<TransactionImportDto>> ParseLineAsync(string? cssLine, CancellationToken cancellationToken)
    {
        Errors = [];
        if (cssLine.IsNullOrEmpty())
        {
            Errors.Add("String is empty");
            return new CustomResponse<TransactionImportDto>() { Succeeded = false, Errors = Errors };
        }

        string[] values = cssLine!.Split(',');
        if(values.Length > 0 && IsHeader(values[0]))
            return new CustomResponse<TransactionImportDto>() { Succeeded = false };

        if (values.Length != 7)
        {
            Errors.Add("String contains incorrect number of arguments");
            return new CustomResponse<TransactionImportDto>() { Succeeded = false, Errors = Errors };
        }

        TransactionImportDto transaction = new()
        {
            TransactionId = ParseTransactionId(values[0]) ?? "",
            Name = ParseName(values[1]) ?? "",
            Email = ParseEmail(values[2]) ?? "",
            Amount = ParseAmount(values[3]) ?? 0m,
            Latitude = ParseLatitude(values[5]) ?? 0m,
            Longitude = ParseLongitude(values[6]) ?? 0m
        };
        transaction.TransactionDate = await ParseDateAsync(values[4],
            transaction.Latitude, transaction.Longitude, cancellationToken) ?? DateTimeOffset.MinValue;

        return Errors.Count == 0
            ? new CustomResponse<TransactionImportDto>() { Succeeded = true, Payload = transaction }
            : new CustomResponse<TransactionImportDto>() { Succeeded = false, Errors = Errors };
    }

    private static bool IsHeader(string toCheck)
    {
        return toCheck.Replace(" ", "").Replace("_", "").Equals("transactionid", StringComparison.CurrentCultureIgnoreCase);
    }

    private string? ParseTransactionId(string toParse)
    {
        if (string.IsNullOrEmpty(toParse))
        {
            Errors.Add(GetError("transactionId", toParse));
            return null;
        }
        return toParse;
    }

    private string? ParseName(string toParse)
    {
        if (string.IsNullOrEmpty(toParse))
        {
            Errors.Add(GetError("name", toParse));
            return null;
        }
        return toParse;
    }

    private string? ParseEmail(string toParse)
    {
        if (string.IsNullOrEmpty(toParse))
        {
            Errors.Add(GetError("email", toParse));
            return null;
        }
        else if (!toParse.Contains('@'))
        {
            Errors.Add(GetError("email", toParse, $"{toParse} is not a valid email address"));
            return null;
        }
        return toParse;
    }

    private decimal? ParseLatitude(string toParse)
    {
        if (toParse.StartsWith('"'))
        {
            toParse = toParse[1..];
        }
        if (string.IsNullOrEmpty(toParse))
        {
            Errors.Add(GetError("latitude", toParse));
            return null;
        }
        else if (!decimal.TryParse(toParse[1..], CultureInfo.InvariantCulture.NumberFormat, out decimal lat))
        {
            Errors.Add(GetError("latitude", toParse, $"{toParse} is not a valid decimal value"));
            return null;
        }
        else if (lat < -90 || lat > 90)
        {
            Errors.Add(GetError("latitude", toParse, $"Latitude must be between -90 and 90 degrees"));
            return null;
        }
        else
        {
            return lat;
        }
    }

    private decimal? ParseLongitude(string toParse)
    {
        if (toParse.EndsWith('"'))
        {
            toParse = toParse[..^1];
        }
        if (string.IsNullOrEmpty(toParse))
        {
            Errors.Add(GetError("longitude", toParse));
            return null;
        }
        else if (!decimal.TryParse(toParse, CultureInfo.InvariantCulture.NumberFormat, out decimal lon))
        {
            Errors.Add(GetError("longitude", toParse, $"{toParse} is not a valid decimal value"));
            return null;
        }
        else if (lon < -180 || lon > 180)
        {
            Errors.Add(GetError("longitude", toParse, $"Longitude must be between -180 and 180 degrees"));
            return null;
        }
        else
        {
            return lon;
        }
    }
    private decimal? ParseAmount(string toParse)
    {
        if (toParse.StartsWith('$'))
        {
            toParse = toParse[1..];
        }
        else
        {
            Errors.Add(GetError("amount", toParse, $"Only amount in US dollars are accepted. Use the dollar sign to indicate currency. E.g. $100.00"));
            return null;
        }

        if (decimal.TryParse(toParse, CultureInfo.InvariantCulture.NumberFormat, out decimal am))
        {
            return am;
        }
        else
        {
            Errors.Add(GetError("amount", toParse, $"{toParse} is not a valid decimal value"));
            return null;
        }
    }

    private async Task<DateTimeOffset?> ParseDateAsync(string toParse, decimal? latitude,
        decimal? longitude, CancellationToken cancellationToken)
    {
        if (latitude == null || longitude == null)
        {
            Errors.Add(GetError("transaction_date", toParse, "One or both coordinates was not provided. Cannot determine a time zone."));
            return null;
        }

        DateTime local;
        try
        {
            local = DateTime.ParseExact(toParse, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture.DateTimeFormat);
        }
        catch (ArgumentNullException)
        {
            Errors.Add(GetError("transaction_date", toParse));
            return null;
        }
        catch (FormatException)
        {
            Errors.Add(GetError("transaction_date", toParse, $"The date is not in valid format. Dates should be provided in the following format \"yyyy-MM-dd HH:mm:ss\""));
            return null;
        }

        try
        {
            var timeZoneDetails = await timeZoneService.GetTimeZoneByCoordinatesAsync(
                latitude.Value, longitude.Value, cancellationToken);

            if (timeZoneDetails.Succeeded && timeZoneDetails.Payload != null)
            {
                int offsetInSeconds = timeZoneHelper.GetOffsetInSeconds(local, timeZoneDetails.Payload);
                return new DateTimeOffset(local, TimeSpan.FromSeconds(offsetInSeconds));
            }
            else
            {
                Errors.Add(GetError("transaction_date", toParse, "External API cannot resolve a timezone"));
                return null;
            }
        }
        catch (Exception ex)
        {
            Errors.Add(GetError("transaction_date", toParse, $"Error happens while determining time zone. Error details: {ex.Message}"));
            return null;
        }
    }

    private static string GetError(string propertyName, string property, string? message = null)
    {
        return $"Cannot parse '{propertyName}' with value '{property}. {(message ?? "")}'";
    }
}
