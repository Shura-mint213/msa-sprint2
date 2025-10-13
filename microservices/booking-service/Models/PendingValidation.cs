namespace booking_service.Models
{
    public class PendingValidation
    {
        public string BookingId { get; set; }
        public string CorrelationId { get; set; }

        // User
        public bool? UserActive { get; set; }
        public bool? UserBlacklisted { get; set; }
        public string? UserStatus { get; set; }

        // Hotel
        public bool? Operational { get; set; }
        public bool? FullyBooked { get; set; }

        // Promo
        public bool? PromoValid { get; set; }
        public double? PromoDiscount { get; set; }
        public bool? PromoVipOnly { get; set; }

        // Review
        public bool? ReviewTrusted { get; set; }

        public bool AllValid()
        {
            return UserActive.HasValue && UserBlacklisted.HasValue && UserStatus != null &&
                   Operational.HasValue && FullyBooked.HasValue &&
                   PromoValid.HasValue && PromoDiscount.HasValue && PromoVipOnly.HasValue &&
                   ReviewTrusted.HasValue &&
                   // Business logic: user is active, not blacklisted, hotel operational, not fully booked, promo valid if applicable, review trusted
                   UserActive.Value && !UserBlacklisted.Value &&
                   Operational.Value && !FullyBooked.Value &&
                   (!PromoValid.Value || !PromoVipOnly.Value || UserStatus == "VIP") && // VIP-only promo check
                   ReviewTrusted.Value;
        }

        public bool IsComplete() =>
            UserActive.HasValue &&
            UserBlacklisted.HasValue &&
            Operational.HasValue &&
            FullyBooked.HasValue &&
            PromoValid.HasValue &&
            ReviewTrusted.HasValue;

        public string FailureReason()
        {
            if (UserBlacklisted == true) return "user_blacklisted";
            if (Operational == false) return "hotel_not_operational";
            if (FullyBooked == true) return "hotel_fully_booked";
            if (PromoValid == false) return "invalid_promo";
            if (ReviewTrusted == false) return "bad_reviews";
            return "unknown";
        }

    }
}
