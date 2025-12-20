using System.Diagnostics.CodeAnalysis;

namespace StackFood.Payments.Domain.Events;

[ExcludeFromCodeCoverage]
public class PaymentRejectedEvent
{
    public string EventType => "PaymentRejected";
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }

    public PaymentRejectedEvent()
    {
        Timestamp = DateTime.UtcNow;
    }
}
