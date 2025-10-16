using booking_client;
using Grpc.Net.Client;

//AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

using var channel = GrpcChannel.ForAddress("http://localhost:9090");

// ✅ это имя генерируется автоматически из service Booking
var client = new Booking.BookingClient(channel);

var response = await client.CreateBookingAsync(new BookingRequest
{
    UserId = "User-2113",
    HotelId = "Hotel-1"
});

Console.WriteLine($"Booking created: {response.Id}, Price: {response.Price}");
