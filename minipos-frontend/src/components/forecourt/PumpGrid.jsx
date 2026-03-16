const STATUS_MAP = {
  IDLE:     { label: 'IDLE',      cls: 'idle',     icon: '💤' },
  FUELLING: { label: 'FUELLING',  cls: 'fuelling', icon: '⛽' },
  DONE:     { label: 'COMPLETE',  cls: 'done',     icon: '✅' }
};

export default function PumpGrid({ pumps, onAuthorise, onStop, onAddFuel }) {
  return (
    <div className="pump-grid">
      {pumps.map(pump => (
        <PumpCard
          key={pump.id}
          pump={pump}
          onAuthorise={onAuthorise}
          onStop={onStop}
          onAddFuel={onAddFuel}
        />
      ))}
    </div>
  );
}

function PumpCard({ pump, onAuthorise, onStop, onAddFuel }) {
  const s = STATUS_MAP[pump.status] ?? STATUS_MAP.IDLE;

  return (
    <div className={`pump-card pump-card--${s.cls}`}>
      <div className="pump-card__header">
        <span className="pump-card__number">Pump {pump.id}</span>
        <span className="pump-card__fuel">{pump.fuelType}</span>
        <span className={`pump-badge pump-badge--${s.cls}`}>{s.icon} {s.label}</span>
      </div>

      {pump.status === 'FUELLING' && (
        <div className="pump-fuelling">
          <div className="pump-fuelling__anim">⛽ Fuelling in progress…</div>
          <button className="btn-ghost btn-sm mt-8" onClick={() => onStop(pump.id)}>
            Stop Pump
          </button>
        </div>
      )}

      {pump.status === 'DONE' && (
        <div className="pump-done">
          <div className="pump-done__stat">{pump.litresDispensed?.toFixed(2)} L</div>
          <div className="pump-done__amount">£{pump.amount?.toFixed(2)}</div>
          <button className="btn-primary btn-sm" onClick={() => onAddFuel(pump)}>
            + Add to Basket
          </button>
        </div>
      )}

      {pump.status === 'IDLE' && (
        <button className="btn-outline btn-sm pump-card__authorise"
          onClick={() => onAuthorise(pump.id)}>
          Authorise Pump
        </button>
      )}
    </div>
  );
}
