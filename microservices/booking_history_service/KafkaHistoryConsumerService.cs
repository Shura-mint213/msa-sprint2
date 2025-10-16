using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class KafkaHistoryConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaHistoryConsumerService> _logger;
    private readonly IConsumer<Ignore, string> _consumer;

    public KafkaHistoryConsumerService(IServiceProvider serviceProvider, ILogger<KafkaHistoryConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _consumer = new ConsumerBuilder<Ignore, string>(new ConsumerConfig
        {
            BootstrapServers = Environment.GetEnvironmentVariable("Kafka__BootstrapServers") ?? "kafka:9092",
            GroupId = "history-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(new[] { "booking-confirmed", "booking-cancelled" });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = _consumer.Consume(stoppingToken);
                _logger.LogInformation($"Received from {cr.Topic}: {cr.Message.Value}");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<HistoryContext>();
                HistoryRecord record;
                if (cr.Topic == "booking-confirmed")
                {
                    var booking = JsonSerializer.Deserialize<BookingConfirmed>(cr.Message.Value);
                    record = new HistoryRecord
                    {
                        BookingId = booking.BookingId,
                        UserId = booking.UserId,
                        HotelId = booking.HotelId,
                        FinalPrice = booking.FinalPrice,
                        Status = "confirmed",
                        RecordedAt = DateTime.UtcNow
                    };
                }
                else if (cr.Topic == "booking-cancelled")
                {
                    var booking = JsonSerializer.Deserialize<BookingCancelled>(cr.Message.Value);
                    record = new HistoryRecord
                    {
                        BookingId = booking.BookingId,
                        UserId = booking.UserId,
                        HotelId = booking.HotelId,
                        Status = "cancelled",
                        Reason = booking.Reason,
                        RecordedAt = DateTime.UtcNow
                    };
                }
                else continue;

                dbContext.History.Add(record);
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Saved record with Id: {record.Id}");  // Лог long Id
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming Kafka message");
            }
        }

        _consumer.Close();
    }
}
