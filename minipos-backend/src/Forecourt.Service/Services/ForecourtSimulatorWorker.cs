using MassTransit;
using MiniPOS.Shared.Messages;

namespace Forecourt.Service.Services;

/// <summary>
/// Background service that simulates the FC Controller hardware.
/// Every 15 seconds picks a random FUELLING pump and marks it DONE.
///
/// IMPORTANT: Uses IServiceScopeFactory to resolve IPublishEndpoint.
/// BackgroundService is Singleton, but IPublishEndpoint is Scoped —
/// you must create a scope manually to resolve it. This is standard .NET pattern.
/// </summary>
public sealed class ForecourtSimulatorWorker : BackgroundService
{
    private readonly IPumpStateService    _pumps;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ForecourtSimulatorWorker> _log;

    private static readonly Dictionary<string, decimal> PricePerLitre = new()
    {
        { "Diesel",   1.52m },
        { "Unleaded", 1.55m },
        { "Premium",  1.72m }
    };

    public ForecourtSimulatorWorker(
        IPumpStateService pumps,
        IServiceScopeFactory scopeFactory,
        ILogger<ForecourtSimulatorWorker> log)
    {
        _pumps        = pumps;
        _scopeFactory = scopeFactory;
        _log          = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("[FC Simulator] Started — auto-completing FUELLING pumps every 15s");

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), ct);

            var fuelling = _pumps.GetAll().Where(p => p.Status == "FUELLING").ToList();
            if (fuelling.Count == 0) continue;

            var pump   = fuelling[Random.Shared.Next(fuelling.Count)];
            var litres = Math.Round((decimal)(Random.Shared.NextDouble() * 40 + 5), 2);
            var price  = PricePerLitre.GetValueOrDefault(pump.FuelType, 1.55m);
            var amount = Math.Round(litres * price, 2);

            pump.Status          = "DONE";
            pump.LitresDispensed = litres;
            pump.Amount          = amount;
            _pumps.Update(pump);

            _log.LogInformation("[FC Controller] Pump {Id} DONE — {L}L x {P}/L = {A}",
                pump.Id, litres, price, amount);

            // Create a scope to resolve scoped IPublishEndpoint from a Singleton service
            using var scope = _scopeFactory.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await bus.Publish(new PumpStatusChangedEvent
            {
                PumpId          = pump.Id,
                Status          = "DONE",
                FuelType        = pump.FuelType,
                LitresDispensed = litres,
                Amount          = amount
            }, ct);
        }
    }
}
