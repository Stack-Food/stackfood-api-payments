using System.Diagnostics.CodeAnalysis;

namespace StackFood.Payments.Domain.Events;

[ExcludeFromCodeCoverage]
public class PaymentApprovedEvent
{
    public string EventType => "PaymentApproved";
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ApprovedAt { get; set; }
    public DateTime Timestamp { get; set; }

    public PaymentApprovedEvent()
    {
        Timestamp = DateTime.UtcNow;
        ApprovedAt = DateTime.UtcNow;
    }
}
