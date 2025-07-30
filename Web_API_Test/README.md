# Web API Test Suite

This test project contains comprehensive integration and performance tests for the Todo Items Web API. The test suite is organized into two main categories: **Functionality Tests** and **Performance Tests**.

## Project Structure

The test suite is split into specialized test classes:

- **`Web_API_Function_Test.cs`**: Comprehensive functionality tests for all API endpoints
- **`Web_API_Performance_Test.cs`**: Performance and load testing

## Test Coverage

### Functionality Tests (`Web_API_Function_Test.cs`)

All functionality tests are marked with `[Category("Functionality")]` and can be run separately.

#### 1. GET /api/todoitems
- **Test Method**: `GetAllItemsEndpointTest()`
- **Purpose**: Tests retrieving all todo items from an empty database and after adding items
- **Features**: 
  - Verifies empty database returns empty array
  - Tests item creation and retrieval
  - Validates data integrity after operations

#### 2. POST /api/todoitems
- **Test Method**: `AddItemEndpointTest()`
- **Purpose**: Comprehensive testing of item creation with various data scenarios
- **Test Cases**:
  - Creating items with ID as integer 0
  - Creating items with ID as string "0" 
  - Creating items with specified IDs
  - Edge cases: items without ID, without name, without completion status
  - Large ID values (Int64.MaxValue)
  - Invalid scenarios: oversized IDs, existing IDs, malformed data
  - Security testing: SQL injection attempts
  - Data validation: null values, empty objects, invalid JSON

#### 3. GET /api/todoitems/{id}
- **Test Method**: `GetItemByID()`
- **Purpose**: Tests retrieving specific todo items by ID with security validation
- **Test Cases**:
  - Valid ID retrieval
  - Invalid/non-existent ID (404 responses)
  - Security tests: SQL injection attempts, special characters, path traversal
  - Input validation: word strings as IDs

#### 4. PUT /api/todoitems/{id}
- **Test Method**: `UpdateItemEndPointTest()`
- **Purpose**: Tests updating existing todo items
- **Features**:
  - Standard update operations
  - Unicode support (Chinese characters)
  - Error scenarios: wrong IDs, non-existent items
  - Security validation: SQL injection in update operations

#### 5. DELETE /api/todoitems/{id}
- **Test Method**: `DeleteTodoItemEndpointTest()`
- **Purpose**: Tests deleting todo items with comprehensive validation
- **Test Cases**:
  - Invalid ID deletion attempts
  - Security testing: SQL injection, special characters, path traversal
  - Valid deletion confirmation

### Performance Tests (`Web_API_Performance_Test.cs`)

#### Performance Load Testing
- **Test Method**: `PerformanceLoadTest()`
- **Purpose**: Load testing with 10,000 consecutive API requests
- **Features**:
  - Tests server handling of high-volume requests
  - Measures response times and throughput
  - Validates server stability under load
  - Provides detailed performance metrics:
    - Total requests processed
    - Success/failure rates
    - Average response time per request
    - Requests per second
  - Automated validation of performance benchmarks (95% success rate, <100ms average response time)

## Test Design Principles

### Self-Contained Tests
Each test method is completely independent and can run in any order. Tests use the `ClearAllTodoItemsBeforeTest()` method to ensure a clean state before execution.

### Modern Testing Practices
- **FluentAssertions**: Uses FluentAssertions library for more readable and expressive test assertions
- **Test Categories**: Organized with `[Category]` attributes for selective test execution
- **Parallel Execution Control**: Uses `[DoNotParallelize]` to prevent race conditions
- **Helper Methods**: Comprehensive helper methods for common operations:
  - `PostTodoItemAsync()` - HTTP POST with flexible JSON objects
  - `PostTodoItemAndValidateAsync()` - POST with automatic validation
  - `GetTodoItemByIdAsync()` - GET by ID with validation
  - `PutTodoItemAsync()` / `PutTodoItemAndValidateAsync()` - UPDATE operations
  - `PostRawContentAsync()` - Raw string posting for invalid JSON tests

