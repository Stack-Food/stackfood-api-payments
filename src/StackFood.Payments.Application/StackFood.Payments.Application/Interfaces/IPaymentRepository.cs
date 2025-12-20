using StackFood.Payments.Domain.Entities;

namespace StackFood.Payments.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid paymentId);
    Task<Payment?> GetByOrderIdAsync(Guid orderId);
    Task<IEnumerable<Payment>> GetByStatusAsync(string status);
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
    Task DeleteAsync(Guid paymentId);
}
