using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TMS.Application.Commands.Client.AddUpdateClient;
using TMS.Application.Commands.Transaction.AddUpdateTransaction;
using TMS.Application.Interfaces;
using TMS.Application.Models;
using TMS.Application.Queries.TransactionClient.GetTransactionsClients;
using TMS.Domain.Enums;

namespace TMS.Application.Services;

public class TransactionService(
    IMediator mediator,
    ICsvHelper csvParser,
    IXlsxHelper xlsxHelper,
    ITimeZoneHelper timeZoneHelper,
    ITransactionPropertyHelper propertyManager,
    ILogger<TransactionService> logger
    ) : ITransactionService
{
    /// <inheritdoc cref="ITransactionService.ImportFromCsvAsync(Stream, CancellationToken)"/>
    public async Task<OperationResult> ImportFromCsvAsync(Stream stream, CancellationToken cancellationToken)
    {
        OperationResult response = new();
        int row = 0;
        int imported = 0;
        int skipped = 0;

        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            row++;
            string? line = await reader.ReadLineAsync(cancellationToken);
            OperationResult<TransactionDto> parsedResponse =
                await csvParser.ParseLineAsync(line, cancellationToken);
            if (!parsedResponse.Succeeded)
            {
                logger.LogInformation("{line} was not imported: {message}.", line, parsedResponse.Message);
                response.Errors.Add($"Row {row} was not imported: {parsedResponse.Message}");
                continue;
            }
            if (parsedResponse.Payload == null)
            {
                skipped++;
                continue;
            }
            try
            {
                TransactionDto transaction = parsedResponse.Payload;
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

                imported++;
            }
            catch (ValidationException ex)
            {
                string error = string.Join(", ", ex.Errors.Select(e => e.ErrorMessage));
                response.Errors.Add($"Row {row} was not imported. Validation error: {error}");
                logger.LogInformation("{line} was not imported. Validation error have occurred: {error}", line, error);
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Row {row} was not imported. An error occurred when importing.");
                logger.LogError(ex, "An unknown error occur when writing to the database.");
            }
        }

        response.Succeeded = true;
        response.Message = $"{row} rows analyzed: {imported} imported, {skipped} skipped, {response.Errors.Count} failed to import";
        return response;
    }

    /// <inheritdoc cref="ITransactionService.GetTransactionsAsync(string, string?, bool, TimeZoneDetails?, DateOnly?, DateOnly?, CancellationToken)"/>
    public async Task<IEnumerable<TransactionExportDto>> GetTransactionsAsync(
        string columns, string? sortBy, bool sortAsc, TimeZoneDetails? timeZoneDetails,
        DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken)
    {
        List<TransactionPropertyName> requestedProperties = propertyManager
            .GetPropertiesTypes(columns.Split(','));

        return await GetTransactionsAsync(requestedProperties, sortBy, sortAsc, timeZoneDetails,
            startDate, endDate, cancellationToken);
    }

    private async Task<IEnumerable<TransactionExportDto>> GetTransactionsAsync(
        List<TransactionPropertyName> properties, string? sortBy, bool sortAsc,
        TimeZoneDetails? timeZoneDetails, DateOnly? startDate, DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        GetTransactionsClientsQuery query = PrepareQuery(properties, sortBy, sortAsc,
            timeZoneDetails, startDate, endDate);

        IEnumerable<TransactionExportDto> transactions = await mediator.Send(query, cancellationToken);

        if (timeZoneDetails != null)
        {
            transactions = ApplyDstRules(transactions, timeZoneDetails, startDate, endDate, cancellationToken);
        }
        return transactions;
    }

    /// <inheritdoc cref="ITransactionService.ExportToExcelAsync(string, string?, bool, TimeZoneDetails?, DateFilterParameters?, DateFilterParameters?, CancellationToken)"/>
    public async Task<MemoryStream> ExportToExcelAsync(string columns, string? sortBy, bool sortAsc,
        TimeZoneDetails? timeZoneDetails, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken)
    {
        List<TransactionPropertyName> requestedProperties = propertyManager
            .GetPropertiesTypes(columns.Split(','));

        IEnumerable<TransactionExportDto> transactions = await GetTransactionsAsync(
            requestedProperties, sortBy, sortAsc, timeZoneDetails, startDate, endDate, cancellationToken);

        return xlsxHelper.WriteTransactionsIntoXlsxFile(transactions, requestedProperties, cancellationToken);
    }

    /// <inheritdoc cref="ITransactionService.GetTransactionsFileName(TimeZoneDetails?, DateFilterParameters?, DateFilterParameters?)"/>
    public string GetTransactionsFileName(TimeZoneDetails? timeZoneDetails, DateOnly? startDate, DateOnly? endDate)
    {
        string name = "transactions";
        if (startDate.HasValue && endDate.HasValue)
        {
            name += $"_{startDate.Value.Year}_{startDate.Value.Month}_{startDate.Value.Day}" +
                $"-{endDate.Value.Year}_{endDate.Value.Month}_{endDate.Value.Day}";
        }
        else if (startDate != null)
        {
            name += $"_after_{startDate.Value.Year}_{startDate.Value.Month}_{startDate.Value.Day}";
        }
        else if (endDate != null)
        {
            name += $"_before_{endDate.Value.Year}_{endDate.Value.Month}_{endDate.Value.Day}";
        }

        name += timeZoneDetails == null ? "_clients_time_zones" : $"_{timeZoneDetails.TimeZoneName}_time_zone";
        name += xlsxHelper.ExcelFileExtension;

        return name;
    }

    /// <inheritdoc cref="ITransactionService.GetExcelFileMimeType()"/>
    public string GetExcelFileMimeType()
    {
        return xlsxHelper.ExcelMimeType;
    }

    private List<TransactionExportDto> ApplyDstRules(IEnumerable<TransactionExportDto> transactions,
        TimeZoneDetails userTimeZone, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken)
    {
        DateTimeOffset? start = GetLimitWithOffset(startDate, userTimeZone, true);
        DateTimeOffset? end = GetLimitWithOffset(endDate, userTimeZone, false);

        List<TransactionExportDto> transactionList = transactions.ToList();
        for (int i = transactionList.Count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (transactionList[i].TransactionDate == null)
            {
                continue;
            }
            transactionList[i].TransactionDate = timeZoneHelper.GetDateTime(
                transactionList[i].TransactionDate!.Value, userTimeZone);

            if (start.HasValue && (transactionList[i].TransactionDate < start
                    || transactionList[i].TransactionDate > end))
            {
                transactionList.RemoveAt(i);
            }

            transactionList[i].Offset = timeZoneHelper.GetReadableOffset(
                transactionList[i].TransactionDate!.Value, userTimeZone);
        }

        return transactionList;
    }

    private DateTimeOffset? GetLimitWithOffset(DateOnly? date, TimeZoneDetails userTimeZone, bool isStartDate)
    {
        if (date == null)
        {
            return null;
        }
        TimeOnly time = isStartDate ? new TimeOnly(0, 0, 0) : new TimeOnly(23, 59, 59, 999);
        var dateTimeOffset = new DateTimeOffset(date.Value, time, TimeSpan.Zero);
        dateTimeOffset.AddMinutes(timeZoneHelper.GetOffsetInSeconds(dateTimeOffset, userTimeZone));
        return dateTimeOffset;
    }

    private GetTransactionsClientsQuery PrepareQuery(List<TransactionPropertyName> requestedProperties,
        string? sortBy, bool sortAsc, TimeZoneDetails? timeZoneDetails, DateOnly? startDate, DateOnly? endDate
        )
    {
        IEnumerable<string> columns = GetDatabaseColumnNames(requestedProperties, timeZoneDetails);
        TransactionPropertyName? sortProperty = propertyManager.GetProperty(sortBy);
        string? sortColumn = sortProperty == null ? null
            : propertyManager.GetDatabaseColumnName(sortProperty.Value);

        int stdOffset = timeZoneDetails?.StandardUtcOffsetSeconds ?? 0;
        int? dstOffset = timeZoneDetails != null && timeZoneDetails.HasDayLightSaving
            ? timeZoneDetails.DstOffsetToUtcSeconds!.Value
            : null;

        int startOffset = ChooseOffsetToUse(stdOffset, dstOffset, true);
        int endOffset = ChooseOffsetToUse(stdOffset, dstOffset, false);
        return new GetTransactionsClientsQuery()
        {
            ColumnNames = columns,
            SortBy = sortColumn,
            SortAsc = sortAsc,
            UseUserTimeZone = timeZoneDetails != null,
            StartDate = startDate,
            EndDate = endDate,
            StartDateOffset = timeZoneHelper.GetReadableOffset(startOffset),
            EndDateOffset = timeZoneHelper.GetReadableOffset(endOffset),
        };
    }

    private List<string> GetDatabaseColumnNames(List<TransactionPropertyName> propertyNames,
        TimeZoneDetails? timeZoneDetails)
    {
        List<string> dbColumns = propertyManager.GetDatabaseColumnNames(propertyNames);
        if (timeZoneDetails != null)
        {
            // Case when offset will be calculated for the current user time zone.
            // No need to obtain from the database offset in the clients' time zones.
            string? offsetName = propertyManager.GetDatabaseColumnName(TransactionPropertyName.Offset);
            if (offsetName != null && dbColumns.Contains(offsetName))
            {
                dbColumns.Remove(offsetName);
            }
        }
        return dbColumns;
    }

    private static int ChooseOffsetToUse(int stdOffset, int? dstOffset, bool isStartDate)
    {
        if (dstOffset == null)
        {
            return stdOffset;
        }
        else
        {
            // There are edge cases when dstOffset is smaller than standard offset
            // so comparison is necessary. For example 'Europe/Dublin' time zone has Winter time
            // that external API treats as negative 1 hour of daylight saving.
            return isStartDate
                ? Math.Max(stdOffset, dstOffset.Value)
                : Math.Min(stdOffset, dstOffset.Value);
        }
    }
}