### Security Testing
Comprehensive security validation including:
- SQL injection attempts in URLs and data
- Path traversal attacks
- Special character handling
- Input validation for various data types

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

### Configuration-Based Setup
The test suite now uses **configuration files** for flexible environment management:

- **Configuration Files**: Uses `appsettings.json`, `appsettings.test.json`, and environment-specific configs
- **Base URL**: Configurable via `ApiSettings:BaseUrl` (defaults to `http://localhost:5089`)
- **Environment Variables**: Supports override via environment variables
- **Test Parameters**: Configurable cleanup delays and logging settings
- **API Validation**: Tests verify the API is running during initialization
- **Test Isolation**: Each test clears all existing data before execution

### Configuration Files

#### appsettings.json (Default Configuration)
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5089",
    "Timeout": 30000,
    "RetryAttempts": 3
  },
  "TestSettings": {
    "CleanupDelayMs": 1000,
    "EnableDetailedLogging": false
  }
}
```

#### appsettings.test.json (Test Environment)
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5089"
  },
  "TestSettings": {
    "CleanupDelayMs": 500,
    "EnableDetailedLogging": true
  }
}
```

### Environment Variable Support
Override configuration using environment variables:
```bash
# Windows
set ApiSettings__BaseUrl=http://localhost:5089
set TestSettings__CleanupDelayMs=2000

# Linux/macOS
export ApiSettings__BaseUrl=http://localhost:5089
export TestSettings__CleanupDelayMs=2000
```

### For Integration Testing with Real API
To use these tests with an actual Web API project:

1. **Add Project Reference**: Add a reference to your Web API project in the test project
2. **Configure WebApplicationFactory**: Replace the basic HttpClient setup with proper WebApplicationFactory configuration
3. **Database Setup**: Configure in-memory database or test database for isolated testing
4. **Update Configuration**: Modify `appsettings.json` or use environment variables to set the correct API URL

Example WebApplicationFactory setup:
```csharp
_factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TodoContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add InMemory database for testing
            services.AddDbContext<TodoContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        });
    });
_client = _factory.CreateClient();
```

### For Manual API Testing
1. **Start Your Web API**: Ensure your Todo API is running (e.g., on `http://localhost:5089`)
2. **Update Configuration**: 
   - Modify `appsettings.json` to set the correct `BaseUrl`
   - Or set environment variable: `ApiSettings__BaseUrl=http://your-api-url`
3. **Database State**: Ensure your API database is in a clean state before running tests

## Running the Tests

### Configuration Priority
The configuration system loads settings in this order (highest to lowest priority):
1. **Environment Variables** (e.g., `ApiSettings__BaseUrl`)
2. **appsettings.{environment}.json** (e.g., `appsettings.staging.json`)
3. **appsettings.test.json**
4. **appsettings.json**
5. **Hardcoded fallback values**

### Using Visual Studio
1. Open Test Explorer (Test → Test Explorer)
2. Build the solution
3. Run all tests or individual test methods
4. **Group by Category**: Group tests by category to run Functionality or Performance tests separately

### Using .NET CLI

```bash
# Run all tests
dotnet test

# Run only functionality tests
dotnet test --filter "Category=Functionality"

# Run only performance tests  
dotnet test --filter "Name=PerformanceLoadTest"

# Run with custom API URL
ApiSettings__BaseUrl=http://localhost:5089 dotnet test

# Run specific test by name
dotnet test --filter "Name=GetAllItemsEndpointTest"

# Run specific test class
dotnet test --filter "ClassName=Web_API_Performance_Test"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# List all available tests
dotnet test --list-tests
```

### CI/CD Integration
For continuous integration, override configuration using environment variables:

```yaml
# GitHub Actions example
env:
  ApiSettings__BaseUrl: "http://test-api:5089"
  TestSettings__CleanupDelayMs: "100"
  TestSettings__EnableDetailedLogging: "true"
```

