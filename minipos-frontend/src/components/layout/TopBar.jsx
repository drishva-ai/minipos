const STATUS_COLOUR = {
  connected:    '#4caf50',
  reconnecting: '#ff9800',
  disconnected: '#f44336'
};

export default function TopBar({ cashier, role, connStatus, onLogout }) {
  return (
    <header className="top-bar">
      <div className="top-bar__brand">
        <span className="top-bar__logo">⛽</span>
        <span className="top-bar__title">Mini POS — Vynamic FCx</span>
      </div>

      <div className="top-bar__right">
        <span className="top-bar__status" style={{ color: STATUS_COLOUR[connStatus] }}>
          ● {connStatus === 'connected' ? 'Live' : connStatus}
        </span>
        <span className="top-bar__cashier">
          👤 {cashier}
          {role && <span className="top-bar__role">{role}</span>}
        </span>
        <button className="btn-ghost btn-sm" onClick={onLogout}>
          Sign Out
        </button>
      </div>
    </header>
  );
}
