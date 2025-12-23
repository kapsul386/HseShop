


# HseShop

Домашняя работа №4 по дисциплине **«Конструирование программного обеспечения»**

---

## Описание проекта

**HseShop** — микросервисная система для обработки заказов и платежей с асинхронным взаимодействием между сервисами через брокер сообщений.

Реализуемый сценарий:

1. Пользователь создаёт аккаунт
2. Пользователь пополняет баланс
3. Пользователь создаёт заказ
4. OrdersService публикует событие `orders.created` (Transactional Outbox)
5. PaymentsService обрабатывает платёж (Inbox + exactly-once)
6. PaymentsService публикует результат (`payments.succeeded` / `payments.failed`)
7. OrdersService обновляет статус заказа (`FINISHED` / `CANCELLED`)
8. Клиент получает push-уведомление о смене статуса через WebSocket

Проект реализован в соответствии с требованиями задания, включая дополнительную часть.

---

## Архитектура системы

### Сервисы

- **ApiGateway**
  - YARP Reverse Proxy
  - единая точка входа
  - проксирование запросов в backend-сервисы

- **OrdersService**
  - создание заказов
  - хранение заказов
  - публикация `orders.created` через Outbox
  - обработка результатов оплаты

- **PaymentsService**
  - создание аккаунта пользователя
  - пополнение баланса
  - обработка `orders.created`
  - exactly-once списание средств
  - публикация `payments.succeeded` / `payments.failed` через Outbox

- **NotificationsService**
  - подписка на события оплаты
  - WebSocket сервер
  - отправка push-уведомлений клиентам о смене статуса заказа

- **Frontend**
  - отдельный сервис
  - REST-взаимодействие с ApiGateway
  - WebSocket-подключение к NotificationsService
  - отображение статусов заказов и push-уведомлений

- **RabbitMQ**
  - брокер сообщений
  - topic exchange `hse.shop`

- **PostgreSQL**
  - отдельная база данных на сервис

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

```

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

- Docker
- Docker Compose

Установка .NET SDK или Node.js локально **не требуется**, если запуск производится через Docker.

---

## Запуск проекта

### 1. Клонирование репозитория

```bash
git clone https://github.com/kapsul386/HseShop.git
cd HseShop
````

### 2. Запуск всей системы

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
| NotificationsService (WebSocket) | ws://localhost:5280                                          |
| NotificationsService health      | [http://localhost:5280/health](http://localhost:5280/health) |
| RabbitMQ UI                      | [http://localhost:15672](http://localhost:15672)             |
| Orders DB                        | localhost:5433                                               |
| Payments DB                      | localhost:5434                                               |

RabbitMQ:

* логин: `guest`
* пароль: `guest`

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

```
hse-shop.postman_collection.json
```

Переменные коллекции:

* `baseUrl = http://localhost:5271`
* `userId = u1`
* `orderId` (устанавливается автоматически)

#### Последовательность запросов

1. **Create account (idempotent)**
   `POST {{baseUrl}}/payments/payments/account`
   Header: `X-User-Id: {{userId}}`

2. **Top up**
   `POST {{baseUrl}}/payments/payments/topup`
   Body:

   ```json
   { "amount": 100 }
   ```

3. **Get balance**
   `GET {{baseUrl}}/payments/payments/balance`

4. **Create order**
   `POST {{baseUrl}}/orders`
   Body:

   ```json
   { "amount": 50 }
   ```

5. **Get order status**
   Выполнить несколько раз с паузой 1–2 секунды
   Ожидаемый статус: `FINISHED` или `CANCELLED`

---

### Через терминал

```bash
curl -X POST http://localhost:5271/payments/payments/account \
  -H "X-User-Id: u1"

curl -X POST http://localhost:5271/payments/payments/topup \
  -H "X-User-Id: u1" \
  -H "Content-Type: application/json" \
  -d "{\"amount\":100}"

curl -X POST http://localhost:5271/orders \
  -H "X-User-Id: u1" \
  -H "Content-Type: application/json" \
  -d "{\"amount\":50}"
```


---

## Автор
```
Купцов Дмитрий Дмитриевич 
БПИ-246
```
