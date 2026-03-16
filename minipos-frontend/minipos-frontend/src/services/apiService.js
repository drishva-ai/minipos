import axios from 'axios';

/**
 * Axios instance — automatically attaches JWT Bearer token to every request.
 * Token is stored in sessionStorage after login.
 */
const http = axios.create({
  timeout: 10_000
});

http.interceptors.request.use(config => {
  const token = sessionStorage.getItem('pos_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

http.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      // Token expired — clear session and reload to login screen
      sessionStorage.clear();
      window.location.reload();
    }
    return Promise.reject(err);
  }
);

// ── Auth API  (POS.WebHost :5000) ─────────────────────────────────────────────
export const AuthApi = {
  /**
   * POST /api/auth/login
   * @param {string} username
   * @param {string} password
   * @returns {Promise<{token: string, cashierId: string, role: string}>}
   */
  login: (username, password) =>
    http.post('/api/auth/login', { username, password }).then(r => r.data),

  /** GET /api/auth/verify — check if stored token is still valid */
  verify: () => http.get('/api/auth/verify').then(r => r.data),
};

// ── Articles API  (Articles.Service :5002) ────────────────────────────────────
export const ArticlesApi = {
  /** GET /api/v1/articles — full product grid */
  getAll: (category) =>
    http.get('/api/v1/articles', { params: category ? { category } : {} }).then(r => r.data),

  /** GET /api/v1/articles/categories */
  getCategories: () =>
    http.get('/api/v1/articles/categories').then(r => r.data),

  /** GET /api/v1/articles/barcode/:barcode */
  getByBarcode: (barcode) =>
    http.get(`/api/v1/articles/barcode/${barcode}`).then(r => r.data),
};

// ── Forecourt API  (Forecourt.Service :5004) ──────────────────────────────────
export const ForecourtApi = {
  /** GET /api/forecourt/pumps — initial pump state on page load */
  getPumps: () => http.get('/api/forecourt/pumps').then(r => r.data),
};

// ── Basket API  (Basket.Service :5001 — read-only debug) ──────────────────────
export const BasketApi = {
  get: (basketId) => http.get(`/api/basket/${basketId}`).then(r => r.data),
};

// ── Payment API  (Payment.Service :5003 — transaction history) ────────────────
export const PaymentApi = {
  getTransactions: () => http.get('/api/payment/transactions').then(r => r.data),
};
