using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;
using TMS.Application.Interfaces;
using TMS.Application.Models;
using TMS.Domain.Enums;

namespace TMS.Application.Helpers;

public class CsvHelper(
    ITimeZoneService _timeZoneService,
    ITimeZoneHelper _timeZoneHelper,
    ITransactionPropertyHelper _propertyManager
    ) : ICsvHelper
{
    private static class Messages
    {
        public const string StringEmpty = "String is empty.";
        public const string StringHeader = "String is a header.";
        public const string IncorrectArgumentsNumber = "String contains an incorrect number of arguments.";
        public const string InvalidEmail = "The email address is not valid.";
        public const string InvalidCurrency = "Only amounts in US dollars are accepted. Use the dollar sign to indicate currency. Example: $100.00.";
        public const string InvalidDecimal = "The value is not a valid decimal.";
        public const string LatitudeOutOfRange = "Latitude must be between -90 and 90 degrees.";
        public const string LongitudeOutOfRange = "Longitude must be between -180 and 180 degrees.";
        public const string InvalidDateFormat = "The date is not in a valid format. Dates should be provided in the following format: 'yyyy-MM-dd HH:mm:ss'.";
        public const string TimezoneResolutionError = "The external API cannot resolve a timezone";
        public const string TimezoneApiError = "An error occurred while determining the time zone. Error details:";
    }

    private List<string> _errors = [];

    /// <inheritdoc cref="ICsvHelper.ParseLineAsync(string?, CancellationToken)"/>
    public async Task<OperationResult<TransactionDto>> ParseLineAsync(
        string? csvLine, CancellationToken cancellationToken)
    {
        _errors = [];
        if (csvLine.IsNullOrEmpty())
        {
            return new OperationResult<TransactionDto>(true, Messages.StringEmpty);
        }

        string[] values = csvLine!.Split(',');
        if (values.Length > 0 && IsHeader(values[0].Trim()))
        {
            return new OperationResult<TransactionDto>(true, Messages.StringHeader);
        }
        if (values.Length != 7)
        {
            return new OperationResult<TransactionDto>(false, Messages.IncorrectArgumentsNumber);
        }

        TransactionDto transaction = new();
        if (TryParseTransactionId(values[0].Trim(), out string transactionId))
        {
            transaction.TransactionId = transactionId;
        }
        if (TryParseName(values[1].Trim(), out string name))
        {
            transaction.Name = name;
        }
        if (TryParseEmail(values[2].Trim(), out string email))
        {
            transaction.Email = email;
        }
        if (TryParseAmount(values[3].Trim(), out decimal amount))
        {
            transaction.Amount = amount;
        }
        if (TryParseLatitude(values[5].Trim(), out decimal latitude))
        {
            transaction.Latitude = latitude;
        }
        if (TryParseLongitude(values[6].Trim(), out decimal longitude))
        {
            transaction.Longitude = longitude;
        }
        DateTimeOffset? date = await ParseDateAsync(values[4].Trim(),
            transaction.Latitude, transaction.Longitude, cancellationToken);
        if (date.HasValue)
        {
            transaction.TransactionDate = date.Value;
        }
        return _errors.Count == 0
            ? new OperationResult<TransactionDto>(true, transaction)
            : new OperationResult<TransactionDto>(false, ErrorsToString());
    }
    private bool IsHeader(string toCheck)
    {
        return _propertyManager.GetProperty(toCheck) != null;
    }
    private bool TryParseTransactionId(string toParse, out string transactionId)
    {
        transactionId = null!;
        if (string.IsNullOrEmpty(toParse))
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.TransactionId, toParse));
            return false;
        }
        transactionId = toParse;
        return true;
    }
    private bool TryParseName(string toParse, out string name)
    {
        name = null!;
        if (string.IsNullOrEmpty(toParse))
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Name, toParse));
            return false;
        }
        name = toParse;
        return true;
    }
    private bool TryParseEmail(string toParse, out string email)
    {
        email = null!;
        if (string.IsNullOrEmpty(toParse) || !toParse.Contains('@'))
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Email, toParse, Messages.InvalidEmail));
            return false;
        }
        email = toParse;
        return true;
    }
    private bool TryParseAmount(string toParse, out decimal amount)
    {
        if (toParse.StartsWith('$'))
        {
            toParse = toParse[1..];
        }
        else
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Amount, toParse, Messages.InvalidCurrency));
            amount = 0m;
            return false;
        }

        if (decimal.TryParse(toParse, CultureInfo.InvariantCulture.NumberFormat, out amount))
        {
            return true;
        }
        else
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Amount, toParse, Messages.InvalidDecimal));
            return false;
        }
    }
    private bool TryParseLatitude(string toParse, out decimal latitude)
    {
        if (toParse.StartsWith('"'))
        {
            toParse = toParse[1..];
        }

        if (string.IsNullOrEmpty(toParse))
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Latitude, toParse));
            latitude = 0m;
            return false;
        }
        else if (!decimal.TryParse(toParse, CultureInfo.InvariantCulture.NumberFormat, out latitude))
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Latitude, toParse, Messages.InvalidDecimal));
            return false;
        }
        else if (latitude < -90 || latitude > 90)
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Latitude, toParse, Messages.LatitudeOutOfRange));
            return false;
        }
        else
        {
            return true;
        }
    }
    private bool TryParseLongitude(string toParse, out decimal longitude)
    {
        if (toParse.EndsWith('"'))
        {
            toParse = toParse[..^1];
        }
        if (string.IsNullOrEmpty(toParse))
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Longitude, toParse));
            longitude = 0m;
            return false;
        }
        else if (!decimal.TryParse(toParse, CultureInfo.InvariantCulture.NumberFormat, out longitude))
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Longitude, toParse, Messages.InvalidDecimal));
            return false;
        }
        else if (longitude < -180 || longitude > 180)
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.Longitude, toParse, Messages.LongitudeOutOfRange));
            return false;
        }
        else
        {
            return true;
        }
    }
    private async Task<DateTimeOffset?> ParseDateAsync(string toParse, decimal latitude,
        decimal longitude, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(toParse, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture.DateTimeFormat,
            DateTimeStyles.None, out var localDateTime))
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.TransactionDate, toParse,
                Messages.InvalidDateFormat));
            return null;
        }

        try
        {
            OperationResult<TimeZoneDetails> timeZoneDetails = await _timeZoneService
                .GetTimeZoneByCoordinatesAsync(latitude, longitude, cancellationToken);

            if (timeZoneDetails.Succeeded && timeZoneDetails.Payload != null)
            {
                int offsetInSeconds = _timeZoneHelper.GetOffsetInSeconds(localDateTime, timeZoneDetails.Payload);
                return new DateTimeOffset(localDateTime, TimeSpan.FromSeconds(offsetInSeconds));
            }
            else
            {
                _errors.Add(BuildErrorMessage(TransactionPropertyName.TransactionDate, toParse,
                    Messages.TimezoneResolutionError));
                return null;
            }
        }
        catch (Exception ex)
        {
            _errors.Add(BuildErrorMessage(TransactionPropertyName.TransactionDate, toParse,
                $"{Messages.TimezoneApiError} {ex.Message}"));
            return null;
        }
    }
    private string BuildErrorMessage(TransactionPropertyName property, string value,
        string? details = null)
    {
        string? propName = _propertyManager.GetDisplayedName(property);
        return $"Cannot parse '{propName}' with value '{value}'. {(details ?? "")} ";
    }
    private string ErrorsToString()
    {
        StringBuilder sb = new();
        foreach (string error in _errors)
        {
            sb.Append(error);
        }
        return sb.ToString();
    }
}
