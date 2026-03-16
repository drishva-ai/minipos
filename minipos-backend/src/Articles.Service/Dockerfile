FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/MiniPOS.Shared/MiniPOS.Shared.csproj src/MiniPOS.Shared/
COPY src/Articles.Service/Articles.Service.csproj src/Articles.Service/
RUN dotnet restore src/Articles.Service/Articles.Service.csproj
COPY src/MiniPOS.Shared/ src/MiniPOS.Shared/
COPY src/Articles.Service/ src/Articles.Service/
RUN dotnet publish src/Articles.Service/Articles.Service.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --retries=3 CMD wget --quiet --tries=1 --spider http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Articles.Service.dll"]
