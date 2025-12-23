using Microsoft.EntityFrameworkCore;
using PaymentsService.Application.Accounts;
using PaymentsService.Infrastructure.Http;
using PaymentsService.Infrastructure.Messaging;
using PaymentsService.Infrastructure.Messaging.Consumers;
using PaymentsService.Infrastructure.Messaging.Outbox;
using PaymentsService.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP user context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

// Application
builder.Services.AddScoped<IAccountsService, AccountsService>();

// DB
builder.Services.AddDbContext<PaymentsDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("PaymentsDb")));

// Rabbit
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));
builder.Services.AddSingleton<IRabbitConnection, RabbitConnection>();

// Messaging
builder.Services.AddScoped<OrderPaymentProcessor>();
builder.Services.AddHostedService<OrdersCreatedConsumer>();
builder.Services.AddHostedService<PaymentsOutboxPublisher>();

var app = builder.Build();

// ✅ Apply migrations on startup (so Outbox/Inbox tables exist in docker volumes)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

// ✅ Swagger in Docker too (ASPNETCORE_ENVIRONMENT=Docker)
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("Docker", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();