using Basket.Service.Consumers;
using Basket.Service.Services;
using MassTransit;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [Basket.Service] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var cfg         = builder.Configuration;
    var rabbitHost  = cfg["RabbitMQ:Host"]        ?? "localhost";
    var rabbitUser  = cfg["RabbitMQ:User"]        ?? "admin";
    var rabbitPass  = cfg["RabbitMQ:Password"]    ?? "password";
    var redisConn   = cfg["Redis:Connection"]     ?? "localhost:6379";
    var articlesUrl = cfg["ArticlesService:BaseUrl"] ?? "http://localhost:5002";

    // ── Redis ─────────────────────────────────────────────────────────────────
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);

    // ── HTTP client → Articles.Service (internal, no JWT needed) ─────────────
    builder.Services.AddHttpClient<IArticlesClient, ArticlesClient>(c =>
    {
        c.BaseAddress = new Uri(articlesUrl);
        c.Timeout     = TimeSpan.FromSeconds(5);
    });

    // ── Services ──────────────────────────────────────────────────────────────
    builder.Services.AddScoped<IBasketRepository, RedisBasketRepository>();

    // ── MassTransit ───────────────────────────────────────────────────────────
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<AddItemConsumer>();
        x.AddConsumer<RemoveItemConsumer>();
        x.AddConsumer<AbortBasketConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h => { h.Username(rabbitUser); h.Password(rabbitPass); });

            cfg.ReceiveEndpoint("basket-queue", e =>
            {
                e.ConfigureConsumer<AddItemConsumer>(ctx);
                e.ConfigureConsumer<RemoveItemConsumer>(ctx);
                e.ConfigureConsumer<AbortBasketConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
            });
        });
    });

    // ── Swagger + Controllers (for debug/read access) ─────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new()
    {
        Title       = "Basket Service",
        Version     = "v1",
        Description = "Manages basket state in Redis. Write operations arrive via RabbitMQ basket-queue from POS.WebHost."
    }));

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Basket Service v1"));
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { service = "Basket.Service", status = "healthy" }));

    Log.Information("Basket.Service starting — listening on basket-queue");
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Basket.Service failed"); throw; }
finally { Log.CloseAndFlush(); }
