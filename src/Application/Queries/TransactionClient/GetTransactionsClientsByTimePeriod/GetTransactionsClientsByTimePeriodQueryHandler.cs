using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClientsByTimePeriod;

public class GetTransactionsClientsByTimePeriodQueryHandler(
    IDbConnectionOptions connectionOptions)
    : IRequestHandler<GetTransactionsClientsByTimePeriodQuery, IEnumerable<TransactionDto>>
{
    public async Task<IEnumerable<TransactionDto>> Handle(GetTransactionsClientsByTimePeriodQuery request, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            request.DateFrom,
            request.DateTo
        };
        string sql = @$"SELECT t.TransactionId, c.Name, c.Email, t.Amount, t.TransactionDate, c.Latitude, c.Longitude
            FROM Transactions t
            JOIN Clients c ON t.ClientId = c.Id
            WHERE TransactionDate >= @DateFrom AND TransactionDate <= @DateTo
            ORDER BY TransactionDate";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        return await dbConnection.QueryAsync<TransactionDto>(sql, parameters);
    }
}
