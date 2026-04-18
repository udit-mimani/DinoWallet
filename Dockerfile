# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files and restore (layer-cached until .csproj changes)
COPY LedgerX.slnx ./
COPY src/LedgerX.Api/LedgerX.Api.csproj ./src/LedgerX.Api/
RUN dotnet restore src/LedgerX.Api/LedgerX.Api.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish src/LedgerX.Api \
    -c Release \
    -o /publish \
    /p:GenerateDocumentationFile=true

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

ENTRYPOINT ["dotnet", "LedgerX.Api.dll"]
