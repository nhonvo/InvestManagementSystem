# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy ALL referenced .csproj files first for layer-cached restore
# InventoryAlert.Api references InventoryAlert.Contracts — both must be present
# Copy solution and CPVM file
COPY InventoryManagementSystem.sln ./
COPY Directory.Packages.props ./
COPY InventoryAlert.Contracts/InventoryAlert.Contracts.csproj ./InventoryAlert.Contracts/
COPY InventoryAlert.Api/InventoryAlert.Api.csproj ./InventoryAlert.Api/
RUN dotnet restore "InventoryAlert.Api/InventoryAlert.Api.csproj"

# Copy all source and publish
COPY . .
RUN dotnet publish "InventoryAlert.Api/InventoryAlert.Api.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish

# Stage 2: Runtime (minimal image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "InventoryAlert.Api.dll"]
