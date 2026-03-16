FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/MiniPOS.Shared/MiniPOS.Shared.csproj src/MiniPOS.Shared/
COPY src/Forecourt.Service/Forecourt.Service.csproj src/Forecourt.Service/
RUN dotnet restore src/Forecourt.Service/Forecourt.Service.csproj
COPY src/MiniPOS.Shared/ src/MiniPOS.Shared/
COPY src/Forecourt.Service/ src/Forecourt.Service/
RUN dotnet publish src/Forecourt.Service/Forecourt.Service.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --retries=3 CMD wget --quiet --tries=1 --spider http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Forecourt.Service.dll"]
