# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files and restore (layer-cached until .csproj changes)
COPY DinoWallet.sln ./
COPY src/DinoWallet.Api/DinoWallet.Api.csproj ./src/DinoWallet.Api/
RUN dotnet restore src/DinoWallet.Api/DinoWallet.Api.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish src/DinoWallet.Api \
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

ENTRYPOINT ["dotnet", "DinoWallet.Api.dll"]
