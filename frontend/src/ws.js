// WebSocket-клиент для получения push-уведомлений по заказу
export function connectWs(orderId, onMessage, onStatus) {
    let ws = null;
    let closedByClient = false;
    let reconnectTimer = null;
    let pingTimer = null;

    const url = `ws://localhost:5280/ws?orderId=${encodeURIComponent(orderId)}`;

    function safeStatus(s) {
        try { onStatus?.(s); } catch {}
    }

    function safeMessage(msg) {
        try { onMessage?.(msg); } catch {}
    }

    function stopTimers() {
        if (reconnectTimer) clearTimeout(reconnectTimer);
        if (pingTimer) clearInterval(pingTimer);
        reconnectTimer = null;
        pingTimer = null;
    }

    // Периодический ping, чтобы соединение не закрывалось прокси
    function startPing() {
        pingTimer = setInterval(() => {
            if (ws && ws.readyState === WebSocket.OPEN) {
                try { ws.send("ping"); } catch {}
            }
        }, 25000);
    }

    function scheduleReconnect() {
        if (closedByClient) return;
        stopTimers();
        safeStatus("reconnecting");
        reconnectTimer = setTimeout(open, 1000);
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
            scheduleReconnect();
        };

        ws.onmessage = (e) => {
            const data = e.data;

            // сервисные сообщения можно игнорировать
            if (typeof data === "string") {
                if (data === "ping" || data === "pong") return;

                try {
                    safeMessage(JSON.parse(data));
                } catch {
                    // не JSON — игнорируем
                }
            }
        };
    }

    open();

    // cleanup-функция
    return () => {
        closedByClient = true;
        stopTimers();
        try { ws?.close(); } catch {}
        ws = null;
    };
}
