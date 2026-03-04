using Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsAPI.Domain.Entities;
using PaymentsAPI.Infrastructure.Persistence;

namespace PaymentsAPI.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEventV1>
{
    private readonly PaymentsDbContext _db;
    private readonly ILogger<OrderPlacedConsumer> _logger;
    private readonly IConfiguration _config;

    public OrderPlacedConsumer(PaymentsDbContext db, ILogger<OrderPlacedConsumer> logger, IConfiguration config)
    {
        _db = db;
        _logger = logger;
        _config = config;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEventV1> context)
    {
        var evt = context.Message;

        var existing = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == evt.OrderId);
        if (existing is not null)
        {
            _logger.LogInformation("Duplicate ignored: OrderId={OrderId}", evt.OrderId);
            return;
        }

        var approvalRate = _config.GetValue<int>("PaymentSimulation:ApprovalRatePercent", 80);
        var rnd = Random.Shared.Next(1, 101);
        var approved = rnd <= approvalRate;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = evt.OrderId,
            UserId = evt.UserId,
            Status = approved ? PaymentStatus.Approved : PaymentStatus.Rejected,
            Reason = approved ? null : "Payment reject (simulation)",
            ProcessedAtUtc = DateTime.UtcNow
        };

        _db.Payments.Add(payment);

        var processed = new PaymentProcessedEventV1(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTime.UtcNow,
            PaymentId: payment.Id,
            OrderId: payment.OrderId,
            UserId: payment.UserId,
            Status: payment.Status.ToString(),
            Reason: payment.Reason
        );

        await context.Publish(processed, p =>
        {
            p.SetRoutingKey("v1.payment-processed");
            p.CorrelationId = processed.EventId;
        });

        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Payment processed: OrderId={OrderId}, Status={Status}", payment.OrderId,
            payment.Status);
    }
}