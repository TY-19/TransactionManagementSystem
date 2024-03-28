using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using System.Text;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Queries.TransactionClient.GetTransactionsClients;

public class GetTransactionsClientsQueryHandler(
    IDbConnectionOptions connectionOptions
    ) : IRequestHandler<GetTransactionsClientsQuery, IEnumerable<TransactionExportDto>>
{
    private readonly Dictionary<string, string> calculationRules = new()
    {
        { "calculateOffset", @$"DATENAME(tzoffset, TransactionDate) AS Offset" }
    };

    public async Task<IEnumerable<TransactionExportDto>> Handle(
        GetTransactionsClientsQuery request, CancellationToken cancellationToken)
    {
        string columns = BuildSelectClause(request.ColumnNames);
        string filtering = BuildWhereClause(request.UseUserTimeZone, request.DateFrom,
            request.DateTo, request.DateFromOffset, request.DateToOffset);
        string ordering = BuildOrderByClause(request.SortBy, request.SortAsc);

        string sql = @$"SELECT {columns}
            FROM Transactions
            JOIN Clients ON Transactions.ClientId = Clients.Id
            {filtering}
            {ordering}";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        return await dbConnection.QueryAsync<TransactionExportDto>(sql);
    }

    private string BuildSelectClause(IEnumerable<string> requestedColumns)
    {
        StringBuilder sb = new();
        foreach (string column in requestedColumns)
        {
            sb.Append(column + ", ");
        }
        ApplyCalculationRules(sb);
        sb.Remove(sb.Length - 2, 1);
        return sb.ToString();
    }

    private void ApplyCalculationRules(StringBuilder sb)
    {
        foreach (KeyValuePair<string, string> rule in calculationRules)
        {
            sb.Replace(rule.Key, rule.Value);
        }
    }

    private static string BuildWhereClause(bool useUserTimeZone, DateOnly? DateFrom,
        DateOnly? DateTo, string DateFromOffset, string DateToOffset)
    {
        return useUserTimeZone
            ? FilterByUserTimeZone(DateFrom, DateTo, DateFromOffset, DateToOffset)
            : FilterByClientDate(DateFrom, DateTo);
    }

    private static string FilterByClientDate(DateOnly? DateFrom, DateOnly? DateTo)
    {
        if (DateFrom == null && DateTo == null)
        {
            return string.Empty;
        }
        string start = (DateFrom ?? DateOnly.MinValue).ToString("yyyy-MM-dd");
        string end = (DateTo ?? DateOnly.MaxValue).ToString("yyyy-MM-dd");
        return $"WHERE Convert(date, TransactionDate) BETWEEN '{start}' AND '{end}'";
    }

    private static string FilterByUserTimeZone(DateOnly? DateFrom,
        DateOnly? DateTo, string DateFromOffset, string DateToOffset)
    {
        if (DateFrom == null && DateTo == null)
        {
            return string.Empty;
        }
        else if (DateFrom != null && DateTo != null)
        {
            return GetConditionOffsetBothLimits(DateFrom.Value, DateTo.Value, DateFromOffset, DateToOffset);
        }
        else if (DateFrom != null)
        {
            return GetConditionOffsetEitherLimit(DateFrom.Value, DateFromOffset, true);
        }
        else
        {
            return GetConditionOffsetEitherLimit(DateTo!.Value, DateToOffset, false);
        }
    }

    private static string GetConditionOffsetEitherLimit(DateOnly date, string offset, bool isStart)
    {
        string sign = isStart ? ">=" : "<=";
        string time = isStart ? "00:00:00" : "23:59:59";
        return $"WHERE TransactionDate {sign} '{date.Year}-{date.Month}-{date.Day} {time} {offset}' ";
    }

    private static string GetConditionOffsetBothLimits(DateOnly DateFrom,
        DateOnly DateTo, string DateFromOffset, string DateToOffset)
    {
        string greaterOrEqual = ">=";
        string smallerOrEqual = "<=";
        string startTime = "00:00:00";
        string endTime = "23:59:59";
        return @$"WHERE TransactionDate {greaterOrEqual} '{DateFrom.Year}-{DateFrom.Month}-{DateFrom.Day} {startTime} {DateFromOffset}'
                AND TransactionDate {smallerOrEqual} '{DateTo.Year}-{DateTo.Month}-{DateTo.Day} {endTime} {DateToOffset}'";
    }

    private static string BuildOrderByClause(string? sortBy, bool sortAsc)
    {
        if (sortBy == null)
        {
            return string.Empty;
        }
        return $"ORDER BY {sortBy} {(sortAsc ? "ASC" : "DESC")}";
    }
}
