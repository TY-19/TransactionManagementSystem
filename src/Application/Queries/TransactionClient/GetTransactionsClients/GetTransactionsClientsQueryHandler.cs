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
        string filtering = BuildWhereClause(request.UseUserTimeZone, request.StartDate,
            request.EndDate, request.StartDateOffset, request.EndDateOffset);
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

    private static string BuildWhereClause(bool useUserTimeZone, DateFilterParameters? startDate,
        DateFilterParameters? endDate, string startDateOffset, string endDateOffset)
    {
        return useUserTimeZone
            ? FilterByUserTimeZone(startDate, endDate, startDateOffset, endDateOffset)
            : FilterByClientDate(startDate, endDate);
    }

    private static string FilterByClientDate(DateFilterParameters? startDate, DateFilterParameters? endDate)
    {
        if (startDate == null && endDate == null)
        {
            return string.Empty;
        }
        string startFilter = startDate == null ? "" : GetConditionNoOffset(startDate);
        string composite = startDate != null && endDate != null ? " AND " : "";
        string endFilter = endDate == null ? "" : GetConditionNoOffset(endDate);
        return $"WHERE {startFilter}{composite}{endFilter}";
    }

    private static string GetConditionNoOffset(DateFilterParameters date)
    {
        string sign = date.IsStartDate ? ">" : "<";
        return @$"(DATEPART(YYYY, TransactionDate) {sign} {date.Year}
            OR (DATEPART(YYYY, TransactionDate) = {date.Year} AND DATEPART(MM, TransactionDate) {sign} {date.Month})
            OR (DATEPART(YYYY, TransactionDate) = {date.Year} AND DATEPART(MM, TransactionDate) = {date.Month} AND DATEPART(DD, TransactionDate) {sign}= {date.Day}))";
    }

    private static string FilterByUserTimeZone(DateFilterParameters? startDate,
        DateFilterParameters? endDate, string startDateOffset, string endDateOffset)
    {
        if (startDate == null && endDate == null)
        {
            return string.Empty;
        }
        else if (startDate != null && endDate != null)
        {
            return GetConditionOffsetBothLimits(startDate, endDate, startDateOffset, endDateOffset);
        }
        else if (startDate != null)
        {
            return GetConditionOffsetEitherLimit(startDate, startDateOffset);
        }
        else
        {
            return GetConditionOffsetEitherLimit(endDate!, endDateOffset);
        }
    }

    private static string GetConditionOffsetEitherLimit(DateFilterParameters date, string offset)
    {
        string sign = date.IsStartDate ? ">=" : "<=";
        string time = date.IsStartDate ? "00:00:00" : "23:59:59";
        return $"WHERE TransactionDate {sign} '{date.Year}-{date.Month}-{date.Day} {time} {offset}' ";
    }

    private static string GetConditionOffsetBothLimits(DateFilterParameters startDate,
        DateFilterParameters endDate, string startDateOffset, string endDateOffset)
    {
        string greaterOrEqual = ">=";
        string smallerOrEqual = "<=";
        string startTime = "00:00:00";
        string endTime = "23:59:59";
        return @$"WHERE TransactionDate {greaterOrEqual} '{startDate.Year}-{startDate.Month}-{startDate.Day} {startTime} {startDateOffset}'
                AND TransactionDate {smallerOrEqual} '{endDate.Year}-{endDate.Month}-{endDate.Day} {endTime} {endDateOffset}'";
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
