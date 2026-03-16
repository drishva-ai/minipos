// BasketPanel.jsx
export default function BasketPanel({ basket, onRemoveItem, onAbortBasket, onPayNow }) {
  const { basketId, items = [], total = 0 } = basket;

  return (
    <aside className="basket-panel">
      <div className="basket-panel__header">
        <span className="basket-panel__id">🛒 {basketId}</span>
        {items.length > 0 && (
          <button className="btn-danger btn-sm" onClick={onAbortBasket}>
            ✕ Abort
          </button>
        )}
      </div>

      <div className="basket-panel__items">
        {items.length === 0
          ? <p className="basket-panel__empty">Scan a product to begin</p>
          : items.map(item => (
              <BasketLine key={item.id} item={item} onRemove={onRemoveItem} />
            ))
        }
      </div>

      <div className="basket-panel__footer">
        <div className="basket-total">
          <span>Total</span>
          <span className="basket-total__amount">£{Number(total).toFixed(2)}</span>
        </div>
        {items.length > 0 && (
          <button className="btn-pay" onClick={onPayNow}>
            💳 Pay — £{Number(total).toFixed(2)}
          </button>
        )}
      </div>
    </aside>
  );
}

function BasketLine({ item, onRemove }) {
  return (
    <div className="basket-line">
      <span className="basket-line__emoji">{item.emoji || (item.isFuel ? '⛽' : '📦')}</span>
      <div className="basket-line__info">
        <span className="basket-line__name">{item.name}</span>
        <span className="basket-line__qty">× {item.quantity}</span>
      </div>
      <span className="basket-line__price">£{(item.price * item.quantity).toFixed(2)}</span>
      <button className="btn-icon" onClick={() => onRemove(item.id)} title="Remove">🗑</button>
    </div>
  );
}
