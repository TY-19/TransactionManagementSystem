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

        string sql = @$"
            MERGE INTO Transactions
            USING (VALUES (@TransactionId, @ClientEmail, @Amount, @TransactionDate))
                AS source (TransactionId, ClientEmail, Amount, TransactionDate)
            ON Transactions.TransactionId = source.TransactionId
            WHEN MATCHED THEN
                UPDATE SET
                    ClientId = (SELECT Id FROM Clients WHERE Email = source.ClientEmail),
                    TransactionDate = source.TransactionDate,
                    Amount = source.Amount
            WHEN NOT MATCHED THEN
                INSERT (TransactionId, ClientId, Amount, TransactionDate)
                VALUES (
                    source.TransactionId,
                    (SELECT Id FROM Clients WHERE Email = source.ClientEmail),
                    source.Amount,
                    source.TransactionDate);
        ";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        await dbConnection.ExecuteAsync(sql, parameters);
    }
}
