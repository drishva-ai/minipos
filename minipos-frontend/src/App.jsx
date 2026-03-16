import { useState, useEffect, useCallback } from 'react';
import { Toaster, toast } from 'react-hot-toast';
import { AuthApi, ArticlesApi } from './services/apiService';
import { useSignalR } from './hooks/useSignalR';
import { useBasket, newBasketId } from './hooks/useBasket';
import { usePumps } from './hooks/usePumps';
import LoginPage from './pages/LoginPage';
import PosPage from './pages/PosPage';

export default function App() {
  // ── Auth state ─────────────────────────────────────────────────────────────
  const [token,   setToken]   = useState(() => sessionStorage.getItem('pos_token'));
  const [cashier, setCashier] = useState(() => sessionStorage.getItem('pos_cashier'));
  const [role,    setRole]    = useState(() => sessionStorage.getItem('pos_role'));

  // ── SignalR ────────────────────────────────────────────────────────────────
  const { status: connStatus, invoke, on } = useSignalR(token);

  // ── Basket (state managed via SignalR events) ──────────────────────────────
  const { basket, addItem, removeItem, abortBasket, resetBasket } = useBasket({ invoke, on });

  // ── Pumps (initial REST load + live SignalR updates) ───────────────────────
  const { pumps, authorisePump, stopPump } = usePumps({ invoke, on });

  // ── Product catalogue ──────────────────────────────────────────────────────
  const [articles,   setArticles]   = useState([]);
  const [categories, setCategories] = useState(['All']);

  useEffect(() => {
    if (!token) return;
    ArticlesApi.getAll()
      .then(data => { setArticles(data); })
      .catch(err => console.error('[App] Failed to load articles:', err));
    ArticlesApi.getCategories()
      .then(cats => setCategories(['All', ...cats]))
      .catch(() => {});
  }, [token]);

  // ── Payment completion (reset basket on success) ───────────────────────────
  useEffect(() => {
    return on('PaymentCompleted', data => {
      toast.success(
        `✅ ${data.paymentMethod} — £${Number(data.total).toFixed(2)}\n${data.transactionId}`,
        { duration: 5000 }
      );
      resetBasket();
    });
  }, [on, resetBasket]);

  // ── SignalR connection notifications ──────────────────────────────────────
  useEffect(() => {
    if (connStatus === 'reconnecting') toast.loading('Reconnecting...', { id: 'conn' });
    if (connStatus === 'connected')    toast.dismiss('conn');
    if (connStatus === 'disconnected') toast.error('Disconnected from server', { id: 'conn' });
  }, [connStatus]);

  // ── Login / logout ────────────────────────────────────────────────────────
  const handleLogin = useCallback(async (username, password) => {
    const data = await AuthApi.login(username, password);
    sessionStorage.setItem('pos_token',  data.token);
    sessionStorage.setItem('pos_cashier', data.cashierId);
    sessionStorage.setItem('pos_role',    data.role);
    setToken(data.token);
    setCashier(data.cashierId);
    setRole(data.role);
    toast.success(`Welcome, ${data.cashierId}`);
  }, []);

  const handleLogout = useCallback(() => {
    sessionStorage.clear();
    setToken(null); setCashier(null); setRole(null);
  }, []);

  // ── Render ────────────────────────────────────────────────────────────────
  return (
    <>
      <Toaster
        position="top-right"
        toastOptions={{
          style: { background: '#1a2332', color: '#e8eaed', border: '1px solid #2d3f55' }
        }}
      />
      {!token
        ? <LoginPage onLogin={handleLogin} />
        : <PosPage
            cashier={cashier}
            role={role}
            connStatus={connStatus}
            basket={basket}
            articles={articles}
            categories={categories}
            pumps={pumps}
            onAddItem={addItem}
            onRemoveItem={removeItem}
            onAbortBasket={abortBasket}
            onAuthorisePump={authorisePump}
            onStopPump={stopPump}
            onAddFuelToBasket={(pump) =>
              invoke('AddFuelToBasket', basket.basketId, pump.id,
                pump.fuelType, pump.litresDispensed, pump.amount)}
            onLogout={handleLogout}
            invoke={invoke}
          />
      }
    </>
  );
}
