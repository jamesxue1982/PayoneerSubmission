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

namespace Web_API_Test;

[TestClass]
[Category("Performance")]
[DoNotParallelize] // This prevents parallel execution of test methods in this class
public sealed class Web_API_Performance_Test
{
    public TestContext? TestContext { get; set; }
    private HttpClient? _client;
    private TestLogger? _testLogger;

    [TestInitialize]
    public void TestInitialize()
    {
        // Note: For actual testing, you would need to reference the Web API project
        // and configure the WebApplicationFactory properly. This is a template
        // showing the structure of comprehensive API tests.

        // For now, we'll create a simple HttpClient for demonstration
        // In real scenarios, replace this with proper WebApplicationFactory setup
        _client = new HttpClient()
        {
            BaseAddress = new Uri("http://localhost:5089") // Changed to HTTP for local development
        };

        //make sure the API is running before tests
        var response = _client.GetAsync("/api/todoitems").Result;
        response.EnsureSuccessStatusCode();

        _testLogger = new TestLogger(TestContext);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _client?.Dispose();
    }


    [TestMethod]
    public async Task PerformanceLoadTest()
    {
        try
        {
            // Arrange
            var testId = "nonexistent_string_id"; // Using string ID to test server handling
            var iterations = 10000;
            var successfulRequests = 0;
            var startTime = DateTime.UtcNow;

            _testLogger?.WriteLine($"Starting performance test with {iterations} requests...");

            // Act - Query the same string ID 10,000 times
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var response = await _client!.GetAsync($"/api/todoitems/{testId}");

                    // We expect BadRequest for string IDs, but count any response as successful
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                        response.StatusCode == System.Net.HttpStatusCode.NotFound)
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
    
}
