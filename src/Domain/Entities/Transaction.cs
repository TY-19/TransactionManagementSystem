namespace TMS.Domain.Entities;

public class Transaction
{
    public string TransactionId { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTimeOffset TransactionDate { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
}
