// Клиент для работы с backend через ApiGateway.
// Все запросы идут с X-User-Id.
export function makeApi(userId) {
    const base = "/api";

    // Базовый helper для fetch
    async function request(path, options = {}) {
        const res = await fetch(base + path, {
            ...options,
            headers: {
                "Content-Type": "application/json",
                "X-User-Id": userId,
            },
        });

        if (!res.ok) {
            const text = await res.text();
            throw new Error(text || res.statusText);
        }

        // поддержка 204 / пустых ответов
        const contentType = res.headers.get("content-type");
        if (contentType && contentType.includes("application/json")) {
            return res.json();
        }

        return null;
    }

    return {
        /* -------- PAYMENTS -------- */

        // Идемпотентное создание аккаунта
        createAccount: () =>
            request("/payments/payments/account", {
                method: "POST",
            }),

        // Пополнение баланса
        topup: (amount) =>
            request("/payments/payments/topup", {
                method: "POST",
                body: JSON.stringify({ amount }),
            }),

        // Текущий баланс
        balance: () =>
            request("/payments/payments/balance"),

        /* -------- ORDERS -------- */

        // Создание заказа (POST /orders)
        createOrder: (amount) =>
            request("/orders", {
                method: "POST",
                body: JSON.stringify({ amount }),
            }),

        // Список заказов пользователя
        listOrders: () =>
            request("/orders"),

        // Получение заказа по id
        getOrder: (orderId) =>
            request(`/orders/${orderId}`),
    };
}
