using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Domain.Entities;

namespace TMS.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.TransactionId);
        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);
        builder.HasOne(t => t.Client)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.ClientId);
    }
}
