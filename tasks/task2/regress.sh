#!/bin/bash
# 🧪 Регрессионные тесты booking-service и booking-history-service

# Цвета и иконки
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'
CHECK="✅"
CROSS="❌"
WARN="⚠️"
ARROW="➡️"
CLOCK="⏳"
DB="🗄️"
LOGS="🪵"
KAFKA="📡"

log_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}${CHECK} $2${NC}"
    else
        echo -e "${RED}${CROSS} $2${NC}"
        echo -e "${YELLOW}${WARN} Подробности:${NC}"
        cat /tmp/last_error.txt 2>/dev/null
        exit 1
    fi
}

# Функция проверки готовности БД через контейнер
check_db_connection() {
    local CONTAINER=$1
    local USER=$2
    local NAME=$3

    echo "🧪 Проверка готовности базы данных ${NAME} в контейнере ${CONTAINER}..."
    for i in {1..10}; do
        docker exec -i "$CONTAINER" pg_isready -U "$USER" -d "$NAME" >/dev/null 2>&1 && break
        echo "⏳ База ${NAME} не готова, пробуем ещё раз..."
        sleep 5
    done

    docker exec -i "$CONTAINER" pg_isready -U "$USER" -d "$NAME" >/dev/null 2>&1 || {
        echo "❌ База ${NAME} в контейнере ${CONTAINER} не готова"
        exit 1
    }

    echo "✅ База ${NAME} готова"
}

# Функция загрузки фикстур через контейнер
load_fixtures() {
    local CONTAINER=$1
    local USER=$2
    local NAME=$3
    local FILE=$4

    echo "🧪 Загрузка тестовых данных (fixtures) в ${NAME} через контейнер ${CONTAINER}..."
    docker exec -i "$CONTAINER" psql -U "$USER" -d "$NAME" < "$FILE" \
      && echo "✅ Фикстуры успешно загружены" \
      || { echo "❌ Ошибка при загрузке фикстур"; exit 1; }
}

# -------------------
# Основной блок
# -------------------

# Проверка и загрузка фикстур в booking-db
check_db_connection "booking-db" "booking" "booking"
load_fixtures "booking-db" "booking" "booking" "init-fixtures.sql"

# Проверка и загрузка фикстур в history-db
check_db_connection "history-db" "history" "history"
# Если в history-db нужны фикстуры, раскомментируйте и укажите файл
# load_fixtures "history-db" "history" "history" "history-fixtures.sql"

sleep 5  # небольшая пауза для сервисов

# Тест REST эндпоинта монолита
echo -e "${ARROW} Тестирование REST-эндпоинта монолита..."
curl -s -X POST "http://localhost:8084/api/bookings?userId=user_test&hotelId=hotel_test&promoCode=SUMMER20" \
    -o /tmp/rest_booking_response.json

if [ $? -eq 0 ] && [ -s /tmp/rest_booking_response.json ]; then
    log_result 0 "Создание бронирования через REST успешно"
else
    log_result 1 "Ошибка при вызове REST эндпоинта монолита"
fi

echo "📄 Ответ монолита:"
cat /tmp/rest_booking_response.json

# Ждем обработки событий Kafka
echo "${CLOCK} Ожидание обработки событий Kafka (10 сек)..."
sleep 10

# Проверка таблицы Bookings
echo "${DB} Проверка таблицы Bookings в booking-db..."
docker exec -i booking-db psql -U booking -d booking -c 'SELECT * FROM "Bookings";' > /tmp/bookings_table.txt 2>/tmp/last_error.txt
cat /tmp/bookings_table.txt

if grep -q "user_test" /tmp/bookings_table.txt; then
    log_result 0 "Таблица Bookings содержит запись user_test"
else
    log_result 1 "Таблица Bookings пуста или запись не создана"
fi

# Проверка таблицы History
echo "${DB} Проверка таблицы History в history-db..."
docker exec -i history-db psql -U history -d history -c 'SELECT * FROM "History";' > /tmp/history_table.txt 2>/tmp/last_error.txt
cat /tmp/history_table.txt

if grep -q "user_test" /tmp/history_table.txt || grep -q "confirmed" /tmp/history_table.txt; then
    log_result 0 "История бронирования успешно записана"
else
    log_result 1 "Таблица History пуста или событие не обработано"
fi

# Проверка логов сервисов
for SERVICE in booking-service booking-history-service; do
    echo "${LOGS} Проверка логов ${SERVICE}..."
    docker logs "${SERVICE}" > "/tmp/${SERVICE}_logs.txt" 2>/tmp/last_error.txt
    if grep -q "booking" "/tmp/${SERVICE}_logs.txt" || grep -q "Saved" "/tmp/${SERVICE}_logs.txt"; then
        log_result 0 "Логи ${SERVICE} содержат записи"
    else
        log_result 1 "Логи ${SERVICE} не содержат ожидаемых записей"
    fi
done

# Проверка Kafka
echo "${KAFKA} Проверка топика booking-confirmed..."
docker exec -i kafka kafka-console-consumer \
    --bootstrap-server kafka:9092 \
    --topic booking-confirmed \
    --timeout-ms 5000 \
    --from-beginning > /tmp/kafka_confirmed.txt 2>/tmp/last_error.txt

if [ -s /tmp/kafka_confirmed.txt ]; then
    log_result 0 "Kafka topic booking-confirmed содержит сообщения"
else
    log_result 1 "Kafka topic booking-confirmed пуст или недоступен"
fi

echo -e "${GREEN}${CHECK} Регрессионные тесты завершены. Проверьте /tmp/*.txt для деталей.${NC}"
