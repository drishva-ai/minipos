import { useState } from 'react';
import TopBar from '../components/layout/TopBar';
import BasketPanel from '../components/basket/BasketPanel';
import ProductGrid from '../components/basket/ProductGrid';
import PumpGrid from '../components/forecourt/PumpGrid';
import PaymentModal from '../components/payment/PaymentModal';

export default function PosPage({
  cashier, role, connStatus,
  basket, articles, categories, pumps,
  onAddItem, onRemoveItem, onAbortBasket,
  onAuthorisePump, onStopPump, onAddFuelToBasket,
  onLogout, invoke
}) {
  const [activeTab,     setActiveTab]     = useState('products');
  const [showPayment,   setShowPayment]   = useState(false);
  const [activeCategory, setCategory]    = useState('All');

  return (
    <div className="pos-root">
      <TopBar
        cashier={cashier}
        role={role}
        connStatus={connStatus}
        onLogout={onLogout}
      />

      <div className="pos-body">
        {/* ── Left: Basket ──────────────────────────────────────────── */}
        <BasketPanel
          basket={basket}
          onRemoveItem={onRemoveItem}
          onAbortBasket={onAbortBasket}
          onPayNow={() => setShowPayment(true)}
        />

        {/* ── Right: Products / Forecourt ───────────────────────────── */}
        <div className="pos-right">
          <div className="tabs">
            <button
              className={`tab ${activeTab === 'products' ? 'active' : ''}`}
              onClick={() => setActiveTab('products')}
            >
              🛒 Products
            </button>
            <button
              className={`tab ${activeTab === 'forecourt' ? 'active' : ''}`}
              onClick={() => setActiveTab('forecourt')}
            >
              ⛽ Forecourt
            </button>
          </div>

          {activeTab === 'products' && (
            <ProductGrid
              articles={articles}
              categories={categories}
              activeCategory={activeCategory}
              onSelectCategory={setCategory}
              onAddItem={onAddItem}
            />
          )}

          {activeTab === 'forecourt' && (
            <PumpGrid
              pumps={pumps}
              onAuthorise={onAuthorisePump}
              onStop={onStopPump}
              onAddFuel={onAddFuelToBasket}
            />
          )}
        </div>
      </div>

      {showPayment && (
        <PaymentModal
          basket={basket}
          invoke={invoke}
          onClose={() => setShowPayment(false)}
        />
      )}
    </div>
  );
}
