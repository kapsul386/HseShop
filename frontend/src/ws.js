export function connectWs(orderId, onMessage, onStatus) {
    const ws = new WebSocket(`ws://localhost:5280/ws?orderId=${orderId}`);

    onStatus("connecting");

    ws.onopen = () => onStatus("connected");
    ws.onclose = () => onStatus("closed");
    ws.onerror = () => onStatus("error");

    ws.onmessage = (e) => onMessage(JSON.parse(e.data));

    return () => ws.close();
}
