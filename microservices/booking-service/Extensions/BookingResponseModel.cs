namespace booking_service.Extensions
{
    public static class BookingResponseModel
    {
        public static BookingResponse Convert(this BookingDB.Models.Booking booking)
        {
            return new BookingResponse
            {
                Id = booking.Id,
                DiscountPercent = booking.DiscountPercent,
                HotelId = booking.HotelId,
                PromoCode = booking.PromoCode,
                Price = booking.Price,
                CreatedAt = booking.CreatedAt.ToString(),
                UserId = booking.UserId,
            };
        }
    }
}
