# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore (layer cache-friendly)
COPY InventoryManagementSystem.sln ./
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

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "InventoryAlert.Api.dll"]
