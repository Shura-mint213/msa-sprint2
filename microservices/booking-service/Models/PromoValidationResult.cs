namespace booking_service.Models
{
    public class PromoValidationResult
    {
        public string CorrelationId { get; set; }
        public bool Valid { get; set; }
        public double? Discount { get; set; }
    }
}
