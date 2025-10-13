using booking_service.Extensions;
using booking_service.Interfaces;
using BookingDB;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace booking_service.Implementation
{
    public class BookingServiceImpl : IBookingServiceImpl
    {
        private readonly BookingContext _dbContext;
        private readonly IProducer<Null, string> _kafkaProducer;
        public async Task<BookingResponse> CreateBookingAsync(BookingRequest request)
        {
            var tempId = Guid.NewGuid().ToString();
            var provisional = new BookingDB.Models.Booking
            {
                Id = tempId,
                UserId = request.UserId,
                HotelId = request.HotelId,
                PromoCode = request.PromoCode ?? string.Empty,
                Status = "pending",
                Price = 100.0,  // Default, calculate later
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Bookings.Add(provisional);
            await _dbContext.SaveChangesAsync();

            // Publish event for async validation
            var validationRequest = new { CorrelationId = tempId, UserId = request.UserId, HotelId = request.HotelId, PromoCode = request.PromoCode ?? "" };
            await _kafkaProducer.ProduceAsync("booking-validation-request", new Message<Null, string> { Value = JsonSerializer.Serialize(validationRequest) });

            return provisional.Convert();
        }

        public async Task<BookingListResponse> ListBookingsAsync(BookingListRequest request)
        {
            var query = _dbContext.Bookings.AsQueryable();

            // Фильтрация по UserId (если указан)
            if (!string.IsNullOrEmpty(request.UserId))
            {
                query = query.Where(b => b.UserId == request.UserId);
            }

            var bookings = await query.ToListAsync();

            var response = new BookingListResponse();
            response.Bookings.AddRange(bookings.Select(b => b.Convert()));

            return response;
        }
    }
}
