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
        public BookingServiceImpl(BookingContext dbContext, IProducer<Null, string> kafkaProducer)
        {
            _dbContext = dbContext;
            _kafkaProducer = kafkaProducer;
        }
        public async Task<BookingResponse> CreateBookingAsync(BookingRequest request)
        {
            var tempId = Guid.NewGuid().ToString();
            var provisional = new BookingDB.Models.Booking
            {
                Id = tempId,
                UserId = request.UserId,
                HotelId = request.HotelId,
                PromoCode = request.PromoCode ?? string.Empty,
                Status = "pending",  // Добавлено
                Price = 100.0,  // Default calculate later
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Bookings.Add(provisional);
            await _dbContext.SaveChangesAsync();

            // Publish validation request (добавьте топик в kafka-init compose)
            var validationRequest = new { CorrelationId = tempId, BookingId = tempId, UserId = request.UserId, HotelId = request.HotelId, PromoCode = request.PromoCode ?? "" };
            await _kafkaProducer.ProduceAsync("booking-validation-request", new Message<Null, string> { Value = JsonSerializer.Serialize(validationRequest) });

            // Для теста: Симулируйте valid results и produce confirmed (в prod — удалите)
            await SimulateValidationAndConfirm(tempId, request, provisional);

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

        private async Task SimulateValidationAndConfirm(string correlationId, BookingRequest request, BookingDB.Models.Booking booking)
        {
            // Симуляция: All valid, VIP user, 20% discount from promo
            var userResult = new { CorrelationId = correlationId, BookingId = booking.Id, Active = true, Blacklisted = false, Status = "VIP" };
            var hotelResult = new { CorrelationId = correlationId, BookingId = booking.Id, Operational = true, FullyBooked = false };
            var promoResult = new { CorrelationId = correlationId, BookingId = booking.Id, Valid = true, Discount = 20.0 };
            var reviewResult = new { CorrelationId = correlationId, BookingId = booking.Id, ReviewScoreOk = true };

            // Produce results (симуляция other services)
            await _kafkaProducer.ProduceAsync("user-validation-result", new Message<Null, string> { Value = JsonSerializer.Serialize(userResult) });
            await _kafkaProducer.ProduceAsync("hotel-validation-result", new Message<Null, string> { Value = JsonSerializer.Serialize(hotelResult) });
            await _kafkaProducer.ProduceAsync("promo-validation-result", new Message<Null, string> { Value = JsonSerializer.Serialize(promoResult) });
            await _kafkaProducer.ProduceAsync("review-validation-result", new Message<Null, string> { Value = JsonSerializer.Serialize(reviewResult) });

            // Ждём process (hack для теста; в prod — saga/callback)
            await Task.Delay(2000);  // 2s для consumer process

            // Update booking (после validation)
            booking.Status = "confirmed";
            booking.Price = 80.0 * (1 - 0.20);  // VIP base 80 - 20%
            booking.DiscountPercent = 20.0;
            await _dbContext.SaveChangesAsync();

            // Produce confirmed
            await _kafkaProducer.ProduceAsync("booking-confirmed", new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(new
                {
                    BookingId = booking.Id,
                    FinalPrice = booking.Price,
                    UserId = booking.UserId,
                    HotelId = booking.HotelId
                })
            });
        }
    }
}
