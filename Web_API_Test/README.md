# Web API Test Suite

This test project contains comprehensive integration and performance tests for the Todo Items Web API. The test suite is organized into two main categories: **Functionality Tests** and **Performance Tests**.

## Project Structure

The test suite is split into specialized test classes:

- **`Web_API_Function_Test.cs`**: Comprehensive functionality tests for all API endpoints
- **`Web_API_Performance_Test.cs`**: Performance and load testing

## Test Coverage

### Functionality Tests (`Web_API_Function_Test.cs`)

All functionality tests are marked with `[Category("Functionality")]` and can be run separately. Each test includes comprehensive logging using `_testLogger?.WriteLine()` for detailed test execution tracking.

#### 1. GET /api/todoitems
- **Test Method**: `GetAllTodoItemsEndpointTest()`
- **Purpose**: Tests retrieving all todo items from the database and validates item creation/retrieval
- **Features**: 
  - Verifies current database state and item count
  - Tests item creation with detailed payload logging
  - Validates data integrity with property-by-property verification
  - Comprehensive cleanup with success confirmation
  - Enhanced error handling with FluentAssertions

#### 2. POST /api/todoitems
- **Test Method**: `AddTodoItemEndpointTest()`
- **Purpose**: Comprehensive testing of item creation with 15 distinct test scenarios
- **Enhanced Features**:
  - **Structured Test Cases**: Numbered TEST 1-15 with detailed descriptions
  - **Payload Logging**: Complete request payload details for each test
  - **Success Indicators**: Visual status indicators (✓, ⚠, ❌) for quick results
  - **Security Testing**: Enhanced SQL injection and malformed data validation
  - **Cleanup Tracking**: Detailed tracking of created items with bulk cleanup logging
- **Test Cases**:
  - TEST 1-3: ID variations (integer 0, string "0", specified IDs)
  - TEST 4-6: Missing properties (no ID, no Name, no IsComplete)
  - TEST 7: Edge case with maximum long value (Int64.MaxValue)
  - TEST 8-9: Invalid data (oversized IDs, very long names)
  - TEST 10: Empty object handling
  - TEST 11-15: Security and validation (duplicate IDs, invalid types, SQL injection, null values, content type validation)

#### 3. GET /api/todoitems/{id}
- **Test Method**: `GetTodoItemByID()`
- **Purpose**: Tests retrieving specific todo items by ID with comprehensive security validation
- **Enhanced Features**:
  - **7 Distinct Test Scenarios**: Each with detailed logging and validation
  - **Security Focus**: Explicit logging for security test scenarios
  - **Detailed Validation**: Property-by-property verification of retrieved items
- **Test Cases**:
  - TEST 1: Valid ID retrieval with complete property validation
  - TEST 2: Invalid/non-existent ID handling
  - TEST 3-7: Security tests (non-numeric IDs, multiple IDs, SQL injection, special characters, path traversal)

#### 4. PUT /api/todoitems/{id}
- **Test Method**: `UpdateTodoItemEndPointTest()`
- **Purpose**: Tests updating existing todo items with internationalization support
- **Enhanced Features**:
  - **Detailed Update Tracking**: Before/after state logging
  - **Internationalization Testing**: Unicode support validation
  - **Security Validation**: SQL injection prevention in update operations
  - **Cleanup Verification**: Success/failure logging for cleanup operations
- **Features**:
  - Standard update operations with detailed logging
  - Unicode support (Chinese characters) with validation
  - Error scenarios: wrong IDs, non-existent items
  - Security validation: SQL injection in update operations

#### 5. DELETE /api/todoitems/{id}
- **Test Method**: `DeleteTodoItemEndpointTest()`
- **Purpose**: Tests deleting todo items with comprehensive security validation
- **Enhanced Features**:
  - **Structured Security Testing**: Each security test with explicit descriptions
  - **Status Code Validation**: Detailed expected vs actual status logging
  - **Test Progression**: Clear test flow with setup, security tests, and valid deletion
- **Test Cases**:
  - Invalid ID deletion attempts with detailed error logging
  - Security testing: SQL injection, special characters, path traversal
  - Valid deletion confirmation with success verification

### Performance Tests (`Web_API_Performance_Test.cs`)

All performance tests include comprehensive initialization and cleanup logging, plus detailed performance metrics.

