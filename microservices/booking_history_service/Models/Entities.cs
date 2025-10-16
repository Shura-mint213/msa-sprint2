using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

public class HistoryContext : DbContext
{
    public HistoryContext(DbContextOptions<HistoryContext> options) : base(options) { }

    public DbSet<HistoryRecord> History { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HistoryRecord>(entity =>
        {
            entity.HasKey(e => e.Id);  // Primary key
            entity.Property(e => e.Id).ValueGeneratedOnAdd();  // Автоинкремент
        });

        base.OnModelCreating(modelBuilder);
    }
}

public class HistoryRecord
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
    public long Id { get; set; }
    public string BookingId { get; set; }
    public double? FinalPrice { get; set; }
    public string UserId { get; set; }
    public string HotelId { get; set; }
    public string Status { get; set; }
    public string? Reason { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class BookingConfirmed
{
    public string BookingId { get; set; }
    public double FinalPrice { get; set; }
    public string UserId { get; set; }
    public string HotelId { get; set; }
}

public class BookingCancelled
{
    public string BookingId { get; set; }
    public string UserId { get; set; }
    public string HotelId { get; set; }
    public string Reason { get; set; }
}
