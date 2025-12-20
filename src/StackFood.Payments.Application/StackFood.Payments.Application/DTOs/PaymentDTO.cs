using StackFood.Payments.Domain.Enums;

namespace StackFood.Payments.Application.DTOs;

public class PaymentDTO
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? QRCode { get; set; }
    public string? QRCodeUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
