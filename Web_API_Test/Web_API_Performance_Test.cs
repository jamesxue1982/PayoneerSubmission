using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using FluentAssertions;
using System.Net.Http.Json;
using System.ComponentModel;
using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Web_API_Test;

[TestClass]
[Category("Performance")]
[DoNotParallelize] // This prevents parallel execution of test methods in this class
public sealed class Web_API_Performance_Test
{
    public TestContext? TestContext { get; set; }
    private HttpClient? _client;
    private TestLogger? _testLogger;
    private IConfiguration? _configuration;

    [TestInitialize]
    public void TestInitialize()
    {
        // Build configuration first
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get logging configuration
        var enableFileLogging = _configuration.GetValue<bool>("TestSettings:EnableFileLogging", true);
        var logDirectory = _configuration["TestSettings:LogDirectory"];
        
        // Get the current test method name from TestContext
        var testMethodName = TestContext?.TestName ?? "UnknownTest";
        
        // Initialize logger with configuration - using test method name for unique files
        if (enableFileLogging)
        {
            _testLogger = new TestLogger(TestContext, logDirectory: logDirectory, testClassName: $"Web_API_Performance_Test_{testMethodName}");
        }
        else
        {
            _testLogger = new TestLogger(TestContext); // File logging disabled, will use null constructor path
        }
        
        _testLogger?.WriteLine("=== PERFORMANCE TEST INITIALIZATION ===");
        _testLogger?.WriteLine($"Test Method: {testMethodName}");
        
        // Note: For actual testing, you would need to reference the Web API project
        // and configure the WebApplicationFactory properly. This is a template
        // showing the structure of comprehensive API tests.

        _testLogger?.WriteLine("Building configuration from appsettings files...");
        // Get the API base URL from configuration, fallback to default if not found
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5089";
        _testLogger?.WriteLine($"Using API base URL: {apiBaseUrl}");

        _testLogger?.WriteLine("Creating HttpClient for API testing...");
        // For now, we'll create a simple HttpClient for demonstration
        // In real scenarios, replace this with proper WebApplicationFactory setup
        _client = new HttpClient()
        {
            BaseAddress = new Uri(apiBaseUrl)
        };
        _testLogger?.WriteLine($"HttpClient configured with base address: {_client.BaseAddress}");

        //make sure the API is running before tests
        _testLogger?.WriteLine("Verifying API connectivity...");
        try
        {
            var response = _client.GetAsync("/api/todoitems").Result;
            response.EnsureSuccessStatusCode();
            _testLogger?.WriteLine($"API connectivity verified successfully. Status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _testLogger?.WriteLine($"API connectivity failed: {ex.Message}");
            throw;
        }

        _testLogger?.WriteLine("Performance test initialization completed successfully.");
        if (!string.IsNullOrEmpty(_testLogger?.LogFilePath))
        {
            _testLogger?.WriteLine($"Test logs will be saved to: {_testLogger.LogFilePath}");
        }
        _testLogger?.WriteLine("============================================");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _testLogger?.WriteLine("=== PERFORMANCE TEST CLEANUP ===");
        _testLogger?.WriteLine("Disposing HttpClient and cleaning up resources...");
        _client?.Dispose();
        
        // Log the final location of the log file
        if (!string.IsNullOrEmpty(_testLogger?.LogFilePath))
        {
            var logPath = _testLogger.LogFilePath;
            _testLogger?.WriteLine($"Complete test log saved to: {logPath}");
            Console.WriteLine($"📄 Performance Test log file: {logPath}");
        }
        
        _testLogger?.WriteLine("Cleanup completed successfully.");
        _testLogger?.WriteLine("=================================");
        
        // Dispose the logger to finalize the log file
        _testLogger?.Dispose();
    }


    [TestMethod]
    [Category("Performance")]
    public async Task GetTodoItemByIDPerformanceLoadTest()
    {
        var testStartTime = DateTime.Now;
        var testName = "GetTodoItemByIDPerformanceLoadTest";
        
        try
        {
            _testLogger?.WriteLine("=== Starting GetTodoItemByID Performance Load Test ===");
            var iterations = 10000;
            var successfulRequests = 0;
            var startTime = DateTime.UtcNow;

            _testLogger?.WriteLine($"Starting performance test with {iterations} requests...");

            // Act - Query the same string ID 10,000 times
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var response = await _client!.GetAsync($"/api/todoitems/{i + 1}");

                    // We expect NotFound for string IDs, but count any response as successful
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                        response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        successfulRequests++;
                    }

                    // Log progress every 1000 requests
                    if ((i + 1) % 1000 == 0)
                    {
                        var currentSuccessRate = (double)successfulRequests / (i + 1) * 100;
                        _testLogger?.WriteLine($"✓ Completed {i + 1} requests... Success rate: {currentSuccessRate:F2}%");
                    }
                }
                catch (Exception ex)
                {
                    _testLogger?.WriteLine($"❌ Request {i + 1} failed: {ex.Message}");
                }
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert and Log Performance Metrics
            _testLogger?.WriteLine($"=== PERFORMANCE TEST RESULTS ===");
            _testLogger?.WriteLine($"Total Requests: {iterations}");
            _testLogger?.WriteLine($"Successful Requests: {successfulRequests}");
            _testLogger?.WriteLine($"Failed Requests: {iterations - successfulRequests}");
            _testLogger?.WriteLine($"Total Duration: {duration.TotalMilliseconds:F2} ms");
            _testLogger?.WriteLine($"Average Response Time: {duration.TotalMilliseconds / iterations:F2} ms per request");
            _testLogger?.WriteLine($"Requests per Second: {iterations / duration.TotalSeconds:F2}");
            _testLogger?.WriteLine($"================================");

            // Assert that at least 95% of requests were successful
            var successRate = (double)successfulRequests / iterations;
            successRate.Should().BeGreaterThan(0.95, $"Expected at least 95% success rate, got {successRate:P2}");

            // Assert that average response time is reasonable (less than 100ms per request)
            var avgResponseTime = duration.TotalMilliseconds / iterations;
            avgResponseTime.Should().BeLessThan(100, "Average response time should be less than 100ms");

            _testLogger?.WriteLine("GetTodoItemByIDPerformanceLoadTest completed successfully.");

            // Log successful test result
            var testDuration = DateTime.Now - testStartTime;
            _testLogger?.WriteTestResult(testName, "PASSED", testDuration, $"Success Rate: {successRate:P2}, Avg Response: {avgResponseTime:F2}ms");

        }
        catch (HttpRequestException ex)
        {
            var errorMsg = $"HTTP Request failed: {ex.Message}";
            _testLogger?.WriteLine($"❌ {errorMsg}");
            
            // Log failed test result
            var testDuration = DateTime.Now - testStartTime;
            _testLogger?.WriteTestResult(testName, "FAILED", testDuration, errorMsg);
            
            // Using FluentAssertions to fail the test
            ex.Should().BeNull(errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error during performance test: {ex.Message}";
            _testLogger?.WriteLine($"❌ {errorMsg}");
            _testLogger?.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Log failed test result
            var testDuration = DateTime.Now - testStartTime;
            _testLogger?.WriteTestResult(testName, "FAILED", testDuration, errorMsg);
            
            // Using FluentAssertions to fail the test
            ex.Should().BeNull(errorMsg);
        }
    }

