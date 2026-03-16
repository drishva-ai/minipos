using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using POS.WebHost.Consumers;
using POS.WebHost.Hubs;
using POS.WebHost.Services;
using Serilog;

// ── Bootstrap Serilog ─────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── Configuration ─────────────────────────────────────────────────────────
    var cfg        = builder.Configuration;
    var jwtKey     = cfg["Jwt:Key"]              ?? throw new InvalidOperationException("Jwt:Key missing");
    var rabbitHost = cfg["RabbitMQ:Host"]        ?? "localhost";
    var rabbitUser = cfg["RabbitMQ:User"]        ?? "admin";
    var rabbitPass = cfg["RabbitMQ:Password"]    ?? "password";

    // ── CORS ──────────────────────────────────────────────────────────────────
    builder.Services.AddCors(o => o.AddPolicy("AllowUI", p =>
        p.WithOrigins(
            "http://localhost:5173",           // Vite dev
            "http://localhost:3000",           // Docker
            cfg["Cors:AllowedOrigin"] ?? "*"  // Production (set via env var)
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));   // Required for SignalR

    // ── JWT Authentication ────────────────────────────────────────────────────
    var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = "minipos",
                ValidAudience            = "minipos-client",
                IssuerSigningKey         = new SymmetricSecurityKey(keyBytes)
            };
            // SignalR requires token in query string (browsers can't set WS headers)
            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) &&
                        ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        ctx.Token = token;
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization();

    // ── SignalR ───────────────────────────────────────────────────────────────
    builder.Services.AddSignalR(o => o.EnableDetailedErrors =
        builder.Environment.IsDevelopment());

    // ── MassTransit + RabbitMQ ────────────────────────────────────────────────
    builder.Services.AddMassTransit(x =>
    {
        // POS.WebHost RECEIVES events that services publish back
        x.AddConsumer<BasketUpdatedConsumer>();
        x.AddConsumer<PaymentCompletedConsumer>();
        x.AddConsumer<PumpStatusChangedConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(rabbitUser);
                h.Password(rabbitPass);
            });

            // Named receive endpoint — other services target this queue
            cfg.ReceiveEndpoint("pos-webhost-events", e =>
            {
                e.ConfigureConsumer<BasketUpdatedConsumer>(ctx);
                e.ConfigureConsumer<PaymentCompletedConsumer>(ctx);
                e.ConfigureConsumer<PumpStatusChangedConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
            });
        });
    });

    // ── Application Services ──────────────────────────────────────────────────
    builder.Services.AddSingleton<ITokenService, TokenService>();

    // ── Controllers + Swagger ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "POS.WebHost — Gateway API",
            Version     = "v1",
            Description = "Handles JWT authentication and serves as the SignalR gateway into the microservices backend.",
            Contact     = new OpenApiContact { Name = "Mini POS Team" }
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Enter your JWT token. Obtain one via POST /api/auth/login"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                        { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "POS.WebHost v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "POS WebHost — Swagger";
    });

    app.UseCors("AllowUI");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<PosHub>("/hubs/pos").RequireCors("AllowUI");

    // Health endpoint for Docker/Render health checks
    app.MapGet("/health", () => Results.Ok(new { service = "POS.WebHost", status = "healthy", time = DateTime.UtcNow }));

    Log.Information("POS.WebHost starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "POS.WebHost failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
