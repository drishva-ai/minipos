using MassTransit;
using Payment.Service.Consumers;
using Payment.Service.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [Payment.Service] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var cfg        = builder.Configuration;
    var rabbitHost = cfg["RabbitMQ:Host"]     ?? "localhost";
    var rabbitUser = cfg["RabbitMQ:User"]     ?? "admin";
    var rabbitPass = cfg["RabbitMQ:Password"] ?? "password";

    // In production: builder.Services.AddDbContext<PaymentDbContext>(...) for SQL Server
    builder.Services.AddSingleton<ITransactionRepository, InMemoryTransactionRepository>();

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<ProcessPaymentConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h => { h.Username(rabbitUser); h.Password(rabbitPass); });
            cfg.ReceiveEndpoint("payment-queue", e =>
            {
                e.ConfigureConsumer<ProcessPaymentConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
            });
        });
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new()
    {
        Title       = "Payment Service",
        Version     = "v1",
        Description = "Processes payments. Commands arrive via payment-queue. GET /api/payment/transactions for audit trail."
    }));

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service v1"));
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { service = "Payment.Service", status = "healthy" }));

    Log.Information("Payment.Service starting — listening on payment-queue");
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Payment.Service failed"); throw; }
finally { Log.CloseAndFlush(); }
