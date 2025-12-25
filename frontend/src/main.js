import { makeApi } from "./api.js";
import { connectWs } from "./ws.js";

let userId = localStorage.getItem("userId") || "u1";
let api = makeApi(userId);

const $ = (id) => document.getElementById(id);

// --- toast helper (без библиотек) ---
function ensureToastHost() {
    let host = document.getElementById("toastHost");
    if (!host) {
        host = document.createElement("div");
        host.id = "toastHost";
        host.style.position = "fixed";
        host.style.right = "16px";
        host.style.bottom = "16px";
        host.style.display = "flex";
        host.style.flexDirection = "column";
        host.style.gap = "8px";
        host.style.zIndex = "9999";
        document.body.appendChild(host);
    }
    return host;
}

function toast(text) {
    const host = ensureToastHost();
    const el = document.createElement("div");
    el.textContent = text;

    el.style.padding = "10px 12px";
    el.style.borderRadius = "10px";
    el.style.boxShadow = "0 6px 20px rgba(0,0,0,.15)";
    el.style.background = "white";
    el.style.border = "1px solid rgba(0,0,0,.08)";
    el.style.fontFamily = "ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Arial";
    el.style.fontSize = "14px";
    el.style.maxWidth = "320px";
    el.style.wordBreak = "break-word";

    host.appendChild(el);

    setTimeout(() => {
        el.style.opacity = "0";
        el.style.transition = "opacity .25s ease";
        setTimeout(() => el.remove(), 300);
    }, 2500);
}

async function safeRun(fn, outElId) {
    try {
        const r = await fn();
        if (outElId) $(outElId).textContent = JSON.stringify(r, null, 2);
        return r;
    } catch (e) {
        const msg = e?.message ?? String(e);
        if (outElId) $(outElId).textContent = msg;
        toast(msg);
        throw e;
    }
}

$("userId").value = userId;

$("saveUser").onclick = () => {
    userId = $("userId").value.trim() || "u1";
    localStorage.setItem("userId", userId);
    api = makeApi(userId);
    toast("Saved userId");
};

$("createAccount").onclick = async () => {
    await safeRun(() => api.createAccount(), "balanceOut");
};

$("topup").onclick = async () => {
    const amount = Number($("topupAmount").value);
    await safeRun(() => api.topup(amount), "balanceOut");
};

$("balance").onclick = async () => {
    await safeRun(() => api.balance(), "balanceOut");
};

$("listOrders").onclick = async () => {
    await safeRun(() => api.listOrders(), "ordersOut");
};

let disconnectWs = null;

function setWsStatus(st) {
    $("wsStatus").textContent = st;
}

$("createOrder").onclick = async () => {
    const amount = Number($("orderAmount").value);

    $("wsOut").textContent = "";
    setWsStatus("connecting");

    // закрываем прошлый WS, если был
    if (disconnectWs) {
        disconnectWs();
        disconnectWs = null;
    }

    let r;
    try {
        r = await api.createOrder(amount);
    } catch (e) {
        setWsStatus("error");
        return;
    }

    const orderId = r.orderId || r.id || r.orderID;
    if (!orderId) {
        toast("createOrder: cannot find orderId in response");
        setWsStatus("error");
        return;
    }

    $("lastOrderId").textContent = orderId;

    disconnectWs = connectWs(
        orderId,
        (msg) => {
            $("wsOut").textContent += JSON.stringify(msg) + "\n";

            // нормальное уведомление вместо alert
            const sid = msg.orderId ?? orderId;
            const st = msg.status ?? msg.state ?? "UNKNOWN";
            toast(`Order ${sid}: ${st}`);
        },
        (st) => setWsStatus(st)
    );
};
