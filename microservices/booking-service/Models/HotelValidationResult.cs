namespace booking_service.Models
{
    public class HotelValidationResult
    {
        public string CorrelationId { get; set; }
        public bool Operational { get; set; }
        public bool FullyBooked { get; set; }
    }
}
