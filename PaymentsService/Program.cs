using Microsoft.EntityFrameworkCore;
using PaymentsService.Messaging;
using PaymentsService.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));

builder.Services.AddDbContext<PaymentsDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("PaymentsDb")));

builder.Services.AddHostedService<OrdersCreatedConsumer>();
builder.Services.AddHostedService<PaymentsOutboxPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));
app.Run();