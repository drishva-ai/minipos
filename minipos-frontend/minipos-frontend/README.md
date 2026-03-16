# minipos-frontend

> **React 18 + Vite** — Mini POS UI for Vynamic FCx petrol station  
> Real-time updates via SignalR · JWT auth · Professional dark POS theme

**Backend repo:** [minipos-backend](https://github.com/YOUR_ORG/minipos-backend)

---

## Features

- 🔐 **JWT Login** — POST /api/auth/login → token stored in sessionStorage
- 🛒 **Live Basket** — updates instantly via SignalR (no polling)
- 🛍 **Product Grid** — 14 products from Articles.Service, filterable by category
- ⛽ **Forecourt** — 6 pumps, live status via SignalR PumpStatusChanged events
- 💳 **Payment Modal** — Cash/Card/Contactless/Voucher with EFT simulation
- 🔄 **Auto-reconnect** — SignalR reconnects automatically with exponential backoff

## Architecture

```
src/
├── pages/
│   ├── LoginPage.jsx         — JWT auth form
│   └── PosPage.jsx           — Main POS layout
├── components/
│   ├── layout/
│   │   └── TopBar.jsx        — Connection status, cashier, logout
│   ├── basket/
│   │   ├── BasketPanel.jsx   — Live basket sidebar
│   │   └── ProductGrid.jsx   — Product tiles with category filter
│   ├── forecourt/
│   │   └── PumpGrid.jsx      — 6 pump cards with live status
│   └── payment/
│       └── PaymentModal.jsx  — Payment method selector + EFT wait state
├── hooks/
│   ├── useSignalR.js         — Connection lifecycle + invoke() + on()
│   ├── useBasket.js          — Basket state driven by SignalR events
│   └── usePumps.js           — Pump state: REST initial load + SignalR live
├── services/
│   ├── signalRService.js     — HubConnection builder
│   └── apiService.js         — Typed Axios clients per microservice
└── styles/
    └── global.css            — Design tokens + all component styles
```

## Quick Start

```bash
# Prerequisites: minipos-backend running (docker-compose up)

git clone https://github.com/YOUR_ORG/minipos-frontend
cd minipos-frontend
npm install
npm run dev
# → http://localhost:5173
```

## Login Credentials

| Username | Password | Role |
|----------|----------|------|
| cashier@pos | password123 | Cashier |
| cashier2@pos | password123 | Cashier |
| manager@pos | manager123 | Manager |

## Docker (production)

```bash
# Run alongside the backend
docker build -t minipos-frontend .
docker run -p 3000:80 minipos-frontend
# → http://localhost:3000
```

## Connecting to a deployed backend

Set `VITE_API_URL` at build time or update `vite.config.js` proxy targets.

## GitHub Secrets for CI/CD

| Secret | Value |
|--------|-------|
| `RENDER_DEPLOY_HOOK_FRONTEND` | Render.com deploy hook URL |
