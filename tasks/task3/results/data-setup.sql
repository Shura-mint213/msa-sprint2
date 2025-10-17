-- Insert test hotel into monolith DB (hotelio-db)
INSERT INTO "hotel" ("id", "city", "description", "operational", "fully_booked", "rating")
VALUES ('test-hotel-1', 'Moscow', 'Test Hotel Moscow', true, false, 4.5);

-- Insert test booking into booking-service DB (booking-db)
INSERT INTO "Bookings" ("Id", "UserId", "HotelId", "PromoCode", "DiscountPercent", "Price", "Status", "CreatedAt")
VALUES ('test-booking-1', 'user1', 'test-hotel-1', 'SUMMER20', 20, 64.0, 'confirmed', NOW());