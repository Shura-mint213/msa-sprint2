# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY booking-service/booking-service.csproj booking-service/
COPY BookingDB/BookingDB.csproj BookingDB/
RUN dotnet restore booking-service/booking-service.csproj
COPY . .
WORKDIR /src/booking-service
RUN dotnet publish booking-service.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "booking-service.dll"]