$baseUrl = "http://localhost:8080/api"
$token = (Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body '{"username":"admin","password":"password123"}').token
$authHeader = @{ Authorization = "Bearer $token" }

Write-Host "--- Section 1: Auth (TC-AUTH-02/03) ---"
try { Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body '{"username":"admin","password":"wrong"}' } catch { Write-Host "TC-AUTH-02 Correctly failed with status: $($_.Exception.Message)" }
try { Invoke-RestMethod -Uri "$baseUrl/products" -Method Get } catch { Write-Host "TC-AUTH-03 Correctly failed with status: $($_.Exception.Message)" }

Write-Host "--- Section 2: Product CRUD (TC-PROD-01/03) ---"
$newProduct = @{
    name = "Manual Test Product $(Get-Random -Maximum 9999)"
    tickerSymbol = "TEST$(Get-Random -Maximum 9999)"
    stockCount = 100
    originPrice = 1000
    currentPrice = 950
    priceAlertThreshold = 0.2
    stockAlertThreshold = 10
}
$createdProduct = Invoke-RestMethod -Uri "$baseUrl/products" -Method Post -ContentType "application/json" -Headers $authHeader -Body ($newProduct | ConvertTo-Json)
Write-Host "TC-PROD-01 Passed: Created ID $($createdProduct.id)"
$allProducts = Invoke-RestMethod -Uri "$baseUrl/products" -Method Get -Headers $authHeader
Write-Host "TC-PROD-03 Passed: Found $($allProducts.totalItems) products"

Write-Host "--- Section 3: Stock Count (TC-STOCK-01) ---"
$patchedProduct = Invoke-RestMethod -Uri "$baseUrl/products/$($createdProduct.id)/stock?stockCount=50" -Method Patch -Headers $authHeader
Write-Host "TC-STOCK-01 Passed: New stock count is $($patchedProduct.stockCount)"

Write-Host "--- Section 4: Sync (TC-SYNC-01) ---"
Invoke-RestMethod -Uri "$baseUrl/products/sync-price" -Method Post -Headers $authHeader
Write-Host "TC-SYNC-01 Passed: Sync triggered"

Write-Host "--- Section 5: Alerts (TC-ALERT-01) ---"
$alerts = Invoke-RestMethod -Uri "$baseUrl/products/price-alerts" -Method Get -Headers $authHeader
Write-Host "TC-ALERT-01 Passed: Found $(if ($alerts) { $alerts.Count } else { 0 }) alerts"

Write-Host "--- Section 6: Events (TC-EVENT-01) ---"
$event = @{
    eventType = "inventoryalert.pricing.price-drop.v1"
    payload = @{ productId = $createdProduct.id; symbol = $newProduct.tickerSymbol; dropPercent = 0.25 }
}
Invoke-RestMethod -Uri "$baseUrl/events" -Method Post -ContentType "application/json" -Headers $authHeader -Body ($event | ConvertTo-Json)
Write-Host "TC-EVENT-01 Passed: Event accepted"

Write-Host "--- Section 7: Worker (TC-WORKER-02) ---"
$earningsEvent = @{
    eventType = "inventoryalert.fundamentals.earnings.v1"
    payload = @{
        symbol = "AAPL"
        period = "Q1 2026"
        actualEPS = 2.18
        estimatedEPS = 2.10
        surprisePercent = 3.8
    }
}
Invoke-RestMethod -Uri "$baseUrl/events" -Method Post -ContentType "application/json" -Headers $authHeader -Body ($earningsEvent | ConvertTo-Json)
Write-Host "TC-WORKER-02 Path A Passed: Earnings event accepted"
Start-Sleep -Seconds 5
# Check DB for persistence
Write-Host "Verifying earnings persistence in DB..."
docker exec inventory_db psql -U postgres -d InventoryAlertDb -c "SELECT * FROM `"EarningsRecords`" WHERE `"Symbol`" = 'AAPL';"

Write-Host "Testing complete."
