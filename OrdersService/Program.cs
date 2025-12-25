using Microsoft.EntityFrameworkCore;
using OrdersService.Application.Orders;
using OrdersService.Infrastructure.Http;
using OrdersService.Infrastructure.Messaging;
using OrdersService.Infrastructure.Messaging.Consumers;
using OrdersService.Infrastructure.Messaging.Outbox;
using OrdersService.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP user context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

// Application
builder.Services.AddScoped<IOrdersService, OrdersService.Application.Orders.OrdersService>();

// DB
builder.Services.AddDbContext<OrdersDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

// Rabbit
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));
builder.Services.AddSingleton<IRabbitConnection, RabbitConnection>();

// Messaging background services
builder.Services.AddHostedService<OrdersOutboxPublisher>();
builder.Services.AddScoped<PaymentResultHandler>();
builder.Services.AddHostedService<PaymentsEventsConsumer>();

var app = builder.Build();

// Apply migrations before background services start working
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}

// Swagger (Development + Docker)
if (app.Environment.IsDevelopment() ||
    app.Environment.EnvironmentName.Equals("Docker", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();