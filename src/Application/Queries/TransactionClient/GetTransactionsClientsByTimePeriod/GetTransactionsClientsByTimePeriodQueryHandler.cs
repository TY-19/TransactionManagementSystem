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
            DateFrom = request.DateFrom.ToString("yyyy-MM-dd"),
            DateTo = request.DateTo.ToString("yyyy-MM-dd")
        };
        string sql = @$"
            SELECT t.TransactionId, c.Name, c.Email, t.Amount, t.TransactionDate, c.Latitude, c.Longitude
            FROM Transactions t
            JOIN Clients c ON t.ClientId = c.Id
            WHERE Convert(date, t.TransactionDate) BETWEEN @DateFrom AND @DateTo
            ORDER BY TransactionDate";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        return await dbConnection.QueryAsync<TransactionDto>(sql, parameters);
    }
}
