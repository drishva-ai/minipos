import { useEffect, useState, useRef, useCallback } from 'react';
import { createConnection, startConnection } from '../services/signalRService';

/**
 * Custom hook — manages SignalR connection lifecycle.
 *
 * Usage:
 *   const { connection, status, invoke } = useSignalR(token);
 *   invoke('AddItem', basketId, barcode);
 *
 * @param {string|null} token - JWT token. Pass null to disconnect.
 */
export function useSignalR(token) {
  const [status,     setStatus]     = useState('disconnected');
  const connectionRef               = useRef(null);

  useEffect(() => {
    if (!token) return;

    const conn = createConnection(token);
    connectionRef.current = conn;

    conn.onreconnecting(() => setStatus('reconnecting'));
    conn.onreconnected(()   => setStatus('connected'));
    conn.onclose(()         => setStatus('disconnected'));

    startConnection(conn).then(() => setStatus('connected'));

    return () => {
      conn.stop().catch(() => {});
      setStatus('disconnected');
    };
  }, [token]);

  /**
   * Invoke a hub method.
   * @param {string} method
   * @param {...any} args
   */
  const invoke = useCallback(async (method, ...args) => {
    const conn = connectionRef.current;
    if (!conn || conn.state !== 'Connected') {
      console.warn(`[SignalR] Cannot invoke ${method} — not connected`);
      return;
    }
    try {
      await conn.invoke(method, ...args);
    } catch (err) {
      console.error(`[SignalR] ${method} failed:`, err);
      throw err;
    }
  }, []);

  /**
   * Register a hub event handler.
   * @param {string} event
   * @param {Function} handler
   */
  const on = useCallback((event, handler) => {
    connectionRef.current?.on(event, handler);
    return () => connectionRef.current?.off(event, handler);
  }, []);

  return {
    connection: connectionRef.current,
    status,
    invoke,
    on
  };
}
