using MediatR;
using System.Globalization;
using TMS.Application.Commands.Client.AddUpdateClient;
using TMS.Application.Commands.Transaction.AddUpdateTransaction;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Services;

public class TransactionService(
    ITimeZoneServiceFactory timeZoneServiceFactory,
    IMediator mediator
    ) : ITransactionService
{
    private readonly ITimeZoneService timeZoneService = timeZoneServiceFactory.GetTimeZoneService();
    public async Task<CustomResponse> ImportFromCsvStreamAsync(Stream stream)
    {
        var response = new CustomResponse();
        int currentRow = 1;
        int totalRowAffected = 0;
        using var reader = new StreamReader(stream);
        while(!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) continue;

            var values = line.Split(',');

            if(values.Length != 7 || values[0] == "transaction_id") continue;

            try
            {
                AddUpdateClientCommand clientCommand = ParseClientInfo(values);
                AddUpdateTransactionCommand transactionCommand = 
                    await ParseTransactionInfoAsync(values, clientCommand);
                await mediator.Send(clientCommand);
                await mediator.Send(transactionCommand);
                totalRowAffected++;
            }
            catch(Exception ex)
            {
                response.Errors.Add($"The row {currentRow} was not imported: {ex.Message}");
            }
            currentRow++;
        }

        return new CustomResponse()
        {
            Succeeded = true,
            Message = $"{totalRowAffected} rows were successfully imported."
        };
    }

    private static AddUpdateClientCommand ParseClientInfo(string[] values)
    {
        decimal latitude = decimal.Parse(values[5][1..], CultureInfo.InvariantCulture.NumberFormat);
        decimal longitude = decimal.Parse(values[6][..^1], CultureInfo.InvariantCulture.NumberFormat);

        return new AddUpdateClientCommand()
        {
            Name = values[1],
            Email = values[2],
            Latitude = latitude,
            Longitude = longitude
        };
    }

    private async Task<AddUpdateTransactionCommand> ParseTransactionInfoAsync(string[] values,
        AddUpdateClientCommand clientCommand)
    {
        decimal amount = decimal.Parse(values[3][1..], CultureInfo.InvariantCulture.NumberFormat);
        DateTimeOffset dateTimeOffset = await ParseDateAsync(
            values[4], clientCommand.Latitude, clientCommand.Longitude);

        return new AddUpdateTransactionCommand()
        {
            TransactionId = values[0],
            ClientEmail = clientCommand.Email,
            Amount = amount,
            TransactionDate = dateTimeOffset
        };
    }

    private async Task<DateTimeOffset> ParseDateAsync(string date, decimal latitude, decimal longitude)
    {
        var local = DateTime.ParseExact(date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture.DateTimeFormat);
        long timestamp = (long)(local - DateTime.UnixEpoch).TotalSeconds;
        int offsetIsSeconds = await timeZoneService.GetTimeZoneOffsetInSecondsAsync(
            latitude, longitude, timestamp);
        return new DateTimeOffset(local, TimeSpan.FromSeconds(offsetIsSeconds));
    }
}
