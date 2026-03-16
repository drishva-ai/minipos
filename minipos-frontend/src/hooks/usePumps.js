import { useState, useEffect, useCallback } from 'react';
import { ForecourtApi } from '../services/apiService';

/**
 * Manages pump state.
 * Initial state loaded via REST GET /api/forecourt/pumps.
 * Live updates received via SignalR PumpStatusChanged events.
 *
 * @param {{ invoke: Function, on: Function }} signalR
 */
export function usePumps({ invoke, on }) {
  const [pumps, setPumps] = useState([]);

  // Load initial pump state from Forecourt.Service REST API
  useEffect(() => {
    ForecourtApi.getPumps()
      .then(data => setPumps(data.map(normalisePump)))
      .catch(err => console.error('[usePumps] Failed to load pumps:', err));
  }, []);

  // Live updates from Forecourt.Service via SignalR
  useEffect(() => {
    return on('PumpStatusChanged', data => {
      setPumps(prev => prev.map(p =>
        p.id === data.pumpId
          ? { ...p, status: data.status, fuelType: data.fuelType,
              litresDispensed: data.litresDispensed, amount: data.amount }
          : p
      ));
    });
  }, [on]);

  const authorisePump = useCallback((pumpId) =>
    invoke('AuthorisePump', pumpId),
  [invoke]);

  const stopPump = useCallback((pumpId) =>
    invoke('StopPump', pumpId),
  [invoke]);

  return { pumps, authorisePump, stopPump };
}

/** Normalise keys from .NET PascalCase to camelCase */
function normalisePump(p) {
  return {
    id:              p.id              ?? p.Id,
    fuelType:        p.fuelType        ?? p.FuelType        ?? '',
    status:          p.status          ?? p.Status          ?? 'IDLE',
    litresDispensed: p.litresDispensed ?? p.LitresDispensed ?? 0,
    amount:          p.amount          ?? p.Amount          ?? 0
  };
}
