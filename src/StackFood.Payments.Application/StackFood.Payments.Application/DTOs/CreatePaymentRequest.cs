namespace StackFood.Payments.Application.DTOs;

public class CreatePaymentRequest
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? CustomerName { get; set; }
}
