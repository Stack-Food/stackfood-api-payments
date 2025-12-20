using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Moq;
using StackFood.Payments.Domain.Entities;
using StackFood.Payments.Domain.Enums;
using StackFood.Payments.Infrastructure.Repositories;

namespace StackFood.Payments.Tests.Repositories
{
    public class PaymentRepositoryTests
    {
        private readonly Mock<IAmazonDynamoDB> _dynamoDbMock;
        private readonly PaymentRepository _repository;
        private readonly string _tableName = "PaymentsTable";

        public PaymentRepositoryTests()
        {
            _dynamoDbMock = new Mock<IAmazonDynamoDB>();
            _repository = new PaymentRepository(_dynamoDbMock.Object, _tableName);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnPayment_WhenItemExists()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var item = new Dictionary<string, AttributeValue>
            {
                { "PaymentId", new AttributeValue { S = paymentId.ToString() } },
                { "OrderId", new AttributeValue { S = Guid.NewGuid().ToString() } },
                { "OrderNumber", new AttributeValue { S = "123" } },
                { "Amount", new AttributeValue { N = "100.50" } },
                { "Status", new AttributeValue { S = PaymentStatus.Pending.ToString() } },
                { "PaymentMethod", new AttributeValue { S = PaymentMethod.QRCode.ToString() } },
                { "CreatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("O") } },
                { "UpdatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };
            _dynamoDbMock.Setup(x => x.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetItemResponse { Item = item });

            // Act
            var result = await _repository.GetByIdAsync(paymentId);

            // Assert
            result.Should().NotBeNull();
            result!.PaymentId.Should().Be(paymentId);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenItemDoesNotExist()
        {
            // Arrange
            _dynamoDbMock.Setup(x => x.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetItemResponse { Item = new Dictionary<string, AttributeValue>() });

            // Act
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByOrderIdAsync_ShouldReturnPayment_WhenItemExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var item = new Dictionary<string, AttributeValue>
            {
                { "PaymentId", new AttributeValue { S = Guid.NewGuid().ToString() } },
                { "OrderId", new AttributeValue { S = orderId.ToString() } },
                { "OrderNumber", new AttributeValue { S = "123" } },
                { "Amount", new AttributeValue { N = "100.50" } },
                { "Status", new AttributeValue { S = PaymentStatus.Pending.ToString() } },
                { "PaymentMethod", new AttributeValue { S = PaymentMethod.QRCode.ToString() } },
                { "CreatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("O") } },
                { "UpdatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };
            _dynamoDbMock.Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResponse { Items = new List<Dictionary<string, AttributeValue>> { item } });

            // Act
            var result = await _repository.GetByOrderIdAsync(orderId);

            // Assert
            result.Should().NotBeNull();
            result!.OrderId.Should().Be(orderId);
        }

        [Fact]
        public async Task GetByOrderIdAsync_ShouldReturnNull_WhenNoItems()
        {
            // Arrange
            _dynamoDbMock.Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResponse { Items = new List<Dictionary<string, AttributeValue>>() });

            // Act
            var result = await _repository.GetByOrderIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStatusAsync_ShouldReturnPayments()
        {
            // Arrange
            var status = PaymentStatus.Pending.ToString();
            var item = new Dictionary<string, AttributeValue>
            {
                { "PaymentId", new AttributeValue { S = Guid.NewGuid().ToString() } },
                { "OrderId", new AttributeValue { S = Guid.NewGuid().ToString() } },
                { "OrderNumber", new AttributeValue { S = "123" } },
                { "Amount", new AttributeValue { N = "100.50" } },
                { "Status", new AttributeValue { S = status } },
                { "PaymentMethod", new AttributeValue { S = PaymentMethod.QRCode.ToString() } },
                { "CreatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("O") } },
                { "UpdatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };
            _dynamoDbMock.Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryResponse { Items = new List<Dictionary<string, AttributeValue>> { item } });

            // Act
            var result = await _repository.GetByStatusAsync(status);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.First().Status.ToString().Should().Be(status);
        }

        [Fact]
        public async Task CreateAsync_ShouldCallPutItemAsync_AndReturnPayment()
        {
            // Arrange
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                OrderNumber = "123",
                Amount = 100.50m,
                Status = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.QRCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dynamoDbMock.Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutItemResponse());

            // Act
            var result = await _repository.CreateAsync(payment);

            // Assert
            result.Should().BeEquivalentTo(payment);
            _dynamoDbMock.Verify(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateUpdatedAt_AndCallPutItemAsync()
        {
            // Arrange
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                OrderNumber = "123",
                Amount = 100.50m,
                Status = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.QRCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            _dynamoDbMock.Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutItemResponse());

            // Act
            var result = await _repository.UpdateAsync(payment);

            // Assert
            result.UpdatedAt.Should().BeAfter(payment.CreatedAt);
            _dynamoDbMock.Verify(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldCallDeleteItemAsync()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            _dynamoDbMock.Setup(x => x.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteItemResponse());

            // Act
            await _repository.DeleteAsync(paymentId);

            // Assert
            _dynamoDbMock.Verify(x => x.DeleteItemAsync(It.Is<DeleteItemRequest>(r =>
                r.Key["PaymentId"].S == paymentId.ToString()), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
