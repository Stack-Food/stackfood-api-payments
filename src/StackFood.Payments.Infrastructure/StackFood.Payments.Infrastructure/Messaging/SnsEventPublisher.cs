using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using StackFood.Payments.Application.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace StackFood.Payments.Infrastructure.Messaging;

[ExcludeFromCodeCoverage]
public class SnsEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;

    public SnsEventPublisher(IAmazonSimpleNotificationService sns)
    {
        _sns = sns;
    }

    public async Task PublishAsync<T>(T eventData, string topicArn) where T : class
    {
        var message = JsonSerializer.Serialize(eventData);

        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = message,
            Subject = eventData.GetType().Name
        };

        await _sns.PublishAsync(request);
    }
}
