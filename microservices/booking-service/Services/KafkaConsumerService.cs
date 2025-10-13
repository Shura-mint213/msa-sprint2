using booking_service.Models;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json;

namespace booking_service.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly ConsumerConfig _config;

        public KafkaConsumerService(IServiceProvider serviceProvider, ILogger<KafkaConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _config = new ConsumerConfig
            {
                BootstrapServers = "kafka:9092",
                GroupId = "booking-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var consumer = new ConsumerBuilder<Ignore, string>(_config).Build();
            consumer.Subscribe(new[] { "user-validation-result", "hotel-validation-result" });

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    _logger.LogInformation($"Received from {cr.Topic}: {cr.Message.Value}");

                    using var scope = _serviceProvider.CreateScope();
                    var bookingValidation = scope.ServiceProvider.GetRequiredService<BookingValidationService>();

                    switch (cr.Topic)
                    {
                        case "user-validation-result":
                            var userMsg = JsonSerializer.Deserialize<UserValidationResult>(cr.Message.Value);
                            await bookingValidation.HandleUserValidation(userMsg);
                            break;

                        case "hotel-validation-result":
                            var hotelMsg = JsonSerializer.Deserialize<HotelValidationResult>(cr.Message.Value);
                            await bookingValidation.HandleHotelValidation(hotelMsg);
                            break;
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error while consuming Kafka");
                }
            }

            consumer.Close();
        }
    }
}