```bash
# Docker example
docker run -e "ApiSettings__BaseUrl=http://api-container:5089" \
           -e "TestSettings__CleanupDelayMs=100" \
           your-test-image
```

## Dependencies

The test project uses:
- **MSTest**: Microsoft's testing framework (.NET 9.0)
- **Microsoft.AspNetCore.Mvc.Testing**: For integration testing ASP.NET Core applications
- **Microsoft.Extensions.Configuration**: For configuration file support
- **Newtonsoft.Json**: For JSON serialization/deserialization
- **FluentAssertions**: Modern assertion library for more readable tests
- **HttpClient**: For making HTTP requests to the API

### Package Versions
```xml
<PackageReference Include="FluentAssertions" Version="8.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.7" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
<PackageReference Include="MSTest" Version="3.6.4" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## Notes

1. **Test Organization**: Tests are split into two specialized classes:
   - `Web_API_Function_Test`: Comprehensive functionality testing
   - `Web_API_Performance_Test`: Load and performance testing

2. **Modern Testing Practices**: 
   - Uses FluentAssertions for readable assertions
   - Test categories for selective execution
   - Helper methods for code reuse and maintainability
   - Security testing integrated into functional tests
   - Configuration-based setup for multiple environments
   - Multi-destination logging (Console, TestContext, Debug)

3. **Test Isolation**: 
   - `ClearAllTodoItemsBeforeTest()` ensures clean state
   - `[DoNotParallelize]` prevents test interference
   - Each test is completely self-contained

4. **Error Handling**: Tests verify both success and error scenarios:
   - HTTP status code validation
   - Security vulnerability testing (SQL injection, path traversal)
   - Input validation and edge cases

5. **Performance Testing**: 
   - Load testing with 10,000 requests
   - Automatic performance metric collection
   - Response time and throughput validation

6. **Data Integrity**: 
   - Tests verify returned data matches expected formats
   - Unicode support validation
   - Edge case handling (null values, empty objects)

7. **HTTP Request Logging**: 
   - Comprehensive request/response logging available via TestLogger
   - Custom headers support (Host, Connection, etc.)
   - Detailed debugging information
   - Configurable logging levels via `TestSettings:EnableDetailedLogging`

8. **Configuration Management**:
   - JSON-based configuration files for different environments
   - Environment variable support for CI/CD integration
   - Hierarchical configuration with override capabilities
   - No hardcoded URLs or test parameters

## Customization

To adapt these tests for different APIs:
1. **Update Configuration**: Modify `appsettings.json` or set environment variables for:
   - API base URL (`ApiSettings:BaseUrl`)
   - Request timeout (`ApiSettings:Timeout`)
   - Retry attempts (`ApiSettings:RetryAttempts`)
   - Cleanup delay (`TestSettings:CleanupDelayMs`)
2. **Modify Data Models**: Update the data models to match your API's structure
3. **Adjust Expected Status Codes**: Update expected HTTP status codes if different
4. **Add Authentication**: Add authentication headers if your API requires authentication
5. **Update Test Data**: Modify test data to match your domain requirements
6. **Customize Helper Methods**: Adjust helper methods for your API's specific needs
7. **Performance Tuning**: Adjust performance test parameters (iteration count, response time thresholds)

### Environment-Specific Configuration Examples

```json
// appsettings.development.json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5089",
    "Timeout": 30000
  },
  "TestSettings": {
    "CleanupDelayMs": 1000,
    "EnableDetailedLogging": true
  }
}

// appsettings.staging.json  
{
  "ApiSettings": {
    "BaseUrl": "https://api.staging.example.com",
    "Timeout": 60000,
    "RetryAttempts": 5
  },
  "TestSettings": {
    "CleanupDelayMs": 2000,
    "EnableDetailedLogging": false
  }
}

// appsettings.production.json
{
  "ApiSettings": {
    "BaseUrl": "https://api.production.example.com",
    "Timeout": 45000,
    "RetryAttempts": 3
  },
  "TestSettings": {
    "CleanupDelayMs": 1500,
    "EnableDetailedLogging": false
  }
}
```
