namespace TMS.Application.Models;

public class TransactionExportDto
{
    public string? TransactionId { get; set; } = null!;
    public string? Name { get; set; } = null!;
    public string? Email { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Amount { get; set; }
    public DateTimeOffset? TransactionDate { get; set; }
    public string? Offset { get; set; }
}
