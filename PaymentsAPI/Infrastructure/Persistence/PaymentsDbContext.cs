using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsAPI.Domain.Entities;

namespace PaymentsAPI.Infrastructure.Persistence;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(b =>
        {
            b.ToTable("Payments");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.OrderId).IsUnique();
            b.HasIndex(x => x.UserId);
            b.Property(x => x.Reason).HasMaxLength(400);
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
