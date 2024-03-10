using ClosedXML.Excel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TMS.Application.Commands.Client.AddUpdateClient;
using TMS.Application.Commands.Transaction.AddUpdateTransaction;
using TMS.Application.Interfaces;
using TMS.Application.Models;
using TMS.Application.Queries.TransactionClient;
using TMS.Application.Queries.TransactionClient.GetAllTransactionsClients;

namespace TMS.Application.Services;

public class TransactionService(
    IMediator mediator,
    ICsvParser csvParser,
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occur when writing to the database");
            }
        }
        return new CustomResponse() { Succeeded = true };
    }

    public async Task<MemoryStream> ExportToExcelAsync(string? fields)
    {
        IEnumerable<TransactionClientPartDto> transactions = await mediator.Send(new GetAllTransactionsClientsQuery()
        {
            RequestedFields = fields
        });

        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Transactions");

        // TODO: Refactor to define columns depends on request parameter not on response type

        var t1 = transactions.First();

        Dictionary<int, Func<TransactionClientPartDto, string>> columnsProperties = new();
        int i = 1;
        if (t1.TransactionId != null)
        {
            worksheet.Cell(1, i).Value = "transaction_id";
            columnsProperties.Add(i, t => t.TransactionId!);
            i++;
        }
        if (t1.Name != null)
        {
            worksheet.Cell(1, i).Value = "name";
            columnsProperties.Add(i, t => t.Name!);
            i++;
        }
        if (t1.Email != null)
        {
            worksheet.Cell(1, i).Value = "email";
            columnsProperties.Add(i, t => t.Email!);
            i++;
        }
        if (t1.Amount != null)
        {
            worksheet.Cell(1, i).Value = "amount";
            columnsProperties.Add(i, t => t.Amount!.ToString());
            i++;
        }
        if (t1.TransactionDate != null)
        {
            worksheet.Cell(1, i).Value = "transaction_date";
            columnsProperties.Add(i, t => t.TransactionDate!.ToString());
            i++;
        }
        if (t1.Latitude != null)
        {
            worksheet.Cell(1, i).Value = "client_location_latitude";
            columnsProperties.Add(i, t => t.Latitude!.ToString());
            i++;
        }
        if (t1.Longitude != null)
        {
            worksheet.Cell(1, i).Value = "client_location_longitude";
            columnsProperties.Add(i, t => t.Longitude!.ToString());
        }

        // TODO: refactor to set decimal values as numbers instead of strings
        // TODO: format values in cells
        int j = 2;
        foreach (var transaction in transactions)
        {
            i = 1;
            while (i < columnsProperties.Count)
            {
                worksheet.Cell(j, i).Value = columnsProperties[i].Invoke(transaction);
                i++;
            }
            j++;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}

