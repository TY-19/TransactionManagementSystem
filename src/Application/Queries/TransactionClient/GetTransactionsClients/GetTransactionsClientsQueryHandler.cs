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
    public async Task<IEnumerable<TransactionExportDto>> Handle(GetTransactionsClientsQuery request, CancellationToken cancellationToken)
    {
        var sql = @$"SELECT {BuildColumns(request.RequestedColumns)}
            FROM Transactions
            JOIN Clients ON Transactions.ClientId = Clients.Id
            {BuildWhereClause(request)}
            {BuildOrderByClause(request.SortBy, request.SortAsc)}";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);

        return await dbConnection.QueryAsync<TransactionExportDto>(sql);
    }

    private string BuildColumns(List<string> requestedColumns)
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

    private static string BuildWhereClause(GetTransactionsClientsQuery query)
    {
        if (query.StartDate == null && query.EndDate == null)
            return "";

        return query.TimeZone == null
            ? FilterByClientDate(query.StartDate, query.EndDate)
            : FilterByUserTimeZone(query.TimeZone, query.StartDate, query.EndDate);
    }
    private static string FilterByClientDate(DateFilterParameters? startDate, DateFilterParameters? endDate)
    {
        string composite = "";
        if (startDate != null && endDate != null)
            composite = " AND ";

        string startFilter = startDate == null ? "" 
            : GetFilterByClientDate(startDate.Year, startDate.Month, startDate.Day, true);
        string endFilter = endDate == null ? ""
            : GetFilterByClientDate(endDate.Year, endDate.Month, endDate.Day, false);

        return $"WHERE {startFilter}{composite}{endFilter}";
    }

    private static string GetFilterByClientDate(int year, int month, int day, bool isStartDate)
    {
        string sign = isStartDate ? ">" : "<";
        return @$"(DATEPART(YYYY, TransactionDate) {sign} {year}
            OR (DATEPART(YYYY, TransactionDate) = {year} AND DATEPART(MM, TransactionDate) {sign} {month})
            OR (DATEPART(YYYY, TransactionDate) = {year} AND DATEPART(MM, TransactionDate) = {month} AND DATEPART(DD, TransactionDate) {sign}= {day}))";
    }

    private static string FilterByUserTimeZone(TimeZoneDetails timeZone, DateFilterParameters? startDate, DateFilterParameters? endDate)
    {
        string standardOffset = GetReadableOffset(timeZone?.StandardUtcOffsetSeconds ?? 0);

        string? dstOffset = timeZone!.HasDayLightSaving
            ? GetReadableOffset(timeZone.DstOffsetToUtcSeconds!.Value)
            : null;

        if (startDate != null && endDate != null)
            return GetFilterWithBothLimitsInUserTimeZone(startDate, endDate, standardOffset, dstOffset);
        
        if (startDate != null)
            return GetFilterWithEitherLimitInUserTimeZone(startDate.Year, startDate.Month, startDate.Day, standardOffset, dstOffset, true);
        
        if (endDate != null)
            return GetFilterWithEitherLimitInUserTimeZone(endDate.Year, endDate.Month, endDate.Day, standardOffset, dstOffset, false);

        return string.Empty;
    }

    private static string GetReadableOffset(int offset)
    {
        char offsetSign = offset >= 0 ? '+' : '-';
        return $"{offsetSign}{Math.Abs(offset / 3600):D2}:{Math.Abs((offset % 3600) / 60):D2}";
    }

    private static string GetFilterWithEitherLimitInUserTimeZone(int year, int month, int day, string offset,
        string? dstOffset, bool isStartDate)
    {
        string sign = isStartDate ? ">=" : "<=";
        string time = isStartDate ? "00:00:00" : "23:59:59";

        if (!string.IsNullOrEmpty(dstOffset) && isStartDate)
            offset = dstOffset;

        return $"WHERE TransactionDate {sign} '{year}-{month}-{day} {time} {offset}' ";
    }

    private static string GetFilterWithBothLimitsInUserTimeZone(DateFilterParameters startDate,
        DateFilterParameters endDate, string offset, string? dstOffset)
    {
        string greaterOrEqual = ">=";
        string smallerOrEqual = "<=";
        string startTime = "00:00:00";
        string endTime = "23:59:59";

        if (string.IsNullOrEmpty(dstOffset))
            dstOffset = offset;
            
        return @$"WHERE TransactionDate {greaterOrEqual} '{startDate.Year}-{startDate.Month}-{startDate.Day} {startTime} {dstOffset}'
                AND TransactionDate {smallerOrEqual} '{endDate.Year}-{endDate.Month}-{endDate.Day} {endTime} {offset}'";
    }

    private static string BuildOrderByClause(string? sortBy, bool sortAsc)
    {
        if (sortBy == null)
            return string.Empty;

        return $"ORDER BY {sortBy} {(sortAsc ? "ASC" : "DESC")}";
    }
}
