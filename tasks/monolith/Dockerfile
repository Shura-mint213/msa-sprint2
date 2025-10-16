# Базовый образ с Java 17
FROM openjdk:17-jdk-slim

# Рабочая директория внутри контейнера
WORKDIR /app

# Копируем только jar-файл
COPY hotelio-monolith-1.0.0.jar app.jar

# Указываем порт
EXPOSE 8080

# Команда запуска
ENTRYPOINT ["java", "-jar", "app.jar"]
