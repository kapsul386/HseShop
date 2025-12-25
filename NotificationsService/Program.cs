using Microsoft.Extensions.Options;
using NotificationsService.Messaging;
using NotificationsService.Ws;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Docker.json", optional: true, reloadOnChange: true);

builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));
builder.Services.AddSingleton<IRabbitConnection, RabbitConnection>();

builder.Services.AddSingleton<WsHub>();
builder.Services.AddHostedService<PaymentsEventsConsumer>();

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/health", () => Results.Ok("OK"));

app.Map("/ws", async (HttpContext ctx, WsHub hub) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsync("Expected WebSocket request");
        return;
    }

    var orderIdStr = ctx.Request.Query["orderId"].ToString();
    if (!Guid.TryParse(orderIdStr, out var orderId))
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsync("Query param 'orderId' must be a GUID");
        return;
    }

    using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
    await hub.HandleConnectionAsync(orderId, socket, ctx.RequestAborted);
});

app.Run();