namespace StackFood.Payments.Domain.Events;

public class PaymentPendingEvent
{
    public string EventType => "PaymentPending";
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime Timestamp { get; set; }

    public PaymentPendingEvent()
    {
        Timestamp = DateTime.UtcNow;
    }
}
