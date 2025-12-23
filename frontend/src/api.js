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

        if (!res.ok) throw new Error(await res.text());
        return res.json();
    }

    return {
        createAccount: () => request("/payments/payments/account", { method: "POST" }),
        topup: (amount) =>
            request("/payments/payments/topup", {
                method: "POST",
                body: JSON.stringify({ amount }),
            }),
        balance: () => request("/payments/payments/balance"),
        createOrder: (amount) =>
            request("/orders/orders", {
                method: "POST",
                body: JSON.stringify({ amount }),
            }),
        listOrders: () => request("/orders/orders"),
    };
}
