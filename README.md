
# HseShop

Домашняя работа №4 по дисциплине **«Конструирование программного обеспечения»**  
Тема: *Асинхронное межсервисное взаимодействие*

---

## Описание проекта

**HseShop** — микросервисная система для обработки заказов и платежей с асинхронным взаимодействием между сервисами через брокер сообщений.

### Реализуемый бизнес-сценарий

1. Пользователь создаёт аккаунт
2. Пользователь пополняет баланс
3. Пользователь создаёт заказ
4. **OrdersService** публикует событие `orders.created` (Transactional Outbox)
5. **PaymentsService** обрабатывает платёж (Transactional Inbox + idempotency)
6. **PaymentsService** публикует результат (`payments.succeeded` / `payments.failed`)
7. **OrdersService** обновляет статус заказа:
   - `FINISHED` — при успешной оплате
   - `CANCELLED` — при недостатке средств
8. Клиент получает push-уведомление о смене статуса через **WebSocket**

Проект полностью соответствует требованиям задания, включая все дополнительные пункты.

---

## Архитектура системы

### Сервисы

- **ApiGateway**
  - YARP Reverse Proxy
  - единая точка входа
  - маршрутизация HTTP-запросов

- **OrdersService**
  - создание заказов
  - хранение заказов
  - Transactional Outbox (`orders.created`)
  - обработка результатов оплаты

- **PaymentsService**
  - создание аккаунта пользователя
  - пополнение баланса
  - Transactional Inbox
  - exactly-once списание средств
  - Transactional Outbox (`payments.succeeded` / `payments.failed`)

- **NotificationsService**
  - подписка на события оплаты
  - WebSocket-сервер
  - push-уведомления клиентам о смене статуса заказа

- **Frontend**
  - отдельный сервис
  - REST-взаимодействие с ApiGateway
  - WebSocket-подключение к NotificationsService
  - отображение заказов и уведомлений

- **RabbitMQ**
  - брокер сообщений
  - topic exchange `hse.shop`

- **PostgreSQL**
  - отдельная база данных на каждый сервис

---

## Используемые технологии

- .NET 9
- ASP.NET Core Web API
- YARP Reverse Proxy
- RabbitMQ (+ Management UI)
- PostgreSQL
- Entity Framework Core + Migrations
- Docker / Docker Compose
- WebSocket
- Postman

---


## Структура репозитория
```text
HseShop/
├── ApiGateway/
│   ├── appsettings.json
│   ├── appsettings.Docker.json
│   ├── Program.cs
│   └── Dockerfile
│
├── OrdersService/
│   ├── Controllers/
│   ├── Application/
│   ├── Infrastructure/
│   ├── Persistence/
│   ├── Program.cs
│   └── Dockerfile
│
├── PaymentsService/
│   ├── Controllers/
│   ├── Application/
│   ├── Infrastructure/
│   ├── Persistence/
│   ├── Program.cs
│   └── Dockerfile
│
├── NotificationsService/
│   ├── Messaging/
│   ├── WebSockets/
│   ├── Program.cs
│   └── Dockerfile
│
├── frontend/
│   ├── src/
│   ├── index.html
│   ├── nginx.conf
│   ├── package.json
│   └── Dockerfile
│
├── Contracts/
│   └── Contracts.csproj
│
├── docker-compose.yml
├── HseShop.sln
├── hse-shop.postman_collection.json
└── README.md
````

---

## Требования для запуска

* Docker
* Docker Compose

---

## Запуск проекта

### 1. Клонирование репозитория

```bash
git clone https://github.com/kapsul386/HseShop.git
cd HseShop
```

### 2. Запуск системы

```bash
docker compose up -d --build
```

### 3. Проверка состояния контейнеров

```bash
docker compose ps
```

Все сервисы должны находиться в состоянии `Up`.

---

## Используемые порты

| Сервис                           | Адрес                                                        |
| -------------------------------- | ------------------------------------------------------------ |
| Frontend                         | [http://localhost:8080](http://localhost:8080)               |
| ApiGateway                       | [http://localhost:5271](http://localhost:5271)               |
| NotificationsService (WebSocket) | ws://localhost:5280/ws?orderId=<ORDER_ID>                    |
| NotificationsService health      | [http://localhost:5280/health](http://localhost:5280/health) |
| RabbitMQ UI                      | [http://localhost:15672](http://localhost:15672)             |
| Orders DB                        | localhost:5433                                               |
| Payments DB                      | localhost:5434                                               |

RabbitMQ credentials:

* login: `guest`
* password: `guest`

---

## Проверка работоспособности

### Через фронт

1. Открыть: **[http://localhost:8080](http://localhost:8080)**
2. Создать аккаунт пользователя
3. Пополнить баланс (например, на 100)
4. Создать заказ (например, на 50)
5. Убедиться, что:

    * WebSocket подключён (`connected`)
    * получено push-уведомление
    * статус заказа `FINISHED`
6. Создать заказ на сумму больше баланса
7. Убедиться, что:

    * статус заказа `CANCELLED`
    * получено push-уведомление

---

### Через Postman

В репозитории находится готовая коллекция:

```text
hse-shop.postman_collection.json
```

Переменные коллекции:

* `baseUrl = http://localhost:5271`
* `userId = u1`
* `orderId` (устанавливается автоматически)

#### Последовательность запросов

1. **Create account (idempotent)**
   `POST {{baseUrl}}/payments/payments/account`

2. **Top up**
   `POST {{baseUrl}}/payments/payments/topup`

   ```json
   { "amount": 100 }
   ```

3. **Get balance**
   `GET {{baseUrl}}/payments/payments/balance`

4. **Create order**
   `POST {{baseUrl}}/orders`

   ```json
   { "amount": 50 }
   ```

5. **List orders**
   `GET {{baseUrl}}/orders`

6. **Get order status**
   `GET {{baseUrl}}/orders/{{orderId}}`
   Ожидаемый статус: `FINISHED` или `CANCELLED`

---

### Через терминал

```bash
# Create account
curl -X POST http://localhost:5271/payments/payments/account \
  -H "X-User-Id: u1"

# Top up
curl -X POST http://localhost:5271/payments/payments/topup \
  -H "X-User-Id: u1" \
  -H "Content-Type: application/json" \
  -d "{\"amount\":100}"

# Get balance
curl http://localhost:5271/payments/payments/balance \
  -H "X-User-Id: u1"

# Create order
curl -X POST http://localhost:5271/orders \
  -H "X-User-Id: u1" \
  -H "Content-Type: application/json" \
  -d "{\"amount\":50}"

# List orders
curl http://localhost:5271/orders \
  -H "X-User-Id: u1"

# Get order status
curl http://localhost:5271/orders/ORDER_ID \
  -H "X-User-Id: u1"
```

---

## Автор

```
Купцов Дмитрий Дмитриевич
БПИ-246
```


