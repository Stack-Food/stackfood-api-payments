using StackFood.Payments.Application.DTOs;
using StackFood.Payments.Application.Interfaces;
using StackFood.Payments.Domain.Entities;
using StackFood.Payments.Domain.Enums;
using StackFood.Payments.Domain.Events;

namespace StackFood.Payments.Application.UseCases.CreatePayment;

public class CreatePaymentUseCase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IFakeCheckoutService _fakeCheckoutService;
    private readonly string _snsTopicArn;

    public CreatePaymentUseCase(
        IPaymentRepository paymentRepository,
        IEventPublisher eventPublisher,
        IFakeCheckoutService fakeCheckoutService,
        string snsTopicArn)
    {
        _paymentRepository = paymentRepository;
        _eventPublisher = eventPublisher;
        _fakeCheckoutService = fakeCheckoutService;
        _snsTopicArn = snsTopicArn;
    }

    public async Task<PaymentDTO> ExecuteAsync(CreatePaymentRequest request)
    {
        // Criar pagamento
        var payment = new Payment
        {
            OrderId = request.OrderId,
            OrderNumber = request.OrderNumber,
            Amount = request.Amount,
            CustomerName = request.CustomerName,
            PaymentMethod = PaymentMethod.QRCode,
            Status = PaymentStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };

        // Simular checkout falso baseado no nome do cliente
        var fakeStatus = _fakeCheckoutService.DetermineStatus(request.CustomerName ?? string.Empty);
        payment.Status = fakeStatus;

        if (fakeStatus == PaymentStatus.Approved)
        {
            payment.Approve();
        }
        else if (fakeStatus == PaymentStatus.Rejected)
        {
            payment.Reject("Pagamento rejeitado via fake checkout");
        }

        // Salvar no DynamoDB
        var savedPayment = await _paymentRepository.CreateAsync(payment);

        // Publicar evento SNS
        await PublishPaymentEventAsync(savedPayment);

        // Retornar DTO
        return MapToDTO(savedPayment);
    }

    private async Task PublishPaymentEventAsync(Payment payment)
    {
        switch (payment.Status)
        {
            case PaymentStatus.Approved:
                var approvedEvent = new PaymentApprovedEvent
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    OrderNumber = payment.OrderNumber,
                    Amount = payment.Amount,
                    ApprovedAt = payment.ApprovedAt ?? DateTime.UtcNow
                };
                await _eventPublisher.PublishAsync(approvedEvent, _snsTopicArn);
                break;

            case PaymentStatus.Rejected:
                var rejectedEvent = new PaymentRejectedEvent
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    OrderNumber = payment.OrderNumber,
                    Reason = payment.Metadata?.ContainsKey("rejection_reason") == true
                        ? payment.Metadata["rejection_reason"]?.ToString()
                        : "Unknown"
                };
                await _eventPublisher.PublishAsync(rejectedEvent, _snsTopicArn);
                break;

            case PaymentStatus.Pending:
                var pendingEvent = new PaymentPendingEvent
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    OrderNumber = payment.OrderNumber,
                    ExpiresAt = payment.ExpiresAt
                };
                await _eventPublisher.PublishAsync(pendingEvent, _snsTopicArn);
                break;
        }
    }

    private PaymentDTO MapToDTO(Payment payment)
    {
        return new PaymentDTO
        {
            PaymentId = payment.PaymentId,
            OrderId = payment.OrderId,
            OrderNumber = payment.OrderNumber,
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            PaymentMethod = payment.PaymentMethod.ToString(),
            QRCode = payment.QRCode,
            QRCodeUrl = payment.QRCodeUrl,
            CreatedAt = payment.CreatedAt,
            ApprovedAt = payment.ApprovedAt
        };
    }
}