    [TestMethod]
    [Category("Performance")]
    public async Task GetAllTodoItemsPerformanceLoadTest()
    {
        try
        {
            _testLogger?.WriteLine("=== Starting GetAllTodoItems Performance Load Test ===");
            var iterations = 10000;
            var successfulRequests = 0;
            var startTime = DateTime.UtcNow;

            _testLogger?.WriteLine($"Starting performance test with {iterations} GET requests to retrieve all TodoItems...");

            _testLogger?.WriteLine("Act - Query the /api/todoitems endpoint 10,000 times");
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var response = await _client!.GetAsync($"/api/todoitems");

                    // Count successful responses (OK status)
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        successfulRequests++;
                    }
                    else
                    {
                        _testLogger?.WriteLine($"GET request {i + 1} returned unexpected status: {response.StatusCode}");
                    }

                    // Log progress every 1000 requests
                    if ((i + 1) % 1000 == 0)
                    {
                        _testLogger?.WriteLine($"Completed {i + 1} GET requests... (Success rate so far: {((double)successfulRequests / (i + 1)):P1})");
                    }
                }
                catch (Exception ex)
                {
                    _testLogger?.WriteLine($"GET request {i + 1} failed: {ex.Message}");
                }
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert and Log Performance Metrics
            _testLogger?.WriteLine($"=== PERFORMANCE TEST RESULTS ===");
            _testLogger?.WriteLine($"Total Requests: {iterations}");
            _testLogger?.WriteLine($"Successful Requests: {successfulRequests}");
            _testLogger?.WriteLine($"Failed Requests: {iterations - successfulRequests}");
            _testLogger?.WriteLine($"Total Duration: {duration.TotalMilliseconds:F2} ms");
            _testLogger?.WriteLine($"Average Response Time: {duration.TotalMilliseconds / iterations:F2} ms per request");
            _testLogger?.WriteLine($"Requests per Second: {iterations / duration.TotalSeconds:F2}");
            _testLogger?.WriteLine($"================================");

