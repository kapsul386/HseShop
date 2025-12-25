export function makeApi(userId) {
    const base = "/api";

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

        // на случай 204 / пустых ответов
        const contentType = res.headers.get("content-type");
        if (contentType && contentType.includes("application/json")) {
            return res.json();
        }

        return null;
    }

    return {
        /* ---------------- PAYMENTS ---------------- */

        createAccount: () =>
            request("/payments/payments/account", {
                method: "POST",
            }),

        topup: (amount) =>
            request("/payments/payments/topup", {
                method: "POST",
                body: JSON.stringify({ amount }),
            }),

        balance: () =>
            request("/payments/payments/balance"),

        /* ---------------- ORDERS ---------------- */

        // POST /orders
        createOrder: (amount) =>
            request("/orders", {
                method: "POST",
                body: JSON.stringify({ amount }),
            }),

        // GET /orders
        listOrders: () =>
            request("/orders"),

        // GET /orders/{id}
        getOrder: (orderId) =>
            request(`/orders/${orderId}`),
    };
}
