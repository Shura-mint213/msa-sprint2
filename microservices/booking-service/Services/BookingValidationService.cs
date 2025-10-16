using booking_service.Models;
using BookingDB;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;

namespace booking_service.Services
{
    public class BookingValidationService
    {
        private readonly BookingContext _dbContext;
        private readonly IProducer<Null, string> _kafkaProducer;
        private readonly ConcurrentDictionary<string, PendingValidation> _pendingValidations = new();  // Aggregate in memory (or DB)

        public BookingValidationService(BookingContext dbContext, IProducer<Null, string> kafkaProducer)
        {
            _dbContext = dbContext;
            _kafkaProducer = kafkaProducer;
        }

        public async Task HandleUserValidation(UserValidationResult message)
        {
            var pending = _pendingValidations.GetOrAdd(message.CorrelationId, _ => new PendingValidation { CorrelationId = message.CorrelationId });
            pending.UserActive = message.Active;
            pending.UserBlacklisted = message.Blacklisted;
            pending.UserStatus = message.Status;

            await ProcessPending(pending);
        }

        public async Task HandleHotelValidation(HotelValidationResult message)
        {
            var pending = _pendingValidations.GetOrAdd(message.CorrelationId, _ => new PendingValidation { CorrelationId = message.CorrelationId });
            pending.Operational = message.Operational;
            pending.FullyBooked = message.FullyBooked;

            await ProcessPending(pending);
        }

        public async Task HandlePromoValidation(PromoValidationResult message)
        {
            var pending = _pendingValidations.GetOrAdd(message.CorrelationId, _ => new PendingValidation { CorrelationId = message.CorrelationId });
            pending.PromoValid = message.Valid;
            pending.PromoDiscount = message.Discount ?? 0.0;

            await ProcessPending(pending);
        }

        public async Task HandleReviewValidation(ReviewValidationResult message)
        {
            var pending = _pendingValidations.GetOrAdd(message.CorrelationId, _ => new PendingValidation { CorrelationId = message.CorrelationId });
            pending.ReviewTrusted = message.ReviewScoreOk;

            await ProcessPending(pending);
        }

        private async Task ProcessPending(PendingValidation pending)
        {
            // Проверяем, все ли проверки завершены
            if (!pending.IsComplete()) return;

            if (pending.AllValid())
            {
                var booking = await _dbContext.Bookings.FindAsync(pending.BookingId);
                if (booking == null) return;

                var basePrice = pending.UserStatus == "VIP" ? 80.0 : 100.0;
                var discount = pending.PromoDiscount ?? 0.0;

                booking.Status = "confirmed";
                booking.DiscountPercent = discount;
                booking.Price = basePrice - discount;

                await _dbContext.SaveChangesAsync();

                await _kafkaProducer.ProduceAsync("booking-confirmed", new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(new
                    {
                        BookingId = pending.BookingId,
                        FinalPrice = booking.Price,
                        UserId = booking.UserId,
                        HotelId = booking.HotelId
                    })
                });
                _pendingValidations.TryRemove(pending.CorrelationId, out _);
            }
            else
            {
                var booking = await _dbContext.Bookings.FindAsync(pending.BookingId);
                if (booking == null) return;

                await _kafkaProducer.ProduceAsync("booking-cancelled", new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(new
                    {
                        BookingId = pending.BookingId,
                        Reason = pending.FailureReason(),
                        UserId = booking.UserId,    
                        HotelId = booking.HotelId   
                    })
                });

                _pendingValidations.TryRemove(pending.CorrelationId, out _);
                _dbContext.Bookings.Remove(booking);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
