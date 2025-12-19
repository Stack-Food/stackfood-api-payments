using FluentAssertions;
using StackFood.Payments.Application.Interfaces;
using StackFood.Payments.Domain.Enums;
using StackFood.Payments.Infrastructure.Services;

namespace StackFood.Payments.Tests.Services
{
    public class FakeCheckoutServiceTests
    {
        private readonly IFakeCheckoutService _service;

        public FakeCheckoutServiceTests()
        {
            _service = new FakeCheckoutService();
        }

        [Theory]
        [InlineData(null, PaymentStatus.Pending)]
        [InlineData("", PaymentStatus.Pending)]
        [InlineData("   ", PaymentStatus.Pending)]
        [InlineData("pago", PaymentStatus.Approved)]
        [InlineData("PAGO", PaymentStatus.Approved)]
        [InlineData("Cliente Pago", PaymentStatus.Approved)]
        [InlineData("cancelado", PaymentStatus.Rejected)]
        [InlineData("CANCELADO", PaymentStatus.Rejected)]
        [InlineData("Order Rejected", PaymentStatus.Rejected)]
        [InlineData("SomeOtherName", PaymentStatus.Pending)]
        public void DetermineStatus_ShouldReturnExpectedStatus(string customerName, PaymentStatus expectedStatus)
        {
            // Act
            var result = _service.DetermineStatus(customerName);

            // Assert
            result.Should().Be(expectedStatus);
        }
    }
}
