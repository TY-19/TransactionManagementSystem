using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
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
        var sql = @$"SELECT {GetRequestedColumnNames(request.RequestedColumns)}
            FROM Transactions
            JOIN Clients ON Transactions.ClientId = Clients.Id
            {GetFilterByTimeInterval(request)}
            ORDER BY {request.SortBy} {(request.SortAsc ? "ASC" : "DESC")}";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);

        return await dbConnection.QueryAsync<TransactionExportDto>(sql);
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

        if (query.TimeZone == null)
        {
            string composite = "";
            if (query.StartDate != null && query.EndDate != null)
                composite = " AND ";

            string startFilter = query.StartDate == null ? ""
                : FilterByClientDate(query.StartDate.Year, query.StartDate.Month, query.StartDate.Day, true);

            string endFilter = query.EndDate == null ? ""
                : FilterByClientDate(query.EndDate.Year,
                    query.EndDate.Month, query.EndDate.Day, false);

            return $"WHERE {startFilter}{composite}{endFilter}";
        }
        else
        {
            return FilterByUserTimeZone(query.TimeZone, query.StartDate, query.EndDate);
        }
    }

    private static string FilterByClientDate(int year, int month, int day, bool isStartDate)
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
        {
            return GetFilterByStartAndEndDateInUserTimeZone(startDate, endDate,
                standardOffset, dstOffset);
        }
        else if (startDate != null)
        {
            return GetFilterByStartOrEndDateInUserTimeZone(startDate.Year, startDate.Month,
                startDate.Day, standardOffset, dstOffset, true);
        }
        else if (endDate != null)
        {
            return GetFilterByStartOrEndDateInUserTimeZone(endDate.Year, endDate.Month,
                endDate.Day, standardOffset, dstOffset, false);
        }
        else
        {
            return string.Empty;
        }
    }

    private static string GetReadableOffset(int offset)
    {
        char offsetSign = offset >= 0 ? '+' : '-';
        return $"{offsetSign}{Math.Abs(offset / 3600):D2}:{Math.Abs(offset % 60):D2}";
    }

    private static string GetFilterByStartOrEndDateInUserTimeZone(int year, int month, int day, string offset,
        string? dstOffset, bool isStartDate)
    {
        string sign = isStartDate ? ">=" : "<=";
        string time = isStartDate ? "00:00:00" : "23:59:59";
        string clause = $"WHERE TransactionDate {sign} '{year}-{month}-{day} {time} {offset}' ";

        if (!string.IsNullOrEmpty(dstOffset))
            clause += $"OR TransactionDate {sign} '{year}-{month}-{day} {time} {dstOffset}')";

        return clause;
    }

    private static string GetFilterByStartAndEndDateInUserTimeZone(DateFilterParameters startDate,
        DateFilterParameters endDate, string offset, string? dstOffset)
    {
        string greaterOrEqual = ">=";
        string smallerOrEqual = "<=";
        string startTime = "00:00:00";
        string endTime = "23:59:59";

        if (string.IsNullOrEmpty(dstOffset))
            return @$"WHERE TransactionDate {greaterOrEqual} '{startDate.Year}-{startDate.Month}-{startDate.Day} {startTime} {offset}'
                    AND TransactionDate {smallerOrEqual} '{endDate.Year}-{endDate.Month}-{endDate.Day} {endTime} {offset}'";
        else
        {
            return @$"WHERE ((TransactionDate {greaterOrEqual} '{startDate.Year}-{startDate.Month}-{startDate.Day} {startTime} {offset}'
                    OR TransactionDate {greaterOrEqual} '{startDate.Year}-{startDate.Month}-{startDate.Day} {startTime} {dstOffset}')
                    AND (TransactionDate {smallerOrEqual} '{endDate.Year}-{endDate.Month}-{endDate.Day} {endTime} {offset}'
                    OR TransactionDate {smallerOrEqual} '{endDate.Year}-{endDate.Month}-{endDate.Day} {endTime} {dstOffset}'))";
        }
    }
}
