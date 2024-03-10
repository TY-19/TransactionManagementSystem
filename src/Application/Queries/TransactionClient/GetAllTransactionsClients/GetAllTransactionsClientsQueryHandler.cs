using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using System.Text;
using TMS.Application.Interfaces;

namespace TMS.Application.Queries.TransactionClient.GetAllTransactionsClients;

public class GetAllTransactionsClientsQueryHandler(
    IDbConnectionOptions connectionOptions
    ) : IRequestHandler<GetAllTransactionsClientsQuery, IEnumerable<TransactionClientPartDto>>
{
    public async Task<IEnumerable<TransactionClientPartDto>> Handle(GetAllTransactionsClientsQuery request, CancellationToken cancellationToken)
    {
        var sql = @$"SELECT {GetRequestedFieldNames(request.RequestedFields)}
            FROM Transactions t
            JOIN Clients c ON t.ClientId = c.Id";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        return await dbConnection.QueryAsync<TransactionClientPartDto>(sql);
    }

    private static string GetRequestedFieldNames(string? requested)
    {
        StringBuilder sb = new();
        var fields = requested.ToLower().Replace("_", "").Split(",");

        foreach (var field in fields)
        {
            switch (field)
            {
                case "id":
                    sb.Append("t.TransactionId, ");
                    break;
                case "transactionid":
                    sb.Append("t.TransactionId, ");
                    break;
                case "name":
                    sb.Append("c.Name, ");
                    break;
                case "email":
                    sb.Append("c.Email, ");
                    break;
                case "amount":
                    sb.Append("t.Amount, ");
                    break;
                case "transactiondate":
                    sb.Append("t.TransactionDate, ");
                    break;
                case "latitude":
                    sb.Append("c.Latitude, ");
                    break;
                case "longitude":
                    sb.Append("c.Longitude, ");
                    break;
            }
        }
        sb.Remove(sb.Length - 2, 1);
        return sb.ToString();
    }
}
