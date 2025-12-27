using Microsoft.Extensions.Hosting;
using StackFood.Payments.Application.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace StackFood.Payments.Infrastructure.Services;

[ExcludeFromCodeCoverage]
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
