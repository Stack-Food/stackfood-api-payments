namespace StackFood.Payments.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T eventData, string topicArn) where T : class;
}
