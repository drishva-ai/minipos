import axios from 'axios';

/**
 * Base URLs for each microservice.
 *
 * Local dev (.env.local):
 *   VITE_API_URL=http://localhost:5000        ← POS.WebHost
 *   VITE_ARTICLES_URL=http://localhost:5002   ← Articles.Service
 *   VITE_BASKET_URL=http://localhost:5001     ← Basket.Service
 *   VITE_PAYMENT_URL=http://localhost:5003    ← Payment.Service
 *   VITE_FORECOURT_URL=http://localhost:5004  ← Forecourt.Service
 *
 * Production (Render env vars): same keys, different values
 */
const POS_URL       = import.meta.env.VITE_API_URL;
const ARTICLES_URL  = import.meta.env.VITE_ARTICLES_URL;
const BASKET_URL    = import.meta.env.VITE_BASKET_URL;
const PAYMENT_URL   = import.meta.env.VITE_PAYMENT_URL;
const FORECOURT_URL = import.meta.env.VITE_FORECOURT_URL;

// ── Axios factory ─────────────────────────────────────────────────────────────
function createHttp(baseURL) {
  const instance = axios.create({ baseURL, timeout: 10_000 });

  instance.interceptors.request.use(config => {
    const token = sessionStorage.getItem('pos_token');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
  });

  instance.interceptors.response.use(
    res => res,
    err => {
      if (err.response?.status === 401) {
        sessionStorage.clear();
        window.location.reload();
      }
      return Promise.reject(err);
    }
  );

  return instance;
}

const http          = createHttp(POS_URL);
const httpArticles  = createHttp(ARTICLES_URL);
const httpBasket    = createHttp(BASKET_URL);
const httpPayment   = createHttp(PAYMENT_URL);
const httpForecourt = createHttp(FORECOURT_URL);

// ── Auth API  (POS.WebHost :5000) ─────────────────────────────────────────────
export const AuthApi = {
  login: (username, password) =>
    http.post('/api/auth/login', { username, password }).then(r => r.data),

  verify: () => http.get('/api/auth/verify').then(r => r.data),
};

// ── Articles API  (Articles.Service :5002) ────────────────────────────────────
export const ArticlesApi = {
  getAll: (category) =>
    httpArticles.get('/api/v1/articles', { params: category ? { category } : {} }).then(r => r.data),

  getCategories: () =>
    httpArticles.get('/api/v1/articles/categories').then(r => r.data),

  getByBarcode: (barcode) =>
    httpArticles.get(`/api/v1/articles/barcode/${barcode}`).then(r => r.data),
};

// ── Forecourt API  (Forecourt.Service :5004) ──────────────────────────────────
export const ForecourtApi = {
  getPumps: () => httpForecourt.get('/api/forecourt/pumps').then(r => r.data),
};

// ── Basket API  (Basket.Service :5001) ────────────────────────────────────────
export const BasketApi = {
  get: (basketId) => httpBasket.get(`/api/basket/${basketId}`).then(r => r.data),
};

// ── Payment API  (Payment.Service :5003) ──────────────────────────────────────
export const PaymentApi = {
  getTransactions: () => httpPayment.get('/api/payment/transactions').then(r => r.data),
};
