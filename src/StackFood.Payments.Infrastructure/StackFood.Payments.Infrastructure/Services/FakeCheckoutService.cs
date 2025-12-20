using StackFood.Payments.Application.Interfaces;
using StackFood.Payments.Domain.Enums;

namespace StackFood.Payments.Infrastructure.Services;

public class FakeCheckoutService : IFakeCheckoutService
{
    public PaymentStatus DetermineStatus(string customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return PaymentStatus.Pending;

        var upperName = customerName.ToUpper();

        if (upperName.Contains("PAGO"))
            return PaymentStatus.Approved;

        if (upperName.Contains("CANCELADO") || upperName.Contains("REJECTED"))
            return PaymentStatus.Rejected;

        return PaymentStatus.Pending;
    }
}
