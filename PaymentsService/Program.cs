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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

builder.Services.AddScoped<IAccountsService, AccountsService>();

builder.Services.AddDbContext<PaymentsDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("PaymentsDb")));

builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));
builder.Services.AddSingleton<IRabbitConnection, RabbitConnection>();

builder.Services.AddScoped<OrderPaymentProcessor>();
builder.Services.AddHostedService<OrdersCreatedConsumer>();
builder.Services.AddHostedService<PaymentsOutboxPublisher>();

var app = builder.Build();

// Apply database migrations on startup so Inbox/Outbox tables exist in Docker
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment() ||
    app.Environment.EnvironmentName.Equals("Docker", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
