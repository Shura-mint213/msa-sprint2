using booking_service.Models;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
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
                BootstrapServers = Environment.GetEnvironmentVariable("Kafka__BootstrapServers") ?? "kafka:29092",
                GroupId = "booking-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaConsumerService starting...");
            _logger.LogInformation($"Connecting to Kafka at: {_config.BootstrapServers}");

            using var consumer = new ConsumerBuilder<Ignore, string>(_config).Build();
            var topics = new[] { "user-validation-result", "hotel-validation-result", "promo-validation-result", "review-validation-result" };

            int retryCount = 0;
            const int maxRetries = 10;

            while (retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    consumer.Subscribe(topics);
                    _logger.LogInformation($"Subscribed to topics: {string.Join(", ", topics)}");

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var cr = consumer.Consume(stoppingToken);
                            if (cr?.Message == null) continue;

                            _logger.LogInformation($"Received from {cr.Topic}: {cr.Message.Value}");

                            using var scope = _serviceProvider.CreateScope();
                            var bookingValidation = scope.ServiceProvider.GetRequiredService<BookingValidationService>();

                            switch (cr.Topic)
                            {
                                case "user-validation-result":
                                    var userMsg = JsonSerializer.Deserialize<UserValidationResult>(cr.Message.Value);
                                    await bookingValidation.HandleUserValidation(userMsg!);
                                    break;
                                case "hotel-validation-result":
                                    var hotelMsg = JsonSerializer.Deserialize<HotelValidationResult>(cr.Message.Value);
                                    await bookingValidation.HandleHotelValidation(hotelMsg!);
                                    break;
                                case "promo-validation-result":
                                    var promoMsg = JsonSerializer.Deserialize<PromoValidationResult>(cr.Message.Value);
                                    await bookingValidation.HandlePromoValidation(promoMsg!);
                                    break;
                                case "review-validation-result":
                                    var reviewMsg = JsonSerializer.Deserialize<ReviewValidationResult>(cr.Message.Value);
                                    await bookingValidation.HandleReviewValidation(reviewMsg!);
                                    break;
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, "Kafka consume error");
                            await Task.Delay(2000, stoppingToken);
                        }
                    }

                    consumer.Close();
                    return;
                }
                catch (KafkaException ex)
                {
                    retryCount++;
                    _logger.LogWarning($"Kafka connection failed (attempt {retryCount}/{maxRetries}): {ex.Message}");
                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError(ex, $"Unexpected error when connecting to Kafka (attempt {retryCount}/{maxRetries})");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogError("KafkaConsumerService failed to connect after multiple retries. Stopping service.");

        }        
    }
}
