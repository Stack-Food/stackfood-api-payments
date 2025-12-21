using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackFood.Payments.Application.DTOs;
using StackFood.Payments.Application.Interfaces;
using StackFood.Payments.Application.UseCases.CreatePayment;
using StackFood.Payments.Domain.Events;
using System.Text.Json;

namespace StackFood.Payments.Infrastructure.Services;

public class OrderQueueConsumer : IOrderQueueConsumer
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly ILogger<OrderQueueConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _executingTask;

    public OrderQueueConsumer(
        IAmazonSQS sqsClient,
        string queueUrl,
        ILogger<OrderQueueConsumer> logger,
        IServiceProvider serviceProvider)
    {
        _sqsClient = sqsClient;
        _queueUrl = queueUrl;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order Queue Consumer starting...");
        _logger.LogInformation("Queue URL: {QueueUrl}", _queueUrl);

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_cancellationTokenSource.Token);

        if (_executingTask.IsCompleted)
        {
            return _executingTask;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null)
        {
            return;
        }

        try
        {
            _cancellationTokenSource?.Cancel();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Queue Consumer started and listening for messages...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20, // Long polling
                    MessageSystemAttributeNames = new List<string> { "All" },
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                if (response?.Messages?.Any() == true)
                {
                    _logger.LogInformation("Received {Count} messages from queue", response.Messages.Count);

                    foreach (var message in response.Messages)
                    {
                        await ProcessMessageAsync(message, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming messages from SQS queue");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Order Queue Consumer stopped");
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing message: {MessageId}", message.MessageId);

            // SNS wraps the message in a JSON envelope
            var snsMessage = JsonSerializer.Deserialize<SnsMessageWrapper>(message.Body);

            if (snsMessage?.Message == null)
            {
                _logger.LogWarning("Invalid SNS message format");
                await DeleteMessageAsync(message);
                return;
            }

            // Deserialize the actual event
            var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(snsMessage.Message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (orderCreatedEvent == null)
            {
                _logger.LogWarning("Failed to deserialize OrderCreatedEvent");
                await DeleteMessageAsync(message);
                return;
            }

            _logger.LogInformation("OrderCreated event received for OrderId: {OrderId}", orderCreatedEvent.OrderId);

            // Process payment using CreatePaymentUseCase
            using var scope = _serviceProvider.CreateScope();
            var createPaymentUseCase = scope.ServiceProvider.GetRequiredService<CreatePaymentUseCase>();

            var request = new CreatePaymentRequest
            {
                OrderId = orderCreatedEvent.OrderId,
                OrderNumber = orderCreatedEvent.OrderNumber,
                Amount = orderCreatedEvent.TotalAmount,
                CustomerName = orderCreatedEvent.CustomerName
            };

            var payment = await createPaymentUseCase.ExecuteAsync(request);

            _logger.LogInformation("Payment created successfully: PaymentId={PaymentId}, Status={Status}",
                payment.PaymentId, payment.Status);

            // Delete message from queue after successful processing
            await DeleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}", message.MessageId);
            // Don't delete message on error - it will be retried
        }
    }

    private async Task DeleteMessageAsync(Message message)
    {
        try
        {
            await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle);
            _logger.LogInformation("Message deleted from queue: {MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", message.MessageId);
        }
    }

    // Helper class to deserialize SNS message wrapper
    private class SnsMessageWrapper
    {
        public string? Message { get; set; }
        public string? MessageId { get; set; }
        public string? TopicArn { get; set; }
        public string? Type { get; set; }
    }
}
