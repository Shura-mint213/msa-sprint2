using booking_service.Interfaces;
using Grpc.Core;

namespace booking_service.Services
{
    public class BookingService : Booking.BookingBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly IBookingServiceImpl _bookingServiceImpl;
        public BookingService(ILogger<GreeterService> logger,
            IBookingServiceImpl bookingServiceImpl)
        {
            _logger = logger;
            _bookingServiceImpl = bookingServiceImpl;
        }

        public override Task<BookingResponse> CreateBooking(BookingRequest request, ServerCallContext context)
        {
            return _bookingServiceImpl.CreateBookingAsync(request);
        }

        public override Task<BookingListResponse> ListBookings(BookingListRequest request, ServerCallContext context)
        {
            return _bookingServiceImpl.ListBookingsAsync(request);
        }
    }
}