#### Performance Load Testing
- **Test Methods**: 
  - `GetTodoItemByIDPerformanceLoadTest()` - 10,000 requests testing item retrieval
  - `GetAllTodoItemsPerformanceLoadTest()` - 10,000 requests testing bulk retrieval
  - `AddTodoItemPerformanceLoadTest()` - 10,000 requests testing item creation (with UnsupportedMediaType validation)
  - `UpdateTodoItemPerformanceLoadTest()` - 10,000 requests testing item updates
  - `DeleteTodoItemPerformanceLoadTest()` - 1,000 create/delete cycles (optimized for actual deletion testing)

- **Enhanced Features**:
  - **Detailed Initialization Logging**: API connectivity verification with success/failure details
  - **Progress Tracking**: Regular progress updates during long-running tests (every 1000 requests)
  - **Success Rate Monitoring**: Real-time success rate calculation and logging
  - **Comprehensive Metrics**: Enhanced performance results with success rates and detailed timing
  - **Test-Specific Optimizations**: Different iteration counts based on test type (e.g., 1,000 for delete tests vs 10,000 for read tests)
  - **Cleanup Verification**: Detailed cleanup logging with success/failure confirmation

- **Performance Metrics**:
  - Total requests processed with detailed breakdown
  - Success/failure rates with percentage calculations
  - Average response time per request
  - Requests per second throughput
  - Test duration with millisecond precision
  - Automated validation of performance benchmarks (95% success rate, <100ms average response time)

## Test Design Principles

### Self-Contained Tests
Each test method is completely independent and can run in any order. Tests include comprehensive logging and cleanup verification to ensure proper test isolation.

### Modern Testing Practices
- **FluentAssertions**: Uses FluentAssertions library throughout, including in catch blocks for consistent assertion syntax
- **Test Categories**: Organized with `[Category]` attributes for selective test execution
- **Parallel Execution Control**: Uses `[DoNotParallelize]` to prevent race conditions
- **Comprehensive Logging**: Enhanced logging with `_testLogger?.WriteLine()` for:
  - Test initialization and cleanup with detailed status
  - Step-by-step test execution with payload details
  - Visual status indicators (✓, ⚠, ❌) for quick result identification
  - Error handling with detailed stack traces and context
  - Performance metrics with real-time progress updates
- **File Logging**: Automatic log file generation with:
  - Per-method timestamped log files in TestLogs directory
  - Complete test execution history with millisecond timestamps
  - Test results summary with duration and status
  - System information header (machine, user, OS, .NET version)
  - Multi-destination logging (Console, TestContext, Debug, File)
  - UTF-8 encoding support for international characters
- **Helper Methods**: Comprehensive helper methods for common operations:
  - `PostTodoItemAsync()` - HTTP POST with flexible JSON objects and optional detailed logging
  - `PostTodoItemAndValidateAsync()` - POST with automatic validation and response logging
  - `GetTodoItemByIdAsync()` - GET by ID with validation
  - `PutTodoItemAsync()` / `PutTodoItemAndValidateAsync()` - UPDATE operations
  - `PostRawContentAsync()` - Raw string posting for invalid JSON tests

### Enhanced Error Handling
- **FluentAssertions in Catch Blocks**: Consistent error handling using FluentAssertions syntax:
  ```csharp
  // For API connectivity issues (inconclusive tests)
  false.Should().BeTrue($"API not running on {apiUrl}. Error: {ex.Message}. Test marked as inconclusive.");
  
  // For unexpected errors (test failures)
  ex.Should().BeNull($"An unexpected error occurred: {ex.Message}");
  ```
- **Configuration-Aware Error Messages**: Error messages include configured API URLs
- **Detailed Error Context**: Stack traces and error details logged for debugging

### Security Testing
Comprehensive security validation including:
- SQL injection attempts in URLs and data with explicit logging
- Path traversal attacks with detailed validation
- Special character handling with status code verification
- Input validation for various data types with payload logging

### Performance Testing Enhancements
- **Multi-Test Coverage**: Separate performance tests for each CRUD operation
- **Adaptive Test Parameters**: Different iteration counts based on operation type
- **Real-Time Monitoring**: Progress updates and success rate tracking during execution
- **Comprehensive Metrics**: Enhanced performance reporting with detailed breakdowns
- **Resource Management**: Proper cleanup and resource disposal with verification

