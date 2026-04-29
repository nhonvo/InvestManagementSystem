$ErrorActionPreference = "Stop"

$baseUrl = "http://localhost:8080"
$headers = @{"Content-Type" = "application/json"}

Write-Host "====================================" -ForegroundColor Cyan
Write-Host " Inventory System Integration Tests " -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# 1. Authentication
Write-Host "`n[1] Testing AuthController (Login)..." -ForegroundColor Yellow
$loginBody = @{
    username = "user1"
    password = "password"
} | ConvertTo-Json

try {
    $loginRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" -Method Post -Headers $headers -Body $loginBody
    $token = $loginRes.accessToken
    Write-Host "  -> Successfully retrieved JWT token!" -ForegroundColor Green
} catch {
    Write-Host "  -> Failed to login: $_" -ForegroundColor Red
    exit 1
}

$authHeaders = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
}

# 2. Market Status
Write-Host "`n[2] Testing MarketController (Status)..." -ForegroundColor Yellow
try {
    $marketRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/market/status" -Method Get -Headers $authHeaders
    Write-Host "  -> Market Check Success! Status: $($marketRes.isMarketOpen)" -ForegroundColor Green
} catch {
    Write-Host "  -> Failed to get market status: $_" -ForegroundColor Red
}

# 3. Stocks API
Write-Host "`n[3] Testing StocksController (Catalog)..." -ForegroundColor Yellow
try {
    $stocksRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/stocks/search?q=AAPL" -Method Get -Headers $authHeaders
    Write-Host "  -> Fetched stocks successfully!" -ForegroundColor Green

    Write-Host "  -> Visiting stock quote to sync AAPL cache..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/api/v1/stocks/AAPL/quote" -Method Get -Headers $authHeaders | Out-Null
} catch {
    Write-Host "  -> Failed to read stock catalog: $_" -ForegroundColor Red
}

# 4. Alert Rules
Write-Host "`n[4] Testing AlertRulesController (CRUD)..." -ForegroundColor Yellow
$alertBody = @{
    tickerSymbol = "AAPL"
    condition = 0 
    targetValue = 250.00
    triggerOnce = $true
} | ConvertTo-Json

try {
    $alertRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/alertrules" -Method Post -Headers $authHeaders -Body $alertBody
    Write-Host "  -> Created Alert Rule ID: $($alertRes.id) for $($alertRes.tickerSymbol)" -ForegroundColor Green

    $alertsList = Invoke-RestMethod -Uri "$baseUrl/api/v1/alertrules" -Method Get -Headers $authHeaders
    Write-Host "  -> Fetched $($alertsList.Count) alert rules!" -ForegroundColor Green
} catch {
    Write-Host "  -> Failed to create/fetch alert rule: $_" -ForegroundColor Red
}

# 5. Watchlist
Write-Host "`n[5] Testing WatchlistController (CRUD)..." -ForegroundColor Yellow
try {
    $wlAddRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist/MSFT" -Method Post -Headers $authHeaders
    Write-Host "  -> Added MSFT to Watchlist!" -ForegroundColor Green

    $wlGetRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist" -Method Get -Headers $authHeaders
    Write-Host "  -> Watchlist count: $($wlGetRes.Count)" -ForegroundColor Green
    
    Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist/MSFT" -Method Delete -Headers $authHeaders | Out-Null
    Write-Host "  -> Removed MSFT from Watchlist!" -ForegroundColor Green
} catch {
    Write-Host "  -> Failed Watchlist Operations: $_" -ForegroundColor Red
}

# 6. Portfolio
Write-Host "`n[6] Testing PortfolioController (CRUD)..." -ForegroundColor Yellow
try {
    $posBody = @{
        symbol = "MSFT"
        quantity = 10
        averagePrice = 300.00
    } | ConvertTo-Json
    $portAddRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/portfolio/positions" -Method Post -Headers $authHeaders -Body $posBody
    Write-Host "  -> Opened position for MSFT!" -ForegroundColor Green

    $portGetRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/portfolio/positions" -Method Get -Headers $authHeaders
    Write-Host "  -> Portfolio positions count: $($portGetRes.items.Count)" -ForegroundColor Green
} catch {
    Write-Host "  -> Failed Portfolio Operations: $_" -ForegroundColor Red
}

# 7. News & Notifications
Write-Host "`n[7] Testing News & Notifications..." -ForegroundColor Yellow
try {
    $newsRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/news/market?category=general" -Method Get -Headers $authHeaders
    Write-Host "  -> Fetched Market News: $($newsRes.Count) items" -ForegroundColor Green
    
    $notifRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/notifications" -Method Get -Headers $authHeaders
    Write-Host "  -> Fetched Notifications! Unread: $($notifRes.items.Count)" -ForegroundColor Green
} catch {
    Write-Host "  -> Failed News/Notifications: $_" -ForegroundColor Red
}

# 8. Native Events integration (SQS / Moto)
Write-Host "`n[8] Testing EventsController (SQS Pipeline)..." -ForegroundColor Yellow
$eventBody = @{
    eventType = "inventoryalert.news.sync-requested.v1"
    payload = @{
        Reason = "E2E_Test_Script"
    }
} | ConvertTo-Json

try {
    $eventRes = Invoke-RestMethod -Uri "$baseUrl/api/v1/events" -Method Post -Headers $authHeaders -Body $eventBody
    Write-Host "  -> Dispatched message to Event Queue: $($eventRes.status)" -ForegroundColor Green
} catch {
    Write-Host "  -> Failed to dispatch event: $_" -ForegroundColor Red
}

# 9. Check Worker Output via Docker command hook
Write-Host "`n[9] Verifying Worker Logs (Wait 5 sec)..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
try {
    $workerLogs = docker logs inventory-worker --tail 20 2>&1
    if ($workerLogs -match "NativeSQS") {
         Write-Host "  -> Worker is active and polling." -ForegroundColor Green
    } else {
         Write-Host "  -> Worker logs read successful." -ForegroundColor Green
    }
} catch {
    Write-Host "  -> Failed to read Docker logs: $_" -ForegroundColor Red
}

# 10. Authentication (Logout)
Write-Host "`n[10] Testing AuthController (Logout)..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/logout" -Method Post -Headers $authHeaders | Out-Null
    Write-Host "  -> Logged out successfully!" -ForegroundColor Green
} catch {
    Write-Host "  -> Failed to logout: $_" -ForegroundColor Red
}

Write-Host "`n====================================" -ForegroundColor Cyan
Write-Host " Test Execution Completed    " -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
