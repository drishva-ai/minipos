# minipos-backend

> **5 .NET 8 Microservices** — Basket · Articles · Payment · Forecourt · POS WebHost  
> Real-time messaging via MassTransit + RabbitMQ · Redis cache · JWT auth · Swagger on every service

---

## Architecture

```
React (minipos-frontend repo)
    │
    │  JWT Bearer Token
    │  WebSocket (SignalR)
    ▼
┌─────────────────────────────────────────────────────┐
│  POS.WebHost  :5000                                  │
│  ┌──────────┐  ┌───────────────────────────────────┐ │
│  │ /api/auth│  │ /hubs/pos  (SignalR Hub)           │ │
│  │  JWT     │  │  [Authorize]                      │ │
│  └──────────┘  │  AddItem() → publish command      │ │
│                │  ProcessPayment() → publish cmd   │ │
│                │  AuthorisePump() → publish cmd    │ │
│                └───────────────────────────────────┘ │
└─────────────────────────┬───────────────────────────┘
                          │
                    RabbitMQ :5672
              ┌───────────┴────────────┐
              │                        │
    basket-queue              payment-queue
    forecourt-queue
              │                        │
     ┌────────▼──────┐       ┌────────▼──────┐
     │ Basket.Service│       │Payment.Service│
     │     :5001     │       │     :5003     │
     │               │       │               │
     │ Redis cache   │       │ Transactions  │
     │ Articles REST │       │ EFT simulate  │
     └───────┬───────┘       └───────┬───────┘
             │                       │
     ┌───────▼───────┐    BasketUpdatedEvent
     │Articles.Service│   PaymentCompletedEvent
     │     :5002      │        │
     └────────────────┘        │
                               │
     ┌─────────────────────────▼──────┐
     │ Forecourt.Service  :5004       │
     │ Background worker auto-completes│
     │ fuelling pumps every 15s        │
     │ PumpStatusChangedEvent          │
     └────────────────────────────────┘
                     │
               All events →
          pos-webhost-events queue →
          POS.WebHost consumers →
          SignalR push → React UI
```

## Services

| Service | Port | Swagger | Responsibility |
|---------|------|---------|----------------|
| POS.WebHost | 5000 | [/swagger](http://localhost:5000/swagger) | JWT auth, SignalR hub, event consumers |
| Basket.Service | 5001 | [/swagger](http://localhost:5001/swagger) | Basket CRUD in Redis via MassTransit |
| Articles.Service | 5002 | [/swagger](http://localhost:5002/swagger) | Product catalogue REST API |
| Payment.Service | 5003 | [/swagger](http://localhost:5003/swagger) | EFT simulation, transaction records |
| Forecourt.Service | 5004 | [/swagger](http://localhost:5004/swagger) | Pump state + FC Controller simulation |

## Quick Start

```bash
# Clone
git clone https://github.com/YOUR_ORG/minipos-backend
cd minipos-backend

# Run everything (builds all 5 services + RabbitMQ + Redis)
docker-compose up --build

# Check services
curl http://localhost:5000/health   # POS.WebHost
curl http://localhost:5002/health   # Articles.Service
```

## Local Development (without Docker)

```bash
# 1. Start infrastructure
docker run -d --name pos-rabbitmq -p 5672:5672 -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=admin -e RABBITMQ_DEFAULT_PASS=password \
  rabbitmq:3.13-management-alpine

docker run -d --name pos-redis -p 6379:6379 redis:7.2-alpine

# 2. Run each service (separate terminal per service)
cd src/Articles.Service  && dotnet run    # localhost:5002
cd src/Forecourt.Service && dotnet run    # localhost:5004
cd src/Basket.Service    && dotnet run    # localhost:5001
cd src/Payment.Service   && dotnet run    # localhost:5003
cd src/POS.WebHost       && dotnet run    # localhost:5000
```

## Technology Decisions

| Tech | Why |
|------|-----|
| **MassTransit** | Abstracts RabbitMQ, provides retry, error queues, consumer pattern |
| **Redis** | Basket state shared across services. < 1ms reads. 30-min TTL auto-expires |
| **SignalR** | Persistent WebSocket connection. React gets live updates, no polling |
| **JWT** | Stateless. Token passed via `?access_token=` for SignalR compatibility |
| **Serilog** | Structured JSON logs. Each service logs its own `[Service.Name]` prefix |
| **Docker multi-stage** | SDK → Compile → Runtime-only image. ~100MB instead of ~500MB |

## GitHub Secrets Required for CI/CD

| Secret | Value |
|--------|-------|
| `RENDER_DEPLOY_POS_WEBHOST` | Render deploy hook URL |
| `RENDER_DEPLOY_BASKET` | Render deploy hook URL |
| `RENDER_DEPLOY_ARTICLES` | Render deploy hook URL |
| `RENDER_DEPLOY_PAYMENT` | Render deploy hook URL |
| `RENDER_DEPLOY_FORECOURT` | Render deploy hook URL |
