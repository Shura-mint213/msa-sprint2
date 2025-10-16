using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<HistoryContext>(options =>
            options.UseNpgsql(
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Host=history-db;Database=history;Username=history;Password=history"));

        services.AddHostedService<KafkaHistoryConsumerService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    });

var host = builder.Build();

// Async миграция с логами
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<HistoryContext>();
    try
    {
        logger.LogInformation("Applying migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed!");
        Environment.Exit(1);  // Краш при ошибке
    }
}

await host.RunAsync();