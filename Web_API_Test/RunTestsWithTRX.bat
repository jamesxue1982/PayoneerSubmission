@echo off
REM Batch script to run tests and keep TRX files

REM Create timestamp for unique TRX filename
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "timestamp=%dt:~0,8%_%dt:~8,6%"
set "trxFileName=TestResults_%timestamp%.trx"
set "resultsDirectory=TestResults"

REM Ensure TestResults directory exists
if not exist "%resultsDirectory%" (
    mkdir "%resultsDirectory%"
    echo Created TestResults directory
)

REM Build and run the dotnet test command
echo Running tests with TRX file preservation...
echo TRX file will be saved as: %resultsDirectory%\TestResults_%timestamp%.trx

dotnet test --configuration Debug --logger "trx;LogFileName=TestResults_%timestamp%.trx" --results-directory %resultsDirectory% --settings test.runsettings --verbosity normal

REM Check if TRX file was created
if exist "%resultsDirectory%\TestResults_%timestamp%.trx" (
    echo ✅ TRX file successfully created: %resultsDirectory%\TestResults_%timestamp%.trx
) else (
    echo ❌ TRX file was not created
)

REM List all TRX files in TestResults
echo.
echo All TRX files in TestResults:
dir "%resultsDirectory%\*.trx" /o-d

pause
