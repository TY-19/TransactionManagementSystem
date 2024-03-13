using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using TMS.Application.Interfaces;

namespace TMS.Application.Commands.Transaction.AddUpdateTransaction;

public class AddUpdateTransactionCommandHandler(
    IDbConnectionOptions connectionOptions
    ) : IRequestHandler<AddUpdateTransactionCommand>
{
    public async Task Handle(AddUpdateTransactionCommand command, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            command.TransactionId,
            command.ClientEmail,
            command.Amount,
            command.TransactionDate
        };

        var sql = @$"
            IF EXISTS (SELECT 1 FROM Transactions WHERE TransactionId = @TransactionId)
            BEGIN
                UPDATE Transactions
                SET ClientId = (SELECT Id FROM Clients WHERE Email = @ClientEmail),
                    TransactionDate = @TransactionDate,
                    Amount = @Amount
                WHERE TransactionId = @TransactionId;
            END
            ELSE
            BEGIN
                INSERT INTO Transactions (TransactionId, ClientId, TransactionDate, Amount)
                VALUES (
                    @TransactionId, 
                    (SELECT Id FROM Clients WHERE Email = @ClientEmail),
                    @TransactionDate,
                    @Amount);
            END
        ";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        await dbConnection.ExecuteAsync(sql, parameters);
    }
}
