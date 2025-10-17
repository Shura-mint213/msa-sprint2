using booking_service.Implementation;
using booking_service.Interfaces;
using booking_service.Services;
using booking_service.ServicesGrpc;
using BookingDB;
using Confluent.Kafka;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Настройка Kestrel: gRPC и HTTP
builder.WebHost.ConfigureKestrel(options =>
{
    // gRPC listener (HTTP/2)
    options.ListenAnyIP(8080, o => o.Protocols = HttpProtocols.Http2);

    // HTTP listener (healthcheck, index)
    options.ListenAnyIP(8081, o => o.Protocols = HttpProtocols.Http1);
});

// Подключение к Postgres
builder.Services.AddDbContext<BookingContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Kafka producer
builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "kafka:29092",
        Acks = Acks.All
    };
    return new ProducerBuilder<Null, string>(config).Build();
});


// gRPC
builder.Services.AddGrpc();

// DI
builder.Services.AddTransient<BookingValidationService>();
builder.Services.AddTransient<IBookingServiceImpl, BookingServiceImpl>();

//builder.Services.AddHostedService<KafkaConsumerService>();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<BookingContext>();
    try
    {
        logger.LogInformation("Applying migrations...");
        db.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed!");
        Environment.Exit(1);
    }
}

// Маршруты
app.MapGrpcService<BookingServiceGrpc>();
//app.MapGet("/", () => "Use gRPC client to access this service.");
app.MapGet("/health", () => Results.Ok("Healthy"));

// Запуск
app.Run();
