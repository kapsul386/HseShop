import { makeApi } from "./api.js";
import { connectWs } from "./ws.js";

let userId = localStorage.getItem("userId") || "u1";
let api = makeApi(userId);

const $ = (id) => document.getElementById(id);

$("userId").value = userId;

$("saveUser").onclick = () => {
    userId = $("userId").value.trim();
    localStorage.setItem("userId", userId);
    api = makeApi(userId);
    alert("Saved userId");
};

$("createAccount").onclick = async () => {
    $("balanceOut").textContent = JSON.stringify(await api.createAccount(), null, 2);
};

$("topup").onclick = async () => {
    const amount = Number($("topupAmount").value);
    $("balanceOut").textContent = JSON.stringify(await api.topup(amount), null, 2);
};

$("balance").onclick = async () => {
    $("balanceOut").textContent = JSON.stringify(await api.balance(), null, 2);
};

$("listOrders").onclick = async () => {
    $("ordersOut").textContent = JSON.stringify(await api.listOrders(), null, 2);
};

let disconnectWs = null;

$("createOrder").onclick = async () => {
    const amount = Number($("orderAmount").value);
    const r = await api.createOrder(amount);
    const orderId = r.orderId;

    $("lastOrderId").textContent = orderId;
    $("wsOut").textContent = "";
    $("wsStatus").textContent = "connecting";

    if (disconnectWs) disconnectWs();

    disconnectWs = connectWs(
        orderId,
        (msg) => {
            $("wsOut").textContent += JSON.stringify(msg) + "\n";
            alert(`Order ${msg.orderId}: ${msg.status}`);
        },
        (st) => ($("wsStatus").textContent = st)
    );
};
