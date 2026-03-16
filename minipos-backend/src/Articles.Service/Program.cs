using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [Articles.Service] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new()
    {
        Title       = "Articles Service",
        Version     = "v1",
        Description = "Product catalogue API. Called internally by Basket.Service for barcode lookup. Also called by the React UI for the product grid display."
    }));
    // Internal service — allow all origins (no JWT needed inside Docker network)
    builder.Services.AddCors(o => o.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    app.UseCors("AllowAll");
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Articles Service v1"));
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { service = "Articles.Service", status = "healthy" }));

    Log.Information("Articles.Service starting");
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Articles.Service failed"); throw; }
finally { Log.CloseAndFlush(); }
