# Configuration Setup

This project now supports configuration files for managing API settings and test parameters.

## Configuration Files

### appsettings.json (Default Configuration)
Contains the base configuration settings:
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

### appsettings.test.json (Test Environment)
Overrides for test-specific settings:
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

### appsettings.staging.json (Staging Environment)
Example configuration for staging environment:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.staging.example.com",
    "Timeout": 60000,
    "RetryAttempts": 5
  },
  "TestSettings": {
    "CleanupDelayMs": 2000,
    "EnableDetailedLogging": true
  }
}
```

## Environment Variables

You can also override settings using environment variables:
```bash
# Override the API base URL
set ApiSettings__BaseUrl=http://localhost:8080

# Override cleanup delay
set TestSettings__CleanupDelayMs=2000
```

## Configuration Priority (Highest to Lowest)

1. Environment Variables
2. appsettings.{environment}.json
3. appsettings.test.json
4. appsettings.json
5. Hardcoded fallback values

## Usage in Different Environments

### Local Development
The tests will automatically use `appsettings.json` and `appsettings.test.json`.

### CI/CD Pipeline
Set environment variables to override configuration:
```yaml
env:
  ApiSettings__BaseUrl: "http://test-api:5000"
  TestSettings__CleanupDelayMs: "100"
```

### Docker Testing
```bash
docker run -e "ApiSettings__BaseUrl=http://api-container:5000" your-test-image
```

## Benefits

- ✅ No more hardcoded URLs
- ✅ Environment-specific configurations
- ✅ Easy CI/CD integration
- ✅ Configurable test parameters
- ✅ Environment variable support
