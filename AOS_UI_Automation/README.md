# AOS UI Automation

A comprehensive UI automation test suite for the Advantage Online Shopping (AOS) website using Microsoft Playwright and MSTest framework.

## Overview

This project automates the testing of shopping cart functionality on the Advantage Online Shopping website, including:
- Product addition to cart
- Price validation and calculations
- Quantity verification
- Cart total validation

## Technology Stack

- **Framework**: .NET 9.0
- **Testing Framework**: MSTest
- **UI Automation**: Microsoft Playwright
- **Language**: C# with latest language features
- **IDE**: Visual Studio Code / Visual Studio

## Project Structure

```
AOS_UI_Automation/
├── AOS_ShoppingCart_Test.cs      # Main test class with shopping cart tests
├── TestConfiguration.cs          # Configuration loader for JSON settings
├── PlaywrightSettings.json       # Test configuration and browser settings
├── AOS_UI_Automation.csproj      # Project file with dependencies
├── AOS_UI_Automation.sln         # Solution file
├── MSTestSettings.cs             # MSTest configuration
├── Logs/                         # Test execution log files (auto-generated)
├── .gitignore                    # Git ignore rules for build artifacts and logs
└── README.md                     # This file
```

## Configuration

The project uses `PlaywrightSettings.json` for centralized configuration:

```json
{
  "BaseUrl": "https://www.advantageonlineshopping.com/",
  "DefaultTimeout": 30000,
  "BrowserName": "chromium",
  "LaunchOptions": {
    "Headless": true,
    "Args": ["--start-maximized", "--disable-web-security", "--disable-features=VizDisplayCompositor"],
    "SlowMo": 1000
  }
}
```

### Configuration Parameters

- `BaseUrl`: Target website URL
- `DefaultTimeout`: Page operation timeout (milliseconds)
- `BrowserName`: Browser to use for testing
- `LaunchOptions.Headless`: Run browser in headless mode (true/false)
- `LaunchOptions.Args`: Browser command line arguments
- `LaunchOptions.SlowMo`: Slow motion delay for debugging (milliseconds)

## Prerequisites

- .NET 9.0 SDK or later
- Visual Studio Code or Visual Studio 2022

## Installation

1. Clone the repository
2. Navigate to the project directory:
   ```bash
   cd AOS_UI_Automation
   ```

3. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

4. Install Playwright browsers:
   ```bash
   pwsh bin/Debug/net9.0/playwright.ps1 install
   ```
   Or on Linux/Mac:
   ```bash
   ./bin/Debug/net9.0/playwright.sh install
   ```

## Running Tests

### Command Line

Run all tests:
```bash
dotnet test
```

Run tests with detailed output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

Run specific test category:
```bash
dotnet test --filter Category=ShoppingCart
```

### Visual Studio Code

1. Install the "C# Dev Kit" extension
2. Open the Test Explorer
3. Run tests individually or all at once

## Test Scenarios

### Shopping Cart Tests (`AOS_AddToCart_PriceValidation`)

This test validates the complete shopping cart workflow:

1. **Product Addition**: Adds three different products to cart:
   - HP ZBook 17 G2 Mobile Workstation (Laptop) - Quantity: 1
   - HP Z8000 Bluetooth Mouse (Mouse) - Quantity: 2
   - HP Elite x2 1011 G1 Tablet (Tablet) - Quantity: 1

2. **Price Validation**: Verifies individual product prices and total calculations

3. **Cart Validation**: Confirms:
   - Correct number of products in cart
   - Accurate quantities for each product
   - Proper price calculations
   - Overall cart total accuracy

## Key Features

- **Page Object Model**: Clean separation of test logic and page interactions
- **Configuration Management**: JSON-based configuration for easy environment switching
- **Comprehensive Logging**: Multi-destination logging (Console, MSTest, Debug, File) with detailed test execution traces
- **File Logging**: Automatic log file generation in `Logs/` folder with timestamped entries
- **Error Handling**: Robust error handling with meaningful error messages
- **Resource Management**: Proper browser lifecycle management with setup/teardown
- **Cross-Platform**: Runs on Windows, Linux, and macOS

## Test Architecture

### TestInitialize
- Loads configuration from JSON
- Initializes Playwright browser instance
- Sets up browser context with configured options
- Creates new page for testing
- Initializes comprehensive logging system

