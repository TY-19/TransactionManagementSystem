using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using System.Text;
using TMS.Application.Interfaces;
using TMS.Application.Models.Dtos;

namespace TMS.Application.Queries.TransactionClient.GetAllTransactionsClients;

public class GetAllTransactionsClientsQueryHandler(
    IDbConnectionOptions connectionOptions
    ) : IRequestHandler<GetAllTransactionsClientsQuery, IEnumerable<TransactionClientExportDto>>
{
    private readonly Dictionary<string, string> calculationRules = new()
    {
        { "calculateOffset", @$"DATENAME(tzoffset, TransactionDate) AS Offset" }
    };
    public async Task<IEnumerable<TransactionClientExportDto>> Handle(GetAllTransactionsClientsQuery request, CancellationToken cancellationToken)
    {
        var sql = @$"SELECT {GetRequestedColumnNames(request.RequestedColumns)}
			FROM Transactions
			JOIN Clients ON Transactions.ClientId = Clients.Id";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        return await dbConnection.QueryAsync<TransactionClientExportDto>(sql);
    }

    private string GetRequestedColumnNames(List<string> requestedColumns)
    {
        StringBuilder sb = new();
        foreach (var column in requestedColumns)
        {
            sb.Append(column + ", ");
        }
        sb.Remove(sb.Length - 2, 1);
        ApplyCalculationRules(sb);
        return sb.ToString();
    }

    private void ApplyCalculationRules(StringBuilder sb)
    {
        foreach (var rule in calculationRules)
            sb.Replace(rule.Key, rule.Value);
    }
}
