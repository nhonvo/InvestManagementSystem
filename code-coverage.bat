@echo off
REM ========================
REM Set Tools & Paths
REM ========================
SET "dotnet=dotnet"
SET "testproject=./InventoryManagementSystem/InventoryAlert.UnitTests/InventoryAlert.UnitTests.csproj"
SET "coveragedir=./coverage/"

echo [info] Ensuring tools are installed...
dotnet tool install --global dotnet-reportgenerator-globaltool
dotnet tool install --global coverlet.console

REM ========================
REM Run Tests & Coverage
REM ========================
echo [info] Cleaning old coverage data...
if exist "%coveragedir%" rd /s /q "%coveragedir%"
mkdir "%coveragedir%"

echo [info] Cleaning build artifacts...
%dotnet% clean %testproject%

echo [info] Running tests with coverlet collector...
dotnet test %testproject% --collect:"XPlat Code Coverage" --results-directory %coveragedir%

REM ========================
REM Generate HTML Report
REM ========================
echo [info] Generating merged report...
reportgenerator ^
  "-reports:%coveragedir%\**\coverage.cobertura.xml" ^
  "-targetdir:%coveragedir%\html" ^
  -reporttypes:Html;TextSummary

REM ========================
REM Open the Report
REM ========================
echo [info] Opening HTML report...
start "" "%coveragedir%\html\index.html"
