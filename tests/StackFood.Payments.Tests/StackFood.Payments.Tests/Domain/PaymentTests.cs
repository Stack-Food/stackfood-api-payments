using FluentAssertions;
using StackFood.Payments.Domain.Entities;
using StackFood.Payments.Domain.Enums;

namespace StackFood.Payments.Tests.Domain;

public class PaymentTests
{
    [Fact]
    public void Constructor_ShouldCreatePaymentWithPendingStatus()
    {
        // Arrange & Act
        var payment = new Payment();

        // Assert
        payment.PaymentId.Should().NotBeEmpty();
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.PaymentMethod.Should().Be(PaymentMethod.QRCode);
        payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        payment.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void Approve_ShouldSetStatusToApproved()
    {
        // Arrange
        var payment = new Payment
        {
            OrderId = Guid.NewGuid(),
            Amount = 100m
        };

        // Act
        payment.Approve();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Approved);
        payment.ApprovedAt.Should().NotBeNull();
        payment.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Reject_ShouldSetStatusToRejected()
    {
        // Arrange
        var payment = new Payment
        {
            OrderId = Guid.NewGuid(),
            Amount = 100m
        };

        // Act
        payment.Reject("Insufficient funds");

        // Assert
        payment.Status.Should().Be(PaymentStatus.Rejected);
        payment.RejectedAt.Should().NotBeNull();
        payment.RejectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        payment.Metadata.Should().ContainKey("rejection_reason");
        payment.Metadata!["rejection_reason"].Should().Be("Insufficient funds");
    }

    [Fact]
    public void Reject_WithoutReason_ShouldNotAddMetadata()
    {
        // Arrange
        var payment = new Payment
        {
            OrderId = Guid.NewGuid(),
            Amount = 100m
        };

        // Act
        payment.Reject();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Rejected);
        payment.Metadata.Should().NotContainKey("rejection_reason");
    }

    [Fact]
    public void Cancel_ShouldSetStatusToCancelled()
    {
        // Arrange
        var payment = new Payment
        {
            OrderId = Guid.NewGuid(),
            Amount = 100m
        };

        // Act
        payment.Cancel();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Payment_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payment = new Payment
        {
            OrderId = orderId,
            OrderNumber = "ORD-001",
            Amount = 250.50m,
            CustomerName = "John Doe",
            QRCode = "base64-qr-code",
            QRCodeUrl = "https://mp.com/qr/123"
        };

        // Assert
        payment.OrderId.Should().Be(orderId);
        payment.OrderNumber.Should().Be("ORD-001");
        payment.Amount.Should().Be(250.50m);
        payment.CustomerName.Should().Be("John Doe");
        payment.QRCode.Should().Be("base64-qr-code");
        payment.QRCodeUrl.Should().Be("https://mp.com/qr/123");
    }
}
