# AOS UI Automation

A comprehensive UI automation test suite for the Advantage Online Shopping (AOS) website using Microsoft Playwright and MSTest framework.

## Overview

This project automates the testing of shopping cart functionality on the Advantage Online Shopping website, including:
- Data-driven product testing using CSV configuration
- Product addition to cart from configurable test data
- Multiple products and quantities support with flexible CSV configuration
- Color-specific product differentiation and validation
- Smart aggregation of shopping cart items by model and color combinations
- Price validation and calculations for individual and aggregated items
- Quantity verification across multiple CSV entries
- Cart total validation with comprehensive aggregation logic
- Dynamic test scenarios based on CSV product definitions

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
├── shopping_items.csv            # Test data file with product configurations
├── AOS_UI_Automation.csproj      # Project file with dependencies
├── AOS_UI_Automation.sln         # Solution file
├── MSTestSettings.cs             # MSTest configuration
├── Logs/                         # Test execution log files (auto-generated)
├── .gitignore                    # Git ignore rules for build artifacts and logs
└── README.md                     # This file
```

## Configuration

The project uses multiple configuration files for different aspects:

### Browser Configuration (`PlaywrightSettings.json`)

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

### Test Data Configuration (`shopping_items.csv`)

The test data is configured using a CSV file that defines the products to be tested. The system supports multiple entries for the same product with different colors or separate purchase intentions:

```csv
Category, Model, Quantity, Color
LaptopsCategory, HP ZBook 17 G2 Mobile Workstation, 1, GRAY
MiceCategory, HP Z8000 Bluetooth Mouse, 2, BLACK
TabletsCategory, HP Elite x2 1011 G1 Tablet, 1, BLACK
LaptopsCategory, HP ZBook 17 G2 Mobile Workstation, 2, BLACK
```

#### CSV Format & Features
- **Category**: Product category (LaptopsCategory, MiceCategory, TabletsCategory)
- **Model**: Exact product name as displayed on the website
- **Quantity**: Number of items to add to cart for this specific entry
- **Color**: Product color to select (GRAY, BLACK, etc.)
- **Multiple Entries**: Same product can appear multiple times with different colors or quantities
- **Smart Aggregation**: System automatically combines entries with matching model+color combinations

### Configuration Parameters

**Browser Settings (`PlaywrightSettings.json`)**:
- `BaseUrl`: Target website URL
- `DefaultTimeout`: Page operation timeout (milliseconds)
- `BrowserName`: Browser to use for testing
- `LaunchOptions.Headless`: Run browser in headless mode (true/false)
- `LaunchOptions.Args`: Browser command line arguments
- `LaunchOptions.SlowMo`: Slow motion delay for debugging (milliseconds)

**Test Data Settings (`shopping_items.csv`)**:
- Easy modification of test products without code changes
- Support for any number of products and quantities in the test
- Multiple entries for the same product with different colors
- Intelligent aggregation of duplicate model+color combinations
- Configurable quantities and colors per product entry
- Flexible product category selection
- Color-specific cart validation and price verification

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

This test validates the complete shopping cart workflow using advanced data-driven testing with intelligent aggregation:

1. **CSV Data Loading**: Dynamically reads product configurations from `shopping_items.csv`:
   - Supports multiple entries for the same product with different colors
   - Example: HP ZBook 17 G2 Mobile Workstation appears 2 times (1 GRAY + 2 BLACK)
   - Each CSV row represents an individual add-to-cart action

2. **Dynamic Product Addition**: Processes each CSV entry individually:
   - Iterates through all CSV rows sequentially
   - Adds products to cart with specified quantities and colors for each entry
   - Handles multiple color variants of the same product

3. **Smart Cart Validation**: Performs intelligent aggregation during validation:
   - **Color Extraction**: Reads color information from each cart row
   - **Model+Color Matching**: Groups CSV entries by exact model and color combination
   - **Quantity Aggregation**: Sums quantities for matching model+color pairs
   - **Price Aggregation**: Calculates total expected price for aggregated quantities

4. **Advanced Validation Logic**: Confirms:
   - Correct number of unique model+color combinations in cart
   - Accurate aggregated quantities (e.g., 2 GRAY laptops from 2 CSV entries)
   - Proper price calculations for aggregated totals
   - Individual cart row validation against aggregated CSV data
   - Overall cart total accuracy across all products

5. **Flexible Multi-Product Testing**: 
   - Test automatically adapts to any number of CSV entries
   - Handles complex scenarios with multiple colors and quantities
   - Supports realistic shopping patterns with repeated purchases

#### Example Aggregation Scenario:
**CSV Data:**
```
HP ZBook 17 G2 Mobile Workstation, 1, GRAY
HP ZBook 17 G2 Mobile Workstation, 2, BLACK  
```

**Expected Cart Results:**
- Cart Row 1: HP ZBook (GRAY) - Quantity: 1, Total: Price × 1
- Cart Row 2: HP ZBook (BLACK) - Quantity: 2, Total: Price × 2

## Key Features

- **Advanced Data-Driven Testing**: CSV-based test data with support for multiple product entries and color variations
- **Intelligent Aggregation**: Smart grouping of cart items by model+color combinations with automatic quantity and price summation
- **Multi-Color Support**: Handles different color variants of the same product as separate cart entries
- **Flexible Product Configuration**: Support for any number of product entries with complex quantity and color combinations
- **Page Object Model**: Clean separation of test logic and page interactions
- **Configuration Management**: JSON-based configuration for easy environment switching
- **Comprehensive Logging**: Multi-destination logging (Console, MSTest, Debug, File) with detailed test execution traces
- **File Logging**: Automatic log file generation in `Logs/` folder with timestamped entries
- **Dynamic Test Scenarios**: Tests automatically adapt to CSV data changes and complex product combinations
- **Color-Specific Validation**: Extracts and validates color information from shopping cart UI elements
- **Error Handling**: Robust error handling with meaningful error messages for aggregation and validation
- **Resource Management**: Proper browser lifecycle management with setup/teardown
- **Cross-Platform**: Runs on Windows, Linux, and macOS
- **Maintainable Test Data**: Easy product modification through CSV without code changes

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
- `ReadShoppingItemsFromCsv()`: Loads and parses product data from CSV file
- `GetProjectRootDirectory()`: Locates project root for consistent file paths
- `ConvertToDecimal()`: Converts price strings to decimal values
- `AddProductToCartAsync()`: Handles product selection and cart addition

## Product Models Tested

The test products are configurable via `shopping_items.csv`. Current test data includes:

- **Laptops**: HP ZBook 17 G2 Mobile Workstation (GRAY)
- **Mice**: HP Z8000 Bluetooth Mouse (BLACK)
- **Tablets**: HP Elite x2 1011 G1 Tablet (BLACK)

To modify test products, simply update the CSV file with new product information. The test will automatically adapt to the new configuration.

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
=== Test Log Started: 2025-08-04 19:52:09 ===
Test Name: AOS_AddToCart_PriceValidation
Log File: D:\...\Logs\AOS_AddToCart_PriceValidation_20250804_195209.log
============================================================
[19:52:09.582] Initializing test environment...
[19:52:10.134] Reading shopping items from: D:\...\shopping_items.csv
[19:52:10.143]   ✅ Loaded: LaptopsCategory - HP ZBook 17 G2 Mobile Workstation (Qty: 1)
[19:52:10.144]   ✅ Loaded: MiceCategory - HP Z8000 Bluetooth Mouse (Qty: 2)
[19:52:10.144]   ✅ Loaded: TabletsCategory - HP Elite x2 1011 G1 Tablet (Qty: 1)
[19:52:10.144]   ✅ Loaded: LaptopsCategory - HP ZBook 17 G2 Mobile Workstation (Qty: 2)
[19:52:10.145] Successfully loaded 4 products from CSV
[19:52:12.540] Test started: AOS_AddToCart_PriceValidation

--- Validating Product 1: HP ZBook 17 G2 Mobile Workstation ---
[19:52:45.123] Cart color: GRAY
[19:52:45.124] Cart quantity: 1
[19:52:45.125] Expected quantity: 1 (from 1 CSV entries with model='HP ZBook 17 G2 Mobile Workstation' and color='GRAY'), Cart quantity: 1
[19:52:45.126] ✅ LaptopsCategory validation passed for HP ZBook 17 G2 Mobile Workstation (GRAY) - aggregated from 1 CSV entries

--- Validating Product 2: HP ZBook 17 G2 Mobile Workstation ---
[19:52:45.127] Cart color: BLACK
[19:52:45.128] Cart quantity: 2
[19:52:45.129] Expected quantity: 2 (from 1 CSV entries with model='HP ZBook 17 G2 Mobile Workstation' and color='BLACK'), Cart quantity: 2
[19:52:45.130] ✅ LaptopsCategory validation passed for HP ZBook 17 G2 Mobile Workstation (BLACK) - aggregated from 1 CSV entries

[19:53:05.602] ✅ Overall cart total validation passed - Test completed successfully!
============================================================
=== Test Log Ended: 2025-08-04 19:53:05 ===
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

### Test Data Files
- **Location**: `shopping_items.csv` in project root
- **Format**: CSV with headers: Category, Model, Quantity, Color
- **Validation**: Automatic validation of CSV format and data integrity
- **Flexibility**: Supports any number of products for testing

### Log Files
- **Location**: `Logs/` folder in project root
- **Naming**: `{TestName}_{yyyyMMdd_HHmmss}.log`
- **Retention**: Files are preserved for analysis (excluded from version control)
- **Size**: Typically 2-5KB per test run with detailed logging

### Git Configuration
- Log files are automatically excluded via `.gitignore`
- Build artifacts (`bin/`, `obj/`) are ignored
- Test results (`TestResults/`) are excluded
- CSV test data files are included in version control for consistency

## Data-Driven Testing

### Advanced CSV Configuration Benefits
- **No Code Changes**: Modify test scenarios by editing CSV file only
- **Multi-Product Support**: Add multiple entries for the same product with different configurations
- **Color Differentiation**: Test different color variants as separate shopping cart items
- **Intelligent Aggregation**: Automatic grouping and validation of products by model+color combinations
- **Easy Maintenance**: Product updates don't require code compilation
- **Scalable Testing**: Add/remove products without test logic changes
- **Complex Scenarios**: Support realistic shopping patterns with repeated purchases
- **Team Collaboration**: Non-technical team members can update test data
- **Version Control**: Test data changes are tracked with clear history

### Adding New Products & Scenarios
1. Edit `shopping_items.csv`
2. Add new rows with: Category, Model, Quantity, Color
3. **Multiple Entries**: Same product can appear multiple times for:
   - Different color variants
   - Separate purchase intentions (e.g., buying 1 + buying 2 more later)
   - Complex quantity testing scenarios
4. Ensure products and colors exist on the target website
5. Run tests - they automatically adapt to new configuration with proper aggregation

### Advanced CSV Examples

**Simple Configuration:**
```csv
Category, Model, Quantity, Color
LaptopsCategory, HP ZBook 17 G2 Mobile Workstation, 1, GRAY
```

**Multi-Color Configuration:**
```csv
Category, Model, Quantity, Color
LaptopsCategory, HP ZBook 17 G2 Mobile Workstation, 1, GRAY
LaptopsCategory, HP ZBook 17 G2 Mobile Workstation, 2, BLACK
```

**Complex Aggregation Scenario:**
```csv
Category, Model, Quantity, Color
LaptopsCategory, HP ZBook 17 G2 Mobile Workstation, 1, GRAY
LaptopsCategory, HP ZBook 17 G2 Mobile Workstation, 2, BLACK
MiceCategory, HP Z8000 Bluetooth Mouse, 1, BLACK
MiceCategory, HP Z8000 Bluetooth Mouse, 1, BLACK
```
*Results in: 1 GRAY laptop, 2 BLACK laptops, 2 BLACK mice*

### Supported Categories
- **LaptopsCategory**: For laptop products
- **MiceCategory**: For mouse products  
- **TabletsCategory**: For tablet products
- Additional categories can be added by updating the ProductCategory enum

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
6. **CSV format errors**: Ensure CSV has proper headers and valid data format
7. **Product not found**: Verify product names in CSV match website exactly
8. **Invalid category**: Check that CSV categories match supported ProductCategory enum values
9. **Color mismatch**: Ensure CSV colors match exactly with website color options (case-sensitive)
10. **Aggregation issues**: Check logs for model+color combination validation details
11. **Multiple color variants**: Verify that different colors appear as separate cart rows

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
