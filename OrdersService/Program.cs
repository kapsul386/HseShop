using Microsoft.EntityFrameworkCore;
using OrdersService.Messaging;
using OrdersService.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));

builder.Services.AddDbContext<OrdersDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

builder.Services.AddHostedService<OrdersOutboxPublisher>();
builder.Services.AddHostedService<PaymentsEventsConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));
app.Run();