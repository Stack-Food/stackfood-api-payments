namespace StackFood.Payments.Application.Interfaces;

public interface IOrderQueueConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
