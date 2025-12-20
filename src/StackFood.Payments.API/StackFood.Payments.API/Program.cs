using Amazon.DynamoDBv2;
using Amazon.SimpleNotificationService;
using StackFood.Payments.Application.Interfaces;
using StackFood.Payments.Application.UseCases.CreatePayment;
using StackFood.Payments.Infrastructure.Messaging;
using StackFood.Payments.Infrastructure.Repositories;
using StackFood.Payments.Infrastructure.Services;
using System.Diagnostics.CodeAnalysis;

namespace StackFood.Payments.API
{
    public class Program
    {
        [ExcludeFromCodeCoverage]
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // AWS Configuration
            var awsOptions = builder.Configuration.GetAWSOptions();
            builder.Services.AddDefaultAWSOptions(awsOptions);
            builder.Services.AddAWSService<IAmazonDynamoDB>();
            builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

            // Environment Variables
            var dynamoDbTableName = builder.Configuration["DynamoDB:TableName"] ?? "Payments";
            var snsTopicArn = builder.Configuration["AWS:SnsTopicArn"] ?? string.Empty;

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
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}