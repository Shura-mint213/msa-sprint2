namespace booking_service.Interfaces
{
    public interface IBookingServiceImpl
    {
        Task<BookingResponse> CreateBookingAsync(BookingRequest request);
        Task<BookingListResponse> ListBookingsAsync(BookingListRequest request);
    }
}
