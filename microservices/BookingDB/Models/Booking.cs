namespace BookingDB.Models
{
    /// <summary>
    /// Модель бронирования отеля
    /// </summary>
    public class Booking
    {
        /// <summary>
        /// Уникальный идентификатор созданного бронирования
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Идентификатор пользователя, сделавшего бронирование
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Идентификатор отеля, который был забронирован
        /// </summary>
        public string HotelId { get; set; }

        /// <summary>
        /// Промокод, применённый к бронированию
        /// </summary>
        public string PromoCode { get; set; }

        /// <summary>
        /// Процент скидки, применённой к бронированию
        /// </summary>
        public double DiscountPercent { get; set; }

        /// <summary>
        /// Финальная цена бронирования после применения скидки
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Статус броно
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Дата и время создания бронирования в формате ISO-8601
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
