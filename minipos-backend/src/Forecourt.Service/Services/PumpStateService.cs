namespace Forecourt.Service.Services;

public sealed class PumpState
{
    public required int    Id              { get; set; }
    public required string FuelType        { get; set; }
    public string   Status          { get; set; } = "IDLE";
    public decimal  LitresDispensed { get; set; }
    public decimal  Amount          { get; set; }
    public DateTime? StartedAt      { get; set; }
}

public interface IPumpStateService
{
    IReadOnlyList<PumpState> GetAll();
    PumpState? Get(int pumpId);
    void Update(PumpState pump);
}

public sealed class PumpStateService : IPumpStateService
{
    private readonly List<PumpState> _pumps =
    [
        new() { Id = 1, FuelType = "Diesel"   },
        new() { Id = 2, FuelType = "Unleaded" },
        new() { Id = 3, FuelType = "Premium"  },
        new() { Id = 4, FuelType = "Diesel"   },
        new() { Id = 5, FuelType = "Unleaded" },
        new() { Id = 6, FuelType = "Premium"  }
    ];

    private readonly object _lock = new();   // 'Lock' type requires .NET 9 — use object

    public IReadOnlyList<PumpState> GetAll()
    {
        lock (_lock) return _pumps.ToList();
    }

    public PumpState? Get(int id)
    {
        lock (_lock) return _pumps.FirstOrDefault(p => p.Id == id);
    }

    public void Update(PumpState pump)
    {
        lock (_lock)
        {
            var i = _pumps.FindIndex(p => p.Id == pump.Id);
            if (i >= 0) _pumps[i] = pump;
        }
    }
}
