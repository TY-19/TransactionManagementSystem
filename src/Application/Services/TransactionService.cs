using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TMS.Application.Commands.Client.AddUpdateClient;
using TMS.Application.Commands.Transaction.AddUpdateTransaction;
using TMS.Application.Interfaces;
using TMS.Application.Models;
using TMS.Application.Queries.TransactionClient.GetTransactionsClients;

namespace TMS.Application.Services;

public class TransactionService(
    IMediator mediator,
    ICsvParser csvParser,
    IXlsxHelper xlsxHelper,
    ITimeZoneHelper timeZoneHelper,
    ITransactionPropertyManager propertyManager,
    ILogger<TransactionService> logger
    ) : ITransactionService
{
    public async Task<CustomResponse> ImportFromCsvAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            var parsedResponse = await csvParser.TryParseLineAsync(line, cancellationToken);
            var transaction = parsedResponse.Payload;
            if (!parsedResponse.Succeeded || transaction == null)
            {
                logger.LogWarning("Parsing has failed with message: {message}. Errors {@errors}", parsedResponse.Message, parsedResponse.Errors);
                continue;
            }

            try
            {
                await mediator.Send(new AddUpdateClientCommand()
                {
                    Name = transaction.Name,
                    Email = transaction.Email,
                    Latitude = transaction.Latitude,
                    Longitude = transaction.Longitude
                }, cancellationToken);

                await mediator.Send(new AddUpdateTransactionCommand()
                {
                    TransactionId = transaction.TransactionId,
                    ClientEmail = transaction.Email,
                    Amount = transaction.Amount,
                    TransactionDate = transaction.TransactionDate
                }, cancellationToken);
            }
            catch (ValidationException ex)
            {
                logger.LogError(ex, "One or more validation errors has occurred");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occur when writing to the database");
                throw;
            }
        }
        return new CustomResponse() { Succeeded = true };
    }

    public async Task<MemoryStream> ExportToExcelAsync(string columns, string sortBy, bool sortAsc, TimeZoneDetails? timeZoneDetails,
        DateFilterParameters? startDate, DateFilterParameters? endDate, CancellationToken cancellationToken)
    {
        var requestedColumns = propertyManager.GetPropertiesTypes(columns.Split(','));
        var sortColumn = propertyManager.GetProperty(sortBy) ?? requestedColumns[0];

        IEnumerable<TransactionExportDto> transactions = await mediator.Send(
            new GetTransactionsClientsQuery()
            {
                RequestedColumns = propertyManager.GetDatabaseColumnNames(requestedColumns),
                SortBy = propertyManager.GetDatabaseColumnName(sortColumn)!,
                SortAsc = sortAsc,
                TimeZone = timeZoneDetails,
                StartDate = startDate,
                EndDate = endDate
            }, cancellationToken);
        transactions = ApplyDstRules(transactions, timeZoneDetails, startDate, endDate, cancellationToken);

        return xlsxHelper.WriteTransactionsIntoXlsxFile(transactions, requestedColumns, cancellationToken);
    }

    public string GetTransactionsFileName(TimeZoneDetails? timeZoneDetails, DateFilterParameters? startDate,
        DateFilterParameters? endDate)
    {
        string name = "transactions";
        if (startDate != null && endDate != null)
            name += $"_{startDate.Year}_{startDate.Month}_{startDate.Day}-{endDate.Year}_{endDate.Month}_{endDate.Day}";
        else if (startDate != null)
            name += $"_after_{startDate.Year}_{startDate.Month}_{startDate.Day}";
        else if (endDate != null)
            name += $"_before_{endDate.Year}_{endDate.Month}_{endDate.Day}";

        name += timeZoneDetails == null ? "_clients_time" : $"_{timeZoneDetails.TimeZone}";
        name += xlsxHelper.FileExtension;

        return name;
    }

    public string GetFileMimeType() => xlsxHelper.ExcelMimeType;

    private IEnumerable<TransactionExportDto> ApplyDstRules(IEnumerable<TransactionExportDto> transactions,
        TimeZoneDetails? userTimeZone, DateFilterParameters? startDate, DateFilterParameters? endDate,
        CancellationToken cancellationToken)
    {
        if (userTimeZone == null)
            return transactions;

        DateTimeOffset? start = startDate?.AsOffset();
        if (startDate != null)
            start!.Value.AddMinutes(timeZoneHelper.GetOffsetInSeconds(start.Value, userTimeZone));
        DateTimeOffset? end = endDate?.AsOffset();
        if (endDate != null)
            end!.Value.AddMinutes(timeZoneHelper.GetOffsetInSeconds(end.Value, userTimeZone));

        List<TransactionExportDto> transactionList = transactions.ToList();
        for (int i = transactionList.Count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (transactionList[i].TransactionDate == null)
                continue;

            transactionList[i].TransactionDate = timeZoneHelper.GetDateTime(
                transactionList[i].TransactionDate!.Value, userTimeZone);

            if (start.HasValue
                && (transactionList[i].TransactionDate < start
                    || transactionList[i].TransactionDate > end))
            {
                transactionList.RemoveAt(i);
            }

            transactionList[i].Offset = timeZoneHelper.GetReadableOffset(
                transactionList[i].TransactionDate!.Value, userTimeZone);
        }

        return transactionList;
    }
}

