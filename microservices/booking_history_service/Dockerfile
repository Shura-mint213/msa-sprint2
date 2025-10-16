# === 1. Runtime base image ===
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Добавляем скрипт ожидания (под root)
COPY scripts/wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

# После того как права выставлены — можно безопасно перейти под непривилегированного пользователя
USER $APP_UID


# === 2. Build stage ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY ["booking_history_service.csproj", "."]
RUN dotnet restore "./booking_history_service.csproj"

# Копируем остальной код и собираем
COPY . .
RUN dotnet build "./booking_history_service.csproj" -c $BUILD_CONFIGURATION -o /app/build


# === 3. Publish stage ===
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./booking_history_service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# === 4. Final runtime stage ===
FROM base AS final
WORKDIR /app

# Копируем собранный результат из publish stage
COPY --from=publish /app/publish .

# Указываем точку входа
ENTRYPOINT ["dotnet", "booking-history-service.dll"]
