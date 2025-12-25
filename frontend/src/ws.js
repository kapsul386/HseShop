export function connectWs(orderId, onMessage, onStatus) {
    let ws = null;
    let closedByClient = false;
    let reconnectTimer = null;
    let pingTimer = null;

    const url = `ws://localhost:5280/ws?orderId=${encodeURIComponent(orderId)}`;

    function safeStatus(s) {
        try { onStatus?.(s); } catch { /* ignore */ }
    }

    function safeMessage(msg) {
        try { onMessage?.(msg); } catch { /* ignore */ }
    }

    function stopTimers() {
        if (reconnectTimer) {
            clearTimeout(reconnectTimer);
            reconnectTimer = null;
        }
        if (pingTimer) {
            clearInterval(pingTimer);
            pingTimer = null;
        }
    }

    function startPing() {
        // Пингуем раз в 25s: помогает держать соединение живым
        // На сервере можно игнорировать "ping" или отвечать "pong" — это не ломает.
        pingTimer = setInterval(() => {
            if (ws && ws.readyState === WebSocket.OPEN) {
                try { ws.send("ping"); } catch { /* ignore */ }
            }
        }, 25000);
    }

    function scheduleReconnect() {
        if (closedByClient) return;
        stopTimers();
        safeStatus("reconnecting");
        reconnectTimer = setTimeout(() => {
            open();
        }, 1000);
    }

    function open() {
        if (closedByClient) return;

        safeStatus("connecting");
        ws = new WebSocket(url);

        ws.onopen = () => {
            safeStatus("connected");
            stopTimers();
            startPing();
        };

        ws.onclose = () => {
            safeStatus("closed");
            scheduleReconnect();
        };

        ws.onerror = () => {
            safeStatus("error");
            // onclose обычно тоже сработает, но если нет — переподключим
            scheduleReconnect();
        };

        ws.onmessage = (e) => {
            const data = e.data;

            // сервер может присылать "pong" или любые текстовые сервисные сообщения
            if (typeof data === "string") {
                if (data === "pong" || data === "ping") return;

                try {
                    safeMessage(JSON.parse(data));
                } catch {
                    // не JSON — просто игнорим, чтобы фронт не падал
                }
            }
        };
    }

    open();

    // cleanup
    return () => {
        closedByClient = true;
        stopTimers();
        try { ws?.close(); } catch { /* ignore */ }
        ws = null;
    };
}
