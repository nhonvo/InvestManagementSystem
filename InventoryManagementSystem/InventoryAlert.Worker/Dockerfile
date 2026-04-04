# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore (layer cache-friendly)
COPY InventoryManagementSystem.sln ./
COPY InventoryAlert.Contracts/InventoryAlert.Contracts.csproj ./InventoryAlert.Contracts/
COPY InventoryAlert.Worker/InventoryAlert.Worker.csproj ./InventoryAlert.Worker/
RUN dotnet restore "InventoryAlert.Worker/InventoryAlert.Worker.csproj"

# Copy all source and publish
COPY . .
RUN dotnet publish "InventoryAlert.Worker/InventoryAlert.Worker.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish

# Stage 2: Runtime (minimal image — worker service, no HTTP)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "InventoryAlert.Worker.dll"]
