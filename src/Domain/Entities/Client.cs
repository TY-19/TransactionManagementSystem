namespace TMS.Domain.Entities;

public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public IEnumerable<Transaction> Transactions { get; set; } = [];
}
