using FluentAssertions;
using Moq;
using StackFood.Payments.Application.DTOs;
using StackFood.Payments.Application.Interfaces;
using StackFood.Payments.Application.UseCases.CreatePayment;
using StackFood.Payments.Domain.Entities;
using StackFood.Payments.Domain.Enums;

namespace StackFood.Payments.Tests.UseCases;

public class CreatePaymentUseCaseTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<IFakeCheckoutService> _fakeCheckoutServiceMock;
    private readonly CreatePaymentUseCase _useCase;
    private const string TopicArn = "arn:aws:sns:us-east-1:000000000000:payment-events";

    public CreatePaymentUseCaseTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _fakeCheckoutServiceMock = new Mock<IFakeCheckoutService>();
        _useCase = new CreatePaymentUseCase(
            _paymentRepositoryMock.Object,
            _eventPublisherMock.Object,
            _fakeCheckoutServiceMock.Object,
            TopicArn
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithApprovedPayment_ShouldCreateAndPublishApprovedEvent()
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            OrderId = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            Amount = 100m,
            CustomerName = "João PAGO"
        };

        _fakeCheckoutServiceMock
            .Setup(x => x.DetermineStatus("João PAGO"))
            .Returns(PaymentStatus.Approved);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(request.OrderId);
        result.Amount.Should().Be(100m);
        result.Status.Should().Be("Approved");

        _paymentRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Payment>()), Times.Once);
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), TopicArn),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithRejectedPayment_ShouldCreateAndPublishRejectedEvent()
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            OrderId = Guid.NewGuid(),
            OrderNumber = "ORD-002",
            Amount = 50m,
            CustomerName = "Maria CANCELADO"
        };

        _fakeCheckoutServiceMock
            .Setup(x => x.DetermineStatus("Maria CANCELADO"))
            .Returns(PaymentStatus.Rejected);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Rejected");

        _paymentRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Payment>()), Times.Once);
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), TopicArn),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingPayment_ShouldCreateAndPublishPendingEvent()
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            OrderId = Guid.NewGuid(),
            OrderNumber = "ORD-003",
            Amount = 75m,
            CustomerName = "Carlos Silva"
        };

        _fakeCheckoutServiceMock
            .Setup(x => x.DetermineStatus("Carlos Silva"))
            .Returns(PaymentStatus.Pending);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Pending");

        _paymentRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Payment>()), Times.Once);
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), TopicArn),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new CreatePaymentRequest
        {
            OrderId = orderId,
            OrderNumber = "ORD-123",
            Amount = 199.99m,
            CustomerName = "Test User PAGO"
        };

        _fakeCheckoutServiceMock
            .Setup(x => x.DetermineStatus(It.IsAny<string>()))
            .Returns(PaymentStatus.Approved);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.PaymentId.Should().NotBeEmpty();
        result.OrderId.Should().Be(orderId);
        result.OrderNumber.Should().Be("ORD-123");
        result.Amount.Should().Be(199.99m);
        result.PaymentMethod.Should().Be("QRCode");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetExpirationTime()
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            OrderId = Guid.NewGuid(),
            OrderNumber = "ORD-004",
            Amount = 50m,
            CustomerName = "Test"
        };

        Payment? capturedPayment = null;
        _fakeCheckoutServiceMock
            .Setup(x => x.DetermineStatus(It.IsAny<string>()))
            .Returns(PaymentStatus.Pending);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>()))
            .Callback<Payment>(p => capturedPayment = p)
            .ReturnsAsync((Payment p) => p);

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        capturedPayment.Should().NotBeNull();
        capturedPayment!.ExpiresAt.Should().NotBeNull();
        capturedPayment.ExpiresAt.Should().BeCloseTo(
            DateTime.UtcNow.AddHours(2),
            TimeSpan.FromSeconds(5)
        );
    }
}
