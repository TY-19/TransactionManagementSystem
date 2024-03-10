using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using System.Text;
using TMS.Application.Common;
using TMS.Application.Interfaces;
using TMS.Application.Models.Dtos;

namespace TMS.Application.Queries.TransactionClient.GetAllTransactionsClients;

public class GetAllTransactionsClientsQueryHandler(
    IDbConnectionOptions connectionOptions
    ) : IRequestHandler<GetAllTransactionsClientsQuery, IEnumerable<TransactionClientExportDto>>
{
    private readonly string queryOffset = @$"DATENAME(tzoffset, TransactionDate) AS Offset";
    public async Task<IEnumerable<TransactionClientExportDto>> Handle(GetAllTransactionsClientsQuery request, CancellationToken cancellationToken)
    {
        var sql = @$"SELECT {GetRequestedFieldNames(request.RequestedColumns)}
			FROM Transactions
			JOIN Clients ON Transactions.ClientId = Clients.Id";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        return await dbConnection.QueryAsync<TransactionClientExportDto>(sql);
    }

    private string GetRequestedFieldNames(List<PropertyNames> requestedColumns)
    {
        StringBuilder sb = new();
        foreach (var column in requestedColumns)
        {
            if (column.NormalizedName == TransactionPropertiesNames.Offset.NormalizedName)
            {
                sb.Append(queryOffset + ", ");
                continue;
            }

            sb.Append(column.DataBaseName + ", ");
        }
        sb.Remove(sb.Length - 2, 1);
        return sb.ToString();
    }
}
