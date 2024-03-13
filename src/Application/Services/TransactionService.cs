using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TMS.Application.Commands.Client.AddUpdateClient;
using TMS.Application.Commands.Transaction.AddUpdateTransaction;
using TMS.Application.Interfaces;
using TMS.Application.Models;
using TMS.Application.Models.Dtos;
using TMS.Application.Queries.TransactionClient.GetTransactionsClients;

namespace TMS.Application.Services;

public class TransactionService(
    IMediator mediator,
    ICsvParser csvParser,
    IXlsxHelper xlsxHelper,
    ITransactionPropertyManager propertyManager,
    ILogger<TransactionService> logger
    ) : ITransactionService
{
    public async Task<CustomResponse> ImportFromCsvAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            var parsedResponse = await csvParser.TryParseLineAsync(line);
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
                });

                await mediator.Send(new AddUpdateTransactionCommand()
                {
                    TransactionId = transaction.TransactionId,
                    ClientEmail = transaction.Email,
                    Amount = transaction.Amount,
                    TransactionDate = transaction.TransactionDate
                });
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

    public async Task<MemoryStream> ExportToExcelAsync(string fields, string sortBy, bool sortAsc, int? userOffset,
        DateFilterParameters? startDate, DateFilterParameters? endDate)
    {
        var requestedColumns = propertyManager.GetPropertiesTypes(fields.Split(','));
        var sortColumn = propertyManager.GetProperty(sortBy) ?? requestedColumns[0];

        IEnumerable<TransactionClientExportDto> transactions = await mediator.Send(
            new GetTransactionsClientsQuery()
            {
                RequestedColumns = propertyManager.GetDatabaseColumnNames(requestedColumns),
                SortBy = propertyManager.GetDatabaseColumnName(sortColumn)!,
                SortAsc = sortAsc,
                UserTimeZoneOffset = GetFormattedOffset(userOffset),
                StartDate = startDate,
                EndDate = endDate
            });

        return xlsxHelper.WriteTransactionsIntoXlsxFile(transactions, requestedColumns, userOffset);
    }

    public string GetExcelFileName(int? userOffset, DateFilterParameters? startDate,
        DateFilterParameters? endDate)
    {
        string name = "transactions";
        if (startDate != null && endDate != null)
            name += $"_{startDate.Year}_{startDate.Month}_{startDate.Day}-{endDate.Year}_{endDate.Month}_{endDate.Day}";
        else if (startDate != null)
            name += $"_after_{startDate.Year}_{startDate.Month}_{startDate.Day}";
        else if (endDate != null)
            name += $"_before_{endDate.Year}_{endDate.Month}_{endDate.Day}";

        if (userOffset == null) name += "_clients_time";
        else name += $"_UTC{GetFormattedOffset(userOffset)}";

        name += ".xlsx";
        return name;
    }

    private static string? GetFormattedOffset(int? offset)
    {
        if (offset == null) return null;

        char sign = offset >= 0 ? '+' : '-';
        offset = Math.Abs(Math.Max(Math.Min(offset.Value, 720), -720));
        return $"{sign}{(offset / 60):D2}:{(offset % 60):D2}";
    }
}

