using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using System.Text;
using TMS.Application.Interfaces;
using TMS.Application.Models.Dtos;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClients;

public class GetTransactionsClientsQueryHandler(
    IDbConnectionOptions connectionOptions
    ) : IRequestHandler<GetTransactionsClientsQuery, IEnumerable<TransactionClientExportDto>>
{
    private readonly Dictionary<string, string> calculationRules = new()
    {
        { "calculateOffset", @$"DATENAME(tzoffset, TransactionDate) AS Offset" }
    };
    public async Task<IEnumerable<TransactionClientExportDto>> Handle(GetTransactionsClientsQuery request, CancellationToken cancellationToken)
    {
        var sql = @$"SELECT {GetRequestedColumnNames(request.RequestedColumns)}
            FROM Transactions
            JOIN Clients ON Transactions.ClientId = Clients.Id
            {GetFilterByTimeInterval(request)}
            ORDER BY {request.SortBy} {(request.SortAsc ? "ASC" : "DESC")}";

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

    private static string GetFilterByTimeInterval(GetTransactionsClientsQuery query)
    {
        if (query.StartDate == null && query.EndDate == null)
            return "";

        string composite = "";
        if (query.StartDate != null && query.EndDate != null)
            composite = " AND ";

        string startFilter = query.StartDate == null ? ""
            : GetFilter(query.UserTimeZoneOffset, query.StartDate.Year,
                query.StartDate.Month, query.StartDate.Day, true);

        string endFilter = query.EndDate == null ? ""
            : GetFilter(query.UserTimeZoneOffset, query.EndDate.Year,
                query.EndDate.Month, query.EndDate.Day, false);

        return $"WHERE {startFilter}{composite}{endFilter}";
    }

    private static string GetFilter(string? offset, int year, int month, int day, bool isStartDate)
    {
        return (string.IsNullOrEmpty(offset))
            ? FilterByClientDate(year, month, day, isStartDate)
            : FilterByCurrentUserDate(offset, year, month, day, isStartDate);
    }

    private static string FilterByClientDate(int year, int month, int day, bool isStartDate)
    {
        string sign = isStartDate ? ">" : "<";
        return @$"(DATEPART(YYYY, TransactionDate) {sign} {year}
            OR (DATEPART(YYYY, TransactionDate) = {year} AND DATEPART(MM, TransactionDate) {sign} {month})
            OR (DATEPART(YYYY, TransactionDate) = {year} AND DATEPART(MM, TransactionDate) = {month} AND DATEPART(DD, TransactionDate) {sign}= {day}))";
    }

    private static string FilterByCurrentUserDate(string offset, int year, int month, int day, bool isStartDate)
    {
        string sign = isStartDate ? ">=" : "<=";
        string time = isStartDate ? "00:00:00" : "23:59:59";
        return $"TransactionDate {sign} '{year}-{month}-{day} {time} {offset}'";
    }
}
