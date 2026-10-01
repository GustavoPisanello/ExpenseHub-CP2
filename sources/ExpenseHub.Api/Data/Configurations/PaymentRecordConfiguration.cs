using ExpenseHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

internal sealed class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.PaidById)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2);

        builder.HasIndex(payment => payment.ExpenseId)
            .IsUnique();
    }
}
