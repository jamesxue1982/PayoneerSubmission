# PowerShell script to view the latest test log file
# This script finds the most recent test log file and opens it for viewing

param(
    [string]$LogDirectory = "TestResults",
    [switch]$OpenInNotepad
)

# Get the current directory and possible log paths
$currentDir = Get-Location
$logPaths = @(
    (Join-Path $currentDir "TestLogs"),                              # Simple logs path in project root
    (Join-Path $currentDir $LogDirectory),                           # Project-level TestResult path (fallback)
    (Join-Path $currentDir "bin\Debug\net9.0\TestLogs"),            # Legacy build output path
    (Join-Path $currentDir "bin\Release\net9.0\TestLogs")           # Legacy release build path
)

Write-Host "🔍 Searching for test logs..." -ForegroundColor Cyan

$foundLogPath = $null
$latestLog = $null

# Check each possible log directory
foreach ($logPath in $logPaths) {
    Write-Host "   Checking: $logPath" -ForegroundColor DarkGray
    
    if (Test-Path $logPath) {
        $logs = Get-ChildItem -Path $logPath -Filter "*.log" -ErrorAction SilentlyContinue
        if ($logs.Count -gt 0) {
            $foundLogPath = $logPath
            $latestLog = $logs | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            Write-Host "✅ Found logs in: $logPath" -ForegroundColor Green
            break
        }
    }
}

if ($null -eq $latestLog) {
    Write-Host "❌ No log files found in any of these locations:" -ForegroundColor Red
    foreach ($path in $logPaths) {
        Write-Host "   - $path" -ForegroundColor Yellow
    }
    Write-Host "💡 Run some tests first to generate log files." -ForegroundColor Yellow
    Write-Host "💡 Example: dotnet test --filter GetAllTodoItemsEndpointTest" -ForegroundColor Cyan
    exit 1
}

Write-Host ""
Write-Host "📄 Latest log file: $($latestLog.Name)" -ForegroundColor Green
Write-Host "📅 Created: $($latestLog.LastWriteTime)" -ForegroundColor Green
Write-Host "📊 Size: $([math]::Round($latestLog.Length / 1KB, 2)) KB" -ForegroundColor Green
Write-Host "📂 Directory: $foundLogPath" -ForegroundColor Green
Write-Host ""

if ($OpenInNotepad) {
    Write-Host "🚀 Opening log file in Notepad..." -ForegroundColor Yellow
    Start-Process notepad.exe -ArgumentList $latestLog.FullName
} else {
    Write-Host "📖 Log file contents:" -ForegroundColor Yellow
    Write-Host "=" * 80 -ForegroundColor DarkGray
    Get-Content $latestLog.FullName
    Write-Host "=" * 80 -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "💡 Use -OpenInNotepad to open the file in Notepad instead" -ForegroundColor Cyan
}

Write-Host "✨ Log file location: $($latestLog.FullName)" -ForegroundColor Magenta
