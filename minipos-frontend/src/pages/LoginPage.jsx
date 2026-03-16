import { useState } from 'react';

export default function LoginPage({ onLogin }) {
  const [username, setUsername] = useState('cashier@pos');
  const [password, setPassword] = useState('password123');
  const [loading,  setLoading]  = useState(false);
  const [error,    setError]    = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true); setError('');
    try {
      await onLogin(username, password);
    } catch (err) {
      setError(err.response?.data?.detail ?? 'Invalid username or password');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-root">
      <div className="login-card">
        <div className="login-brand">
          <span className="login-icon">⛽</span>
          <h1>Mini POS</h1>
          <p>Vynamic FCx — Petrol Station</p>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <div className="field">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={e => setUsername(e.target.value)}
              placeholder="cashier@pos"
              autoComplete="username"
              required
            />
          </div>
          <div className="field">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="••••••••"
              autoComplete="current-password"
              required
            />
          </div>
          {error && <p className="login-error">{error}</p>}
          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign In'}
          </button>
        </form>

        <div className="login-creds">
          <p className="creds-title">Demo Credentials</p>
          <table>
            <tbody>
              <tr><td>cashier@pos</td><td>password123</td><td>Cashier</td></tr>
              <tr><td>manager@pos</td><td>manager123</td><td>Manager</td></tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
