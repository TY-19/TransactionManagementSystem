using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TMS.Application.Commands.Client.AddUpdateClient;
using TMS.Application.Commands.Transaction.AddUpdateTransaction;
using TMS.Application.Interfaces;
using TMS.Application.Models;

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
}
