using StackFood.Payments.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace StackFood.Payments.Domain.Entities;

[ExcludeFromCodeCoverage]
public class Payment
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    // Mercado Pago specific
    public string? MercadoPagoPaymentId { get; set; }
    public string? QRCode { get; set; }
    public string? QRCodeUrl { get; set; }

    // Customer info
    public string? CustomerName { get; set; }

    // Metadata - flexible JSON for MP response
    public Dictionary<string, object>? Metadata { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public Payment()
    {
        PaymentId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = PaymentStatus.Pending;
        PaymentMethod = PaymentMethod.QRCode;
        Metadata = new Dictionary<string, object>();
    }

    public void Approve()
    {
        Status = PaymentStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string? reason = null)
    {
        Status = PaymentStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        if (reason != null && Metadata != null)
        {
            Metadata["rejection_reason"] = reason;
        }
    }

    public void Cancel()
    {
        Status = PaymentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
