# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first — Docker caches this layer unless .csproj changes
COPY src/MiniPOS.Shared/MiniPOS.Shared.csproj     src/MiniPOS.Shared/
COPY src/POS.WebHost/POS.WebHost.csproj            src/POS.WebHost/
RUN dotnet restore src/POS.WebHost/POS.WebHost.csproj

# Copy source and publish
COPY src/MiniPOS.Shared/  src/MiniPOS.Shared/
COPY src/POS.WebHost/     src/POS.WebHost/
RUN dotnet publish src/POS.WebHost/POS.WebHost.csproj \
    -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime (no SDK — much smaller image) ────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "POS.WebHost.dll"]
