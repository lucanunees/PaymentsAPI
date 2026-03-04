namespace PaymentsAPI.Domain.Entities;

public enum PaymentStatus
{
    Approved = 1,
    Rejected = 2
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Reason { get; set; }
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}