using Microsoft.AspNetCore.Mvc;
using StackFood.Payments.Application.DTOs;
using StackFood.Payments.Application.Interfaces;
using StackFood.Payments.Application.UseCases.CreatePayment;

namespace StackFood.Payments.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly CreatePaymentUseCase _createPaymentUseCase;

    public PaymentsController(
        IPaymentRepository paymentRepository,
        CreatePaymentUseCase createPaymentUseCase)
    {
        _paymentRepository = paymentRepository;
        _createPaymentUseCase = createPaymentUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDTO>> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var payment = await _createPaymentUseCase.ExecuteAsync(request);
            return CreatedAtAction(nameof(GetPaymentById), new { id = payment.PaymentId }, payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentDTO>> GetPaymentById(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);

        if (payment == null)
            return NotFound(new { error = "Payment not found" });

        return Ok(new PaymentDTO
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
        });
    }

    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<PaymentDTO>> GetPaymentByOrderId(Guid orderId)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);

        if (payment == null)
            return NotFound(new { error = "Payment not found for this order" });

        return Ok(new PaymentDTO
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
        });
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetPaymentsByStatus(string status)
    {
        var payments = await _paymentRepository.GetByStatusAsync(status);

        var paymentDtos = payments.Select(p => new PaymentDTO
        {
            PaymentId = p.PaymentId,
            OrderId = p.OrderId,
            OrderNumber = p.OrderNumber,
            Amount = p.Amount,
            Status = p.Status.ToString(),
            PaymentMethod = p.PaymentMethod.ToString(),
            QRCode = p.QRCode,
            QRCodeUrl = p.QRCodeUrl,
            CreatedAt = p.CreatedAt,
            ApprovedAt = p.ApprovedAt
        });

        return Ok(paymentDtos);
    }
}