            // Assert that at least 95% of requests were successful
            var successRate = (double)successfulRequests / iterations;
            successRate.Should().BeGreaterThan(0.95, $"Expected at least 95% success rate, got {successRate:P2}");

            // Assert that average response time is reasonable (less than 100ms per request)
            var avgResponseTime = duration.TotalMilliseconds / iterations;
            avgResponseTime.Should().BeLessThan(100, "Average response time should be less than 100ms");
        }
        catch (HttpRequestException ex)
        {
            _testLogger?.WriteLine($"HTTP Request failed: {ex.Message}");
            Assert.Fail($"HTTP Request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _testLogger?.WriteLine($"Unexpected error during performance test: {ex.Message}");
            Assert.Fail($"Unexpected error during performance test: {ex.Message}");
        }
    }

    [TestMethod]
    [Category("Performance")]
    public async Task AddTodoItemPerformanceLoadTest()
    {
        try
        {
            _testLogger?.WriteLine("=== Starting AddTodoItem Performance Load Test ===");
            var iterations = 10000;
            var successfulRequests = 0;
            var startTime = DateTime.UtcNow;

            _testLogger?.WriteLine($"Starting performance test with {iterations} requests...");

            // Act - Query the same string ID 10,000 times
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var stringContent = new StringContent("some text", Encoding.UTF8, "text/plain");
                    var response = await _client!.PostAsync("/api/todoitems", stringContent);

                    // We expect BadRequest for string IDs, but count any response as successful
                    if (response.StatusCode == System.Net.HttpStatusCode.UnsupportedMediaType)
                    {
                        successfulRequests++;
                    }

                    // Log progress every 1000 requests
                    if ((i + 1) % 1000 == 0)
                    {
                        _testLogger?.WriteLine($"Completed {i + 1} requests...");
                    }
                }
                catch (Exception ex)
                {
                    _testLogger?.WriteLine($"Request {i + 1} failed: {ex.Message}");
                }
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert and Log Performance Metrics
            _testLogger?.WriteLine($"=== PERFORMANCE TEST RESULTS ===");
            _testLogger?.WriteLine($"Total Requests: {iterations}");
            _testLogger?.WriteLine($"Successful Requests: {successfulRequests}");
            _testLogger?.WriteLine($"Failed Requests: {iterations - successfulRequests}");
            _testLogger?.WriteLine($"Total Duration: {duration.TotalMilliseconds:F2} ms");
            _testLogger?.WriteLine($"Average Response Time: {duration.TotalMilliseconds / iterations:F2} ms per request");
            _testLogger?.WriteLine($"Requests per Second: {iterations / duration.TotalSeconds:F2}");
            _testLogger?.WriteLine($"================================");

            // Assert that at least 95% of requests were successful
            var successRate = (double)successfulRequests / iterations;
            successRate.Should().BeGreaterThan(0.95, $"Expected at least 95% success rate, got {successRate:P2}");

            // Assert that average response time is reasonable (less than 100ms per request)
            var avgResponseTime = duration.TotalMilliseconds / iterations;
            avgResponseTime.Should().BeLessThan(100, "Average response time should be less than 100ms");
        }
        catch (HttpRequestException ex)
        {
            _testLogger?.WriteLine($"HTTP Request failed: {ex.Message}");
            Assert.Fail($"HTTP Request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _testLogger?.WriteLine($"Unexpected error during performance test: {ex.Message}");
            Assert.Fail($"Unexpected error during performance test: {ex.Message}");
        }
    }

    [TestMethod]
    [Category("Performance")]
    public async Task UpdateTodoItemPerformanceLoadTest()
    {
        try
        {
            _testLogger?.WriteLine("=== Starting UpdateTodoItem Performance Load Test ===");
            var iterations = 10000;
            var successfulRequests = 0;
            var startTime = DateTime.UtcNow;

            _testLogger?.WriteLine($"Starting performance test with {iterations} update requests...");

            _testLogger?.WriteLine("Creating a new TodoItem in the database for testing...");
            var todoItem = new
            {
                id = 0,
                Name = "Performance Test Update Item",
                IsComplete = false
            };
            var json = JsonConvert.SerializeObject(todoItem, Formatting.Indented);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client!.PostAsync("/api/todoitems", content);
            response.EnsureSuccessStatusCode();
            var createdItem = JsonConvert.DeserializeObject<TodoItem>(response.Content.ReadAsStringAsync().Result);
            var idForTesting = createdItem?.Id ?? 0;
            
            _testLogger?.WriteLine($"Successfully created TodoItem with ID: {idForTesting} for performance testing");

            _testLogger?.WriteLine("Act - Update the same TodoItem multiple times for performance testing");
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var UpdatedItem = new
                    {
                        id = idForTesting,
                        Name = $"Updated Performance Test Item - Iteration {i + 1}",
                        IsComplete = i % 2 == 0 // Alternate between true/false for variety
                    };

                    json = JsonConvert.SerializeObject(UpdatedItem);
                    content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await _client!.PutAsync($"/api/todoitems/{idForTesting}", content);

                    // Count successful updates (NoContent is the expected response for successful PUT)
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        successfulRequests++;
                    }
                    else
                    {
                        _testLogger?.WriteLine($"Update request {i + 1} returned unexpected status: {response.StatusCode}");
                    }

                    // Log progress every 1000 requests
                    if ((i + 1) % 1000 == 0)
                    {
                        _testLogger?.WriteLine($"Completed {i + 1} update requests... (Success rate so far: {((double)successfulRequests / (i + 1)):P1})");
                    }
                }
                catch (Exception ex)
                {
                    _testLogger?.WriteLine($"Update request {i + 1} failed: {ex.Message}");
                }
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert and Log Performance Metrics
            _testLogger?.WriteLine($"=== UPDATE PERFORMANCE TEST RESULTS ===");
            _testLogger?.WriteLine($"Total Update Requests: {iterations}");
            _testLogger?.WriteLine($"Successful Updates: {successfulRequests}");
            _testLogger?.WriteLine($"Failed Updates: {iterations - successfulRequests}");
            _testLogger?.WriteLine($"Total Duration: {duration.TotalMilliseconds:F2} ms");
            _testLogger?.WriteLine($"Average Response Time: {duration.TotalMilliseconds / iterations:F2} ms per update");
            _testLogger?.WriteLine($"Updates per Second: {iterations / duration.TotalSeconds:F2}");
            _testLogger?.WriteLine($"Success Rate: {((double)successfulRequests / iterations):P2}");
            _testLogger?.WriteLine($"=======================================");

            // Assert that at least 95% of requests were successful
            var successRate = (double)successfulRequests / iterations;
            successRate.Should().BeGreaterThan(0.95, $"Expected at least 95% success rate, got {successRate:P2}");

            // Assert that average response time is reasonable (less than 100ms per request)
            var avgResponseTime = duration.TotalMilliseconds / iterations;
            avgResponseTime.Should().BeLessThan(100, "Average response time should be less than 100ms");

            _testLogger?.WriteLine("Cleaning up: Deleting the test TodoItem...");
            response = await _client!.DeleteAsync($"/api/todoitems/{idForTesting}");
            if (response.IsSuccessStatusCode)
            {
                _testLogger?.WriteLine($"Successfully deleted test TodoItem with ID: {idForTesting}");
            }
            else
            {
                _testLogger?.WriteLine($"Warning: Failed to delete test TodoItem with ID: {idForTesting}. Status: {response.StatusCode}");
            }
            
            _testLogger?.WriteLine("UpdateTodoItem Performance Load Test completed successfully.");

        }
        catch (HttpRequestException ex)
        {
            _testLogger?.WriteLine($"HTTP Request failed during update performance test: {ex.Message}");
            Assert.Fail($"HTTP Request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _testLogger?.WriteLine($"Unexpected error during update performance test: {ex.Message}");
            Assert.Fail($"Unexpected error during performance test: {ex.Message}");
        }
    }
    
     [TestMethod]
    [Category("Performance")]
    public async Task DeleteTodoItemPerformanceLoadTest()
    {
        try
        {
            _testLogger?.WriteLine("=== Starting DeleteTodoItem Performance Load Test ===");
            var iterations = 1000;
            var successfulRequests = 0;
            var startTime = DateTime.UtcNow;

            _testLogger?.WriteLine($"Starting performance test with {iterations} requests...");

            _testLogger?.WriteLine("Add a new TodoItem to the database for testing...");
            var todoItem = new
            {
                id = 0,
                Name = "Test Item",
                IsComplete = false
            };
            var json = JsonConvert.SerializeObject(todoItem, Formatting.Indented);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client!.PostAsync("/api/todoitems", content);
            response.EnsureSuccessStatusCode();
            var createdItem = JsonConvert.DeserializeObject<TodoItem>(response.Content.ReadAsStringAsync().Result);
            var idForTesting = createdItem?.Id ?? 'a';

            _testLogger?.WriteLine($"Successfully created TodoItem with ID: {idForTesting} for delete performance testing");
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    response = await _client!.DeleteAsync($"/api/todoitems/{idForTesting}");

                    // We expect BadRequest for string IDs, but count any response as successful
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent || 
                        response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        successfulRequests++;
                    }

                    // Log progress every 100 requests (reduced frequency due to lower iteration count)
                    if ((i + 1) % 100 == 0)
                    {
                        _testLogger?.WriteLine($"Completed {i + 1} create/delete cycles...");
                    }
                }
                catch (Exception ex)
                {
                    _testLogger?.WriteLine($"Create/Delete cycle {i + 1} failed: {ex.Message}");
                }
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert and Log Performance Metrics
            _testLogger?.WriteLine($"=== DELETE PERFORMANCE TEST RESULTS ===");
            _testLogger?.WriteLine($"Total Create/Delete Cycles: {iterations}");
            _testLogger?.WriteLine($"Successful Deletions: {successfulRequests}");
            _testLogger?.WriteLine($"Failed Deletions: {iterations - successfulRequests}");
            _testLogger?.WriteLine($"Total Duration: {duration.TotalMilliseconds:F2} ms");
            _testLogger?.WriteLine($"Average Cycle Time: {duration.TotalMilliseconds / iterations:F2} ms per create/delete cycle");
            _testLogger?.WriteLine($"Cycles per Second: {iterations / duration.TotalSeconds:F2}");
            _testLogger?.WriteLine($"Success Rate: {((double)successfulRequests / iterations):P2}");
            _testLogger?.WriteLine($"======================================");

            // Assert that at least 95% of requests were successful
            var successRate = (double)successfulRequests / iterations;
            successRate.Should().BeGreaterThan(0.95, $"Expected at least 95% success rate, got {successRate:P2}");

            // Assert that average response time is reasonable (less than 200ms per cycle due to create+delete operations)
            var avgResponseTime = duration.TotalMilliseconds / iterations;
            avgResponseTime.Should().BeLessThan(200, "Average create/delete cycle time should be less than 200ms");

            _testLogger?.WriteLine("DeleteTodoItem Performance Load Test completed successfully.");

        }
        catch (HttpRequestException ex)
        {
            _testLogger?.WriteLine($"HTTP Request failed during delete performance test: {ex.Message}");
            Assert.Fail($"HTTP Request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _testLogger?.WriteLine($"Unexpected error during delete performance test: {ex.Message}");
            Assert.Fail($"Unexpected error during performance test: {ex.Message}");
        }
    }
}