### Data Integrity
- Tests use anonymous objects for flexible JSON creation
- Supports various ID types (int, string, large numbers)
- Unicode character support (Chinese text testing)
- Edge case validation (null values, empty objects, malformed data)

### Data Models
The tests use the following data models that match the API's expected format:

```csharp
public class TodoItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}


## Setup Requirements

### Prerequisites
1. **Web API Running**: Ensure your Todo API is running on `http://localhost:5089`
2. **Clean Database**: Ensure your API database is in a clean state before running tests

### Quick Start
1. Clone or download this test project
2. Start your Todo Items Web API (default: `http://localhost:5089`)
3. Run the tests using any of the methods below

## Running the Tests

### Method 1: Visual Studio
1. Open Test Explorer (Test → Test Explorer)
2. Build the solution
3. Click "Run All Tests" or select specific tests
4. View results in Test Explorer

### Method 2: Command Line (Simple)
```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "GetAllTodoItemsEndpointTest"
```

### Method 3: With TRX Results (Preserves test reports)
```bash
# Using PowerShell script (recommended)
.\RunTestsWithTRX.ps1

# Using batch file
.\RunTestsWithTRX.bat

# Manual command
dotnet test --logger "trx" --results-directory TestResults
```

### Viewing Test Logs

Tests automatically generate detailed log files for each test method:
- **Location**: `TestLogs/` directory
- **Naming**: `Web_API_Function_Test_{TestMethodName}_{YYYYMMDD_HHMMSS}.log`
- **Content**: Complete execution history, test steps, results, and system information

#### Quick Log Access
```bash
# View latest log file
.\ViewLatestLog.ps1

# Open latest log in Notepad
.\ViewLatestLog.ps1 -OpenInNotepad
```
3. Run all tests or individual test methods

### Test Results
- **Console Output**: Real-time test progress and results
- **TRX Files**: XML test reports in `TestResults/` directory (when using TRX logging)
- **Log Files**: Detailed per-test logs in `TestLogs/` directory

### Advanced Options
```bash
# Run with detailed console output
dotnet test --logger "console;verbosity=detailed"

# Run specific test categories (when applicable)
dotnet test --filter "TestCategory=Functionality"

# List all available tests without running
dotnet test --list-tests
```

## Configuration

### API Configuration
The tests use configuration files to set the API endpoint:

#### appsettings.json (Main configuration)
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5089"
  },
  "TestSettings": {
    "EnableFileLogging": true,
    "LogDirectory": "TestLogs"
  }
}
```

#### Changing the API URL
To test against a different API endpoint:
1. **Edit appsettings.json**: Change the `BaseUrl` value
2. **Environment Variable**: Set `ApiSettings__BaseUrl=http://your-api-url`
3. **Command Line**: `$env:ApiSettings__BaseUrl="http://localhost:8080"; dotnet test`

## Dependencies

The test project uses:
- **MSTest**: Microsoft's testing framework (.NET 9.0)
- **Microsoft.AspNetCore.Mvc.Testing**: For integration testing ASP.NET Core applications
- **Microsoft.Extensions.Configuration**: For configuration file support
- **Newtonsoft.Json**: For JSON serialization/deserialization
- **FluentAssertions**: Modern assertion library for more readable tests
- **HttpClient**: For making HTTP requests to the API

## Notes

1. **Simple Setup**: Just start your API on `http://localhost:5089` and run `dotnet test`
2. **Automatic Logging**: Each test method generates its own detailed log file
3. **Test Independence**: Each test is self-contained and can run in any order
4. **TRX Support**: Use the provided scripts to generate persistent test reports
5. **Visual Studio**: Full integration with Test Explorer for easy test execution

## Quick Troubleshooting

- **Tests Fail**: Ensure your Web API is running on the correct port (default: 5089)
- **No Log Files**: Check that `TestLogs` directory has write permissions
- **TRX Missing**: Use `.\RunTestsWithTRX.ps1` instead of plain `dotnet test`
- **Wrong API URL**: Edit `appsettings.json` or set `ApiSettings__BaseUrl` environment variable

## Customization

To adapt these tests for different APIs:
1. **Update API URL**: Modify `appsettings.json` `BaseUrl` setting
2. **Modify Data Models**: Update the TodoItem structure to match your API
3. **Adjust Endpoints**: Change the API endpoint paths in the test methods
4. **Update Test Data**: Modify test data to match your domain requirements
