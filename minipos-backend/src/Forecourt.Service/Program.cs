using Forecourt.Service.Consumers;
using Forecourt.Service.Services;
using MassTransit;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [Forecourt.Service] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var cfg        = builder.Configuration;
    var rabbitHost = cfg["RabbitMQ:Host"]     ?? "localhost";
    var rabbitUser = cfg["RabbitMQ:User"]     ?? "admin";
    var rabbitPass = cfg["RabbitMQ:Password"] ?? "password";

    builder.Services.AddSingleton<IPumpStateService, PumpStateService>();

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<AuthorisePumpConsumer>();
        x.AddConsumer<StopPumpConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(rabbitUser);
                h.Password(rabbitPass);
            });

            cfg.ReceiveEndpoint("forecourt-queue", e =>
            {
                e.ConfigureConsumer<AuthorisePumpConsumer>(ctx);
                e.ConfigureConsumer<StopPumpConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(500, 1000));
            });
        });
    });

    builder.Services.AddHostedService<ForecourtSimulatorWorker>();

    builder.Services.AddCors(o => o.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new()
    {
        Title       = "Forecourt Service",
        Version     = "v1",
        Description = "Manages 6 fuel pump states."
    }));

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors("AllowAll");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Forecourt Service v1"));
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { service = "Forecourt.Service", status = "healthy" }));

    Log.Information("Forecourt.Service starting — 6 pumps ready");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Forecourt.Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}