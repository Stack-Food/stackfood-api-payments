using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using StackFood.Payments.Application.Interfaces;
using StackFood.Payments.Domain.Entities;
using StackFood.Payments.Domain.Enums;
using System.Text.Json;

namespace StackFood.Payments.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public PaymentRepository(IAmazonDynamoDB dynamoDb, string tableName)
    {
        _dynamoDb = dynamoDb;
        _tableName = tableName;
    }

    public async Task<Payment?> GetByIdAsync(Guid paymentId)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "PaymentId", new AttributeValue { S = paymentId.ToString() } }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request);
        return response.Item.Any() ? MapToPayment(response.Item) : null;
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI_OrderId",
            KeyConditionExpression = "OrderId = :orderId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":orderId", new AttributeValue { S = orderId.ToString() } }
            }
        };

        var response = await _dynamoDb.QueryAsync(request);
        return response.Items.Any() ? MapToPayment(response.Items.First()) : null;
    }

    public async Task<IEnumerable<Payment>> GetByStatusAsync(string status)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI_Status_CreatedAt",
            KeyConditionExpression = "#status = :status",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#status", "Status" }
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":status", new AttributeValue { S = status } }
            }
        };

        var response = await _dynamoDb.QueryAsync(request);
        return response.Items.Select(MapToPayment);
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PaymentId", new AttributeValue { S = payment.PaymentId.ToString() } },
            { "OrderId", new AttributeValue { S = payment.OrderId.ToString() } },
            { "OrderNumber", new AttributeValue { S = payment.OrderNumber } },
            { "Amount", new AttributeValue { N = payment.Amount.ToString() } },
            { "Status", new AttributeValue { S = payment.Status.ToString() } },
            { "PaymentMethod", new AttributeValue { S = payment.PaymentMethod.ToString() } },
            { "CreatedAt", new AttributeValue { S = payment.CreatedAt.ToString("O") } },
            { "UpdatedAt", new AttributeValue { S = payment.UpdatedAt.ToString("O") } }
        };

        if (!string.IsNullOrEmpty(payment.CustomerName))
            item.Add("CustomerName", new AttributeValue { S = payment.CustomerName });

        if (!string.IsNullOrEmpty(payment.QRCode))
            item.Add("QRCode", new AttributeValue { S = payment.QRCode });

        if (!string.IsNullOrEmpty(payment.QRCodeUrl))
            item.Add("QRCodeUrl", new AttributeValue { S = payment.QRCodeUrl });

        if (payment.ApprovedAt.HasValue)
            item.Add("ApprovedAt", new AttributeValue { S = payment.ApprovedAt.Value.ToString("O") });

        if (payment.RejectedAt.HasValue)
            item.Add("RejectedAt", new AttributeValue { S = payment.RejectedAt.Value.ToString("O") });

        if (payment.ExpiresAt.HasValue)
            item.Add("ExpiresAt", new AttributeValue { S = payment.ExpiresAt.Value.ToString("O") });

        if (payment.Metadata != null && payment.Metadata.Any())
            item.Add("Metadata", new AttributeValue { S = JsonSerializer.Serialize(payment.Metadata) });

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await _dynamoDb.PutItemAsync(request);
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        payment.UpdatedAt = DateTime.UtcNow;
        return await CreateAsync(payment); // DynamoDB PutItem upserts
    }

    public async Task DeleteAsync(Guid paymentId)
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "PaymentId", new AttributeValue { S = paymentId.ToString() } }
            }
        };

        await _dynamoDb.DeleteItemAsync(request);
    }

    private Payment MapToPayment(Dictionary<string, AttributeValue> item)
    {
        var payment = new Payment
        {
            PaymentId = Guid.Parse(item["PaymentId"].S),
            OrderId = Guid.Parse(item["OrderId"].S),
            OrderNumber = item["OrderNumber"].S,
            Amount = decimal.Parse(item["Amount"].N),
            Status = Enum.Parse<PaymentStatus>(item["Status"].S),
            PaymentMethod = Enum.Parse<PaymentMethod>(item["PaymentMethod"].S),
            CreatedAt = DateTime.Parse(item["CreatedAt"].S),
            UpdatedAt = DateTime.Parse(item["UpdatedAt"].S)
        };

        if (item.ContainsKey("CustomerName"))
            payment.CustomerName = item["CustomerName"].S;

        if (item.ContainsKey("QRCode"))
            payment.QRCode = item["QRCode"].S;

        if (item.ContainsKey("QRCodeUrl"))
            payment.QRCodeUrl = item["QRCodeUrl"].S;

        if (item.ContainsKey("ApprovedAt"))
            payment.ApprovedAt = DateTime.Parse(item["ApprovedAt"].S);

        if (item.ContainsKey("RejectedAt"))
            payment.RejectedAt = DateTime.Parse(item["RejectedAt"].S);

        if (item.ContainsKey("ExpiresAt"))
            payment.ExpiresAt = DateTime.Parse(item["ExpiresAt"].S);

        if (item.ContainsKey("Metadata"))
            payment.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(item["Metadata"].S);

        return payment;
    }
}
