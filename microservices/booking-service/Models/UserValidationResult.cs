namespace booking_service.Models
{
    public class UserValidationResult
    {
        public string CorrelationId { get; set; }
        public bool Active { get; set; }
        public bool Blacklisted { get; set; }
        public string Status { get; set; }
    }
}
