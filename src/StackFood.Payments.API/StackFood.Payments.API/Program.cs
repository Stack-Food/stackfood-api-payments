using Amazon.DynamoDBv2;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using StackFood.Payments.Application.Interfaces;
using StackFood.Payments.Application.UseCases.CreatePayment;
using StackFood.Payments.Infrastructure.Messaging;
using StackFood.Payments.Infrastructure.Repositories;
using StackFood.Payments.Infrastructure.Services;
using System.Diagnostics.CodeAnalysis;

namespace StackFood.Payments.API
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Health Checks
            builder.Services.AddHealthChecks();

            // AWS Configuration
            var awsOptions = builder.Configuration.GetAWSOptions();
            builder.Services.AddDefaultAWSOptions(awsOptions);
            builder.Services.AddAWSService<IAmazonDynamoDB>();
            builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
            builder.Services.AddAWSService<IAmazonSQS>();

            // Environment Variables
            var dynamoDbTableName = builder.Configuration["DynamoDB:TableName"] ?? "Payments";
            var snsTopicArn = builder.Configuration["AWS:SNS:TopicArn"] ?? string.Empty;
            var sqsQueueUrl = builder.Configuration["AWS:SQS:QueueUrl"] ?? string.Empty;

            // Repositories
            builder.Services.AddScoped<IPaymentRepository>(sp =>
            {
                var dynamoDb = sp.GetRequiredService<IAmazonDynamoDB>();
                return new PaymentRepository(dynamoDb, dynamoDbTableName);
            });

            // Services
            builder.Services.AddScoped<IEventPublisher, SnsEventPublisher>();
            builder.Services.AddSingleton<IFakeCheckoutService, FakeCheckoutService>();

            // Use Cases
            builder.Services.AddScoped(sp =>
            {
                var paymentRepository = sp.GetRequiredService<IPaymentRepository>();
                var eventPublisher = sp.GetRequiredService<IEventPublisher>();
                var fakeCheckoutService = sp.GetRequiredService<IFakeCheckoutService>();
                return new CreatePaymentUseCase(paymentRepository, eventPublisher, fakeCheckoutService, snsTopicArn);
            });

            // SQS Consumer
            builder.Services.AddSingleton<IOrderQueueConsumer>(sp =>
            {
                var sqsClient = sp.GetRequiredService<IAmazonSQS>();
                var logger = sp.GetRequiredService<ILogger<OrderQueueConsumer>>();
                return new OrderQueueConsumer(sqsClient, sqsQueueUrl, logger, sp);
            });

            // Background Service
            builder.Services.AddHostedService<OrderQueueConsumerBackgroundService>();

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            app.UsePathBase("/payments");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/payments/swagger/v1/swagger.json", "StackFood payments API v1");
            });

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthorization();
            app.MapControllers();

            // Health Check Endpoint
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
