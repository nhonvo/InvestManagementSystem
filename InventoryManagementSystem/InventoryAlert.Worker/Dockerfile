# === Stage 1: Build ===
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution and project files for layer-cached restore
COPY ["InventoryManagementSystem.sln", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["InventoryAlert.Domain/InventoryAlert.Domain.csproj", "InventoryAlert.Domain/"]
COPY ["InventoryAlert.Infrastructure/InventoryAlert.Infrastructure.csproj", "InventoryAlert.Infrastructure/"]
COPY ["InventoryAlert.Worker/InventoryAlert.Worker.csproj", "InventoryAlert.Worker/"]

# Restore dependencies
RUN dotnet restore "InventoryAlert.Worker/InventoryAlert.Worker.csproj"

# Copy full source and publish
COPY . .
RUN dotnet publish "InventoryAlert.Worker/InventoryAlert.Worker.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish

# === Stage 2: Runtime ===
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Install dependencies (gcompat for native libs usage)
RUN apk add --no-cache icu-libs curl gcompat krb5-libs

# Configure runtime for Alpine (Globalization)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

# Run as non-root user
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryAlert.Worker.dll"]

LABEL description="Hangfire & SQS Processing Worker"