### TestCleanup
- Closes page, context, and browser
- Disposes Playwright resources
- Finalizes log files with completion timestamps
- Ensures clean state for next test

### Helper Methods
- `ConvertToDecimal()`: Converts price strings to decimal values
- `AddProductToCartAsync()`: Handles product selection and cart addition

## Product Models Tested

- **Laptops**: HP ZBook 17 G2 Mobile Workstation
- **Mice**: HP Z8000 Bluetooth Mouse  
- **Tablets**: HP Elite x2 1011 G1 Tablet

## Logging

The project includes comprehensive multi-destination logging through the `TestLogger` class:

### Logging Destinations
- **Console Output**: Real-time monitoring during test execution
- **MSTest TestContext**: Integration with test framework reporting
- **Debug Output**: Development and IDE debugging support
- **File Logging**: Persistent log files in `Logs/` folder

### Log File Features
- **Automatic Generation**: Each test run creates a unique timestamped log file
- **File Location**: `Logs/{TestName}_{yyyyMMdd_HHmmss}.log`
- **Detailed Timestamps**: Millisecond precision for performance analysis
- **Complete Test Trace**: From initialization to cleanup with step-by-step actions
- **Thread-Safe**: Safe for parallel test execution
- **Visual Indicators**: ✅ and ❌ emojis for quick status identification

### Sample Log Output
```log
=== Test Log Started: 2025-07-30 19:52:09 ===
Test Name: AOS_AddToCart_PriceValidation
Log File: D:\...\Logs\AOS_AddToCart_PriceValidation_20250730_195209.log
============================================================
[19:52:09.582] Initializing test environment...
[19:52:12.540] Test started: AOS_AddToCart_PriceValidation
[19:52:15.857] Adding laptop product to cart...
[19:52:15.858]   → Navigating to homepage for HP ZBook 17 G2 Mobile Workstation
[19:52:32.013]   ✅ Added HP ZBook 17 G2 Mobile Workstation - Qty: 1, Price: $1799.00
[19:53:05.602] ✅ Overall cart total validation passed - Test completed successfully!
============================================================
=== Test Log Ended: 2025-07-30 19:53:05 ===
```

## Browser Support

Currently configured for Chromium, but can be easily modified to support:
- Firefox
- WebKit (Safari)

## Environment Configuration

Modify `PlaywrightSettings.json` for different environments:
- **Development**: `"Headless": false` for visual debugging
- **CI/CD**: `"Headless": true` for automated runs (current setting)
- **Performance Testing**: Adjust `"SlowMo"` value (currently 1000ms)
- **Different URLs**: Update `"BaseUrl"` for staging/production environments
- **Timeout Adjustment**: Modify `"DefaultTimeout"` based on environment performance

## File Management

### Log Files
- **Location**: `Logs/` folder in project root
- **Naming**: `{TestName}_{yyyyMMdd_HHmmss}.log`
- **Retention**: Files are preserved for analysis (excluded from version control)
- **Size**: Typically 2-5KB per test run with detailed logging

### Git Configuration
- Log files are automatically excluded via `.gitignore`
- Build artifacts (`bin/`, `obj/`) are ignored
- Test results (`TestResults/`) are excluded

## Contributing

1. Follow existing code patterns and naming conventions
2. Add comprehensive tests for new features
3. Update documentation as needed
4. Ensure all tests pass before submitting changes

## Troubleshooting

### Common Issues

1. **Browser not found**: Run `playwright install` to download browsers
2. **Timeout errors**: Increase `DefaultTimeout` in configuration
3. **Element not found**: Website may have changed - update selectors
4. **Log file access**: Ensure `Logs/` folder permissions allow write access
5. **Configuration errors**: Verify `PlaywrightSettings.json` is valid JSON

### Debug Mode

- Set `"Headless": false` and increase `"SlowMo"` value to watch test execution
- Check log files in `Logs/` folder for detailed execution traces
- Use timestamped entries to identify performance bottlenecks
- Look for ✅ and ❌ indicators to quickly identify issues

### Performance Optimization

- **Headless Mode**: Set `"Headless": true` for faster execution
- **Reduce SlowMo**: Lower `"SlowMo"` value for quicker test runs
- **Timeout Tuning**: Adjust `"DefaultTimeout"` based on environment performance
- **Log Analysis**: Use log timestamps to identify slow operations

## License

This project is for educational and testing purposes.
