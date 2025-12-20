using System.Diagnostics.CodeAnalysis;

namespace StackFood.Payments.Worker
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        private static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}