using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace NotificationsService.Ws;

public sealed class WsHub
{
    // orderId -> connections (connId -> socket)
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, WebSocket>> _map = new();

    public async Task HandleConnectionAsync(Guid orderId, WebSocket socket, CancellationToken ct)
    {
        var connId = Guid.NewGuid();
        var group = _map.GetOrAdd(orderId, _ => new ConcurrentDictionary<Guid, WebSocket>());
        group[connId] = socket;

        // Держим соединение открытым (читаем "пустые" сообщения)
        var buf = new byte[1024];
        try
        {
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var res = await socket.ReceiveAsync(buf, ct);
                if (res.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch
        {
            // ignore
        }
        finally
        {
            group.TryRemove(connId, out _);
            if (group.IsEmpty)
                _map.TryRemove(orderId, out _);
        }
    }

    public async Task NotifyAsync(Guid orderId, string status, string? reason, CancellationToken ct)
    {
        if (!_map.TryGetValue(orderId, out var group) || group.IsEmpty)
            return;

        var payload = JsonSerializer.Serialize(new
        {
            orderId,
            status,
            reason
        });

        var bytes = Encoding.UTF8.GetBytes(payload);
        var seg = new ArraySegment<byte>(bytes);

        foreach (var kv in group.ToArray())
        {
            var socket = kv.Value;

            if (socket.State != WebSocketState.Open)
            {
                group.TryRemove(kv.Key, out _);
                continue;
            }

            try
            {
                await socket.SendAsync(seg, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: ct);
            }
            catch
            {
                group.TryRemove(kv.Key, out _);
            }
        }
    }
}
