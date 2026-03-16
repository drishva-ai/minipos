using Forecourt.Service.Services;
using MassTransit;
using MiniPOS.Shared.Messages;

namespace Forecourt.Service.Consumers;

public sealed class AuthorisePumpConsumer : IConsumer<AuthorisePumpCommand>
{
    private readonly IPumpStateService _pumps;
    private readonly IPublishEndpoint  _bus;
    private readonly ILogger<AuthorisePumpConsumer> _log;

    public AuthorisePumpConsumer(IPumpStateService pumps, IPublishEndpoint bus,
        ILogger<AuthorisePumpConsumer> log)
    {
        _pumps = pumps; _bus = bus; _log = log;
    }

    public async Task Consume(ConsumeContext<AuthorisePumpCommand> ctx)
    {
        var cmd  = ctx.Message;
        var pump = _pumps.Get(cmd.PumpId);

        if (pump is null || pump.Status != "IDLE")
        {
            _log.LogWarning("[AuthorisePump] Pump {Id} not available (status: {S})",
                cmd.PumpId, pump?.Status ?? "not found");
            return;
        }

        pump.Status          = "FUELLING";
        pump.StartedAt       = DateTime.UtcNow;
        pump.LitresDispensed = 0;
        pump.Amount          = 0;
        _pumps.Update(pump);

        _log.LogInformation("[AuthorisePump] Pump {Id}: IDLE → FUELLING | Cashier: {C}",
            cmd.PumpId, cmd.CashierId);

        await _bus.Publish(new PumpStatusChangedEvent
        {
            PumpId   = pump.Id,
            Status   = "FUELLING",
            FuelType = pump.FuelType
        });
    }
}

public sealed class StopPumpConsumer : IConsumer<StopPumpCommand>
{
    private readonly IPumpStateService _pumps;
    private readonly IPublishEndpoint  _bus;
    private readonly ILogger<StopPumpConsumer> _log;

    public StopPumpConsumer(IPumpStateService pumps, IPublishEndpoint bus,
        ILogger<StopPumpConsumer> log)
    {
        _pumps = pumps; _bus = bus; _log = log;
    }

    public async Task Consume(ConsumeContext<StopPumpCommand> ctx)
    {
        var pump = _pumps.Get(ctx.Message.PumpId);
        if (pump is null) return;

        pump.Status          = "IDLE";
        pump.LitresDispensed = 0;
        pump.Amount          = 0;
        _pumps.Update(pump);

        _log.LogInformation("[StopPump] Pump {Id} → IDLE", ctx.Message.PumpId);

        await _bus.Publish(new PumpStatusChangedEvent
        {
            PumpId   = pump.Id,
            Status   = "IDLE",
            FuelType = pump.FuelType
        });
    }
}
