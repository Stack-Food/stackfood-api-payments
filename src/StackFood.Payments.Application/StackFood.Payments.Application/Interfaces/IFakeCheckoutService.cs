using StackFood.Payments.Domain.Enums;

namespace StackFood.Payments.Application.Interfaces;

public interface IFakeCheckoutService
{
    PaymentStatus DetermineStatus(string customerName);
}
