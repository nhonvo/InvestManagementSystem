# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution, CPVM, and projects for restore
COPY InventoryManagementSystem.sln ./
COPY Directory.Packages.props ./
COPY InventoryAlert.Contracts/InventoryAlert.Contracts.csproj ./InventoryAlert.Contracts/
COPY InventoryAlert.Worker/InventoryAlert.Worker.csproj ./InventoryAlert.Worker/
RUN dotnet restore "InventoryAlert.Worker/InventoryAlert.Worker.csproj"

# Copy all source and publish
COPY . .
RUN dotnet publish "InventoryAlert.Worker/InventoryAlert.Worker.csproj" \
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

ENTRYPOINT ["dotnet", "InventoryAlert.Worker.dll"]
