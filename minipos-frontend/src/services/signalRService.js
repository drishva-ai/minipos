import * as signalR from '@microsoft/signalr';

/**
 * SignalR connection factory.
 *
 * Builds a connection to POS.WebHost /hubs/pos.
 * JWT is passed as ?access_token= query param — browsers cannot
 * set custom headers on WebSocket upgrades, so this is the required approach.
 *
 * Reconnect schedule: 0s → 2s → 5s → 10s → 30s → 60s
 *
 * baseURL is read from REACT_APP_API_URL env variable.
 * In production (Render): set REACT_APP_API_URL=https://minipos-backend-api.onrender.com
 * In development: set REACT_APP_API_URL=http://localhost:5000
 *
 * @param {string} token - JWT token from /api/auth/login
 * @returns {signalR.HubConnection}
 */
export function createConnection(token) {
  const baseUrl = import.meta.env.VITE_API_URL;
  return new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/pos`, {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000, 60000])
    .configureLogging(
      import.meta.env.DEV
        ? signalR.LogLevel.Information
        : signalR.LogLevel.Warning
    )
    .build();
}

/**
 * Start the connection with exponential retry on failure.
 * @param {signalR.HubConnection} connection
 */
export async function startConnection(connection) {
  try {
    await connection.start();
    console.info('[SignalR] ✓ Connected to POS.WebHost /hubs/pos');
  } catch (err) {
    console.error('[SignalR] Connection failed — retrying in 5s', err);
    setTimeout(() => startConnection(connection), 5000);
  }
}
