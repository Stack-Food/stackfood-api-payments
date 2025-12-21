using Microsoft.Extensions.Hosting;
using StackFood.Payments.Application.Interfaces;

namespace StackFood.Payments.Infrastructure.Services;

public class OrderQueueConsumerBackgroundService : BackgroundService
{
    private readonly IOrderQueueConsumer _consumer;

    public OrderQueueConsumerBackgroundService(IOrderQueueConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumer.StartAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
