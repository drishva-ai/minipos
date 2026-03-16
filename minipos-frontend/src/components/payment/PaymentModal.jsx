import { useState, useEffect } from 'react';

const METHODS = [
  { id: 'Cash',        label: 'Cash',        icon: '💵' },
  { id: 'Card',        label: 'Card',        icon: '💳' },
  { id: 'Contactless', label: 'Contactless', icon: '📱' },
  { id: 'Voucher',     label: 'Voucher',     icon: '🎟' }
];

export default function PaymentModal({ basket, invoke, onClose }) {
  const [method,  setMethod]  = useState('Cash');
  const [phase,   setPhase]   = useState('select');   // select | processing | error

  useEffect(() => {
    // Payment result comes via App.jsx → on('PaymentCompleted') → resetBasket → closes modal
    // We don't need to handle success here — the toast + basket reset handles it
  }, []);

  const handleConfirm = async () => {
    setPhase('processing');
    try {
      await invoke('ProcessPayment', basket.basketId, method, basket.total, basket.items ?? []);
      // Modal stays "processing" until PaymentCompleted event fires (800ms for card)
      // App.jsx handles PaymentCompleted → toast + resetBasket
      setTimeout(() => onClose(), 2000);
    } catch {
      setPhase('error');
    }
  };

  return (
    <div className="modal-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="modal-card">

        {phase === 'select' && (
          <>
            <h2 className="modal-title">Payment</h2>
            <div className="payment-total">£{Number(basket.total).toFixed(2)}</div>
            <p className="payment-count">{basket.items?.length ?? 0} items in basket</p>

            <div className="payment-methods">
              {METHODS.map(m => (
                <button
                  key={m.id}
                  className={`method-btn ${method === m.id ? 'active' : ''}`}
                  onClick={() => setMethod(m.id)}
                >
                  <span className="method-btn__icon">{m.icon}</span>
                  <span className="method-btn__label">{m.label}</span>
                </button>
              ))}
            </div>

            <div className="modal-actions">
              <button className="btn-ghost" onClick={onClose}>Cancel</button>
              <button className="btn-primary" onClick={handleConfirm}>
                Confirm {method}
              </button>
            </div>
          </>
        )}

        {phase === 'processing' && (
          <div className="payment-processing">
            <div className="spinner" />
            <p>
              {method === 'Card' || method === 'Contactless'
                ? '📡 Contacting EFT terminal…'
                : '⏳ Processing payment…'}
            </p>
          </div>
        )}

        {phase === 'error' && (
          <div className="payment-error">
            <div className="payment-error__icon">⚠️</div>
            <h2>Payment Failed</h2>
            <p>Please check the terminal and retry.</p>
            <div className="modal-actions">
              <button className="btn-ghost" onClick={onClose}>Cancel</button>
              <button className="btn-primary" onClick={() => setPhase('select')}>Retry</button>
            </div>
          </div>
        )}

      </div>
    </div>
  );
}
