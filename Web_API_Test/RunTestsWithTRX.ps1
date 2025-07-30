# PowerShell script to run tests and keep TRX files
param(
    [string]$TestFilter = "",
    [string]$Configuration = "Debug"
)

# Create timestamp for unique TRX filename
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$trxFileName = "TestResults_$timestamp.trx"
$resultsDirectory = "TestResults"

# Ensure TestResults directory exists
if (!(Test-Path $resultsDirectory)) {
    New-Item -ItemType Directory -Path $resultsDirectory -Force
    Write-Host "Created TestResults directory" -ForegroundColor Green
}

# Build the dotnet test command
$testCommand = "dotnet test"
$testCommand += " --configuration $Configuration"
$testCommand += " --logger `"trx;LogFileName=TestResults_$timestamp.trx`""
$testCommand += " --results-directory $resultsDirectory"
$testCommand += " --settings test.runsettings"
$testCommand += " --verbosity normal"

if ($TestFilter) {
    $testCommand += " --filter `"$TestFilter`""
}

Write-Host "Running command: $testCommand" -ForegroundColor Cyan
Write-Host "TRX file will be saved as: $resultsDirectory\$trxFileName" -ForegroundColor Yellow

# Execute the test command
Invoke-Expression $testCommand

# Check if TRX file was created
$trxPath = Join-Path $resultsDirectory $trxFileName
if (Test-Path $trxPath) {
    Write-Host "✅ TRX file successfully created: $trxPath" -ForegroundColor Green
    Write-Host "File size: $((Get-Item $trxPath).Length) bytes" -ForegroundColor Green
} else {
    Write-Host "❌ TRX file was not created" -ForegroundColor Red
}

# List all TRX files in TestResults
Write-Host "`nAll TRX files in TestResults:" -ForegroundColor Cyan
Get-ChildItem -Path $resultsDirectory -Filter "*.trx" | Sort-Object LastWriteTime -Descending | ForEach-Object {
    Write-Host "  $($_.Name) - $(Get-Date $_.LastWriteTime -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
}
