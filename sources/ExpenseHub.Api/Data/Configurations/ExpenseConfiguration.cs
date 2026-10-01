using ExpenseHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(expense => expense.Id);

        builder.Property(expense => expense.OwnerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(expense => expense.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(expense => expense.Amount)
            .HasPrecision(18, 2);

        builder.Property(expense => expense.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(expense => expense.Version)
            .IsConcurrencyToken();

        builder.HasIndex(expense => expense.OwnerId);
        builder.HasIndex(expense => expense.Status);

        builder.HasOne(expense => expense.Category)
            .WithMany()
            .HasForeignKey(expense => expense.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(expense => expense.History)
            .WithOne()
            .HasForeignKey(history => history.ExpenseId);

        builder.HasOne(expense => expense.Payment)
            .WithOne()
            .HasForeignKey<PaymentRecord>(payment => payment.ExpenseId);
    }
}
