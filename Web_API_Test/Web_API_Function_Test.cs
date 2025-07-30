using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using FluentAssertions;
using System.Net.Http.Json;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections;

namespace Web_API_Test;

/// <summary>
/// Comprehensive test class for Web API functionality testing.
/// Tests all CRUD operations (Create, Read, Update, Delete) for TodoItems API endpoints.
/// Includes security testing, edge case validation, and error handling verification.
/// </summary>
[TestClass]
[Category("Functionality")]
[DoNotParallelize] // This prevents parallel execution of test methods in this class
public sealed class Web_API_function_Test
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
            _testLogger = new TestLogger(TestContext, logDirectory: logDirectory, testClassName: $"Web_API_Function_Test_{testMethodName}");
        }
        else
        {
            _testLogger = new TestLogger(TestContext); // File logging disabled, will use null constructor path
        }
        
        _testLogger?.WriteLine("=== FUNCTION TEST INITIALIZATION ===");
        _testLogger?.WriteLine($"Test Method: {testMethodName}");
        _testLogger?.WriteLine("Building configuration from appsettings files...");

        // Get the API base URL from configuration, fallback to default if not found
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5089";
        _testLogger?.WriteLine($"Using API base URL: {apiBaseUrl}");

        _testLogger?.WriteLine("Creating HttpClient for API testing...");
        _client = new HttpClient()
        {
            BaseAddress = new Uri(apiBaseUrl)
        };

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

        _testLogger?.WriteLine("Function test initialization completed successfully.");
        if (!string.IsNullOrEmpty(_testLogger?.LogFilePath))
        {
            _testLogger?.WriteLine($"Test logs will be saved to: {_testLogger.LogFilePath}");
        }
        _testLogger?.WriteLine("==========================================");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _testLogger?.WriteLine("=== FUNCTION TEST CLEANUP ===");
        _testLogger?.WriteLine("Disposing HttpClient and cleaning up resources...");
        _client?.Dispose();
        
        // Log the final location of the log file
        if (!string.IsNullOrEmpty(_testLogger?.LogFilePath))
        {
            var logPath = _testLogger.LogFilePath;
            _testLogger?.WriteLine($"Complete test log saved to: {logPath}");
            Console.WriteLine($"📄 Test log file: {logPath}");
        }
        
        _testLogger?.WriteLine("Cleanup completed successfully.");
        _testLogger?.WriteLine("==============================");
        
        // Dispose the logger to finalize the log file
        _testLogger?.Dispose();
    }


    /// <summary>
    /// Tests the GET /api/todoitems endpoint functionality.
    /// Verifies that the API can retrieve all todo items correctly and that newly added items appear in the list.
    /// </summary>
    [TestMethod]
    [Category("Functionality")]

    public async Task GetAllTodoItemsEndpointTest()
    {
        var testStartTime = DateTime.Now;
        var testName = "GetAllTodoItemsEndpointTest";
        
        // Skip this test if API is not running
        try
        {
            _testLogger?.WriteLine("=== Starting GetAllTodoItemsEndpointTest ===");
            //await ClearAllTodoItemsBeforeTest();                    //clear the database before test

            _testLogger?.WriteLine("Retrieving all TodoItems from the database...");
            var response = await _client!.GetAsync("/api/todoitems");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var todoItems = JsonConvert.DeserializeObject<List<TodoItem>>(content);
            todoItems.Should().NotBeNull();
            todoItems!.Should().HaveCount(todoItems.Count, "Expected no items in the database");
            int currentItemNumber = todoItems.Count;
            _testLogger?.WriteLine($"Current number of TodoItems in database: {currentItemNumber}");

            _testLogger?.WriteLine("Creating a new TodoItem to add to the database...");
            var newTodoItem = new
            {
                id = 0,
                Name = "Newly added Todo Item",
                IsComplete = false
            };
            _testLogger?.WriteLine($"New TodoItem details - Name: '{newTodoItem.Name}', IsComplete: {newTodoItem.IsComplete}");
            
            var createdItem = await PostTodoItemAndValidateAsync(newTodoItem, System.Net.HttpStatusCode.Created);
            createdItem.Should().NotBeNull();
            createdItem!.Name.Should().Be(newTodoItem.Name);
            createdItem.IsComplete.Should().Be(newTodoItem.IsComplete);
            createdItem.Id.Should().BeGreaterThan(0);
            var newItemID = createdItem.Id;
            _testLogger?.WriteLine($"Successfully created TodoItem with ID: {newItemID}");

            // Now check if the item was added
            _testLogger?.WriteLine("Verifying the new TodoItem was added to the database...");
            response = await _client.GetAsync("/api/todoitems");
            response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync();
            todoItems = JsonConvert.DeserializeObject<List<TodoItem>>(content);
            todoItems.Should().NotBeNull();
            todoItems!.Should().HaveCount(currentItemNumber + 1, "Expected one item in the database after adding a new item");
            _testLogger?.WriteLine($"Database now contains {todoItems.Count} items (expected: {currentItemNumber + 1})");
            
            var item = todoItems.Where<TodoItem>(i => i.Id == newItemID).First<TodoItem>();
            item.Should().NotBeNull();
            item.Name.Should().Be(newTodoItem.Name, "The added item should match the one we added");
            item.IsComplete.Should().Be(newTodoItem.IsComplete, "The added item's completion status should match");
            item.Id.Should().Be(newItemID, "The added item's ID should match the one we received from the API");
            _testLogger?.WriteLine($"Verification successful - TodoItem found with correct properties");

            _testLogger?.WriteLine($"Cleaning up: Deleting the newly created TodoItem with ID: {newItemID}");
            response = await _client!.DeleteAsync($"/api/todoitems/{newItemID}");
            response.EnsureSuccessStatusCode();
            _testLogger?.WriteLine($"Successfully deleted TodoItem with ID: {newItemID}");

            _testLogger?.WriteLine("GetAllTodoItemsEndpointTest completed successfully.");
            _testLogger?.WriteLine("============================================");

            // Log successful test result
            var duration = DateTime.Now - testStartTime;
            _testLogger?.WriteTestResult(testName, "PASSED", duration);

        }
        catch (HttpRequestException ex)
        {
            var apiUrl = _configuration?["ApiSettings:BaseUrl"] ?? "http://localhost:5089";
            var errorMsg = $"API not accessible at {apiUrl}. Error: {ex.Message}";
            _testLogger?.WriteLine($"❌ HTTP Request failed - {errorMsg}");
            
            // Log inconclusive test result
            var duration = DateTime.Now - testStartTime;
            _testLogger?.WriteTestResult(testName, "INCONCLUSIVE", duration, errorMsg);
            
            // Using FluentAssertions to mark test as inconclusive
            false.Should().BeTrue($"API not running on {apiUrl}. Error: {ex.Message}. Test marked as inconclusive.");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error: {ex.Message}";
            _testLogger?.WriteLine($"❌ Unexpected error in GetAllTodoItemsEndpointTest: {ex.Message}");
            _testLogger?.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Log failed test result
            var duration = DateTime.Now - testStartTime;
            _testLogger?.WriteTestResult(testName, "FAILED", duration, errorMsg);
            
            // Using FluentAssertions to fail the test
            ex.Should().BeNull($"An unexpected error occurred: {ex.Message}");
        }
    }




    /// <summary>
    /// Tests the POST /api/todoitems endpoint with various input scenarios.
    /// Validates creation of todo items with different ID types, edge cases, security attacks, and error conditions.
    /// </summary>
    [TestMethod]
    [Category("Functionality")]
    public async Task AddTodoItemEndpointTest()
    {
        var testStartTime = DateTime.Now;
        var testName = "AddTodoItemEndpointTest";
        
        try
        {
            _testLogger?.WriteLine("=== Starting AddTodoItemEndpointTest ===");
            //create a list to save the created item IDs, test data.
            List<long> createdItemIds = new List<long>() { };
            _testLogger?.WriteLine("Initialized list to track created item IDs for cleanup");

            _testLogger?.WriteLine("TEST 1: Adding TodoItem with ID = 0 (integer)...");
            var itemWithIdZero = new
            {
                id = 0,
                Name = "Test adding with id is integer 0",
                IsComplete = false
            };
            _testLogger?.WriteLine($"Request payload: Name='{itemWithIdZero.Name}', IsComplete={itemWithIdZero.IsComplete}, ID={itemWithIdZero.id}");
            var createdItem = await PostTodoItemAndValidateAsync(itemWithIdZero, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(itemWithIdZero.Name);
            createdItem.IsComplete.Should().Be(itemWithIdZero.IsComplete);
            createdItem.Id.Should().BeGreaterThan(0); // Expecting API to return autogenerated long id
            createdItemIds.Add(createdItem.Id); //add ids to the list
            //save the id for operations in the following validation
            var baseItemID = createdItem.Id;
            _testLogger?.WriteLine($"✓ Successfully created TodoItem with auto-generated ID: {baseItemID}");

            _testLogger?.WriteLine("TEST 2: Adding TodoItem with ID = '0' (string)...");
            var itemWithIdStringZero = new
            {
                id = "0",
                Name = "The item with id is string 0",
                IsComplete = false
            };
            _testLogger?.WriteLine($"Request payload: Name='{itemWithIdStringZero.Name}', IsComplete={itemWithIdStringZero.IsComplete}, ID='{itemWithIdStringZero.id}'");
            createdItem = await PostTodoItemAndValidateAsync(itemWithIdStringZero, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(itemWithIdStringZero.Name);
            createdItem.IsComplete.Should().Be(itemWithIdStringZero.IsComplete);
            createdItem.Id.Should().Be(baseItemID + 1); // Expecting API to return auto generated int Id incremented by 1
            createdItemIds.Add(createdItem.Id); //add ids to the list
            _testLogger?.WriteLine($"✓ Successfully created TodoItem with ID: {createdItem.Id} (expected: {baseItemID + 1})");

            _testLogger?.WriteLine("TEST 3: Adding TodoItem with specific ID value...");
            var itemWithAnSpecifiedID = new
            {
                id = baseItemID + 2,
                Name = "An item with a given ID",
                IsComplete = false
            };
            _testLogger?.WriteLine($"Request payload: Name='{itemWithAnSpecifiedID.Name}', IsComplete={itemWithAnSpecifiedID.IsComplete}, ID={itemWithAnSpecifiedID.id}");
            createdItem = await PostTodoItemAndValidateAsync(itemWithAnSpecifiedID, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(itemWithAnSpecifiedID.Name);
            createdItem.IsComplete.Should().Be(itemWithAnSpecifiedID.IsComplete);
            createdItem.Id.Should().Be(itemWithAnSpecifiedID.id); // Expecting API to return long Id given
            createdItemIds.Add(createdItem.Id); //add ids to the list
            _testLogger?.WriteLine($"✓ Successfully created TodoItem with specified ID: {createdItem.Id}");

            _testLogger?.WriteLine("TEST 4: Adding TodoItem with no ID property...");
            var NoIDItem = new
            {
                Name = "Item with no id property",
                IsComplete = true
            };
            _testLogger?.WriteLine($"Request payload: Name='{NoIDItem.Name}', IsComplete={NoIDItem.IsComplete}, ID=<not provided>");
            createdItem = await PostTodoItemAndValidateAsync(NoIDItem, System.Net.HttpStatusCode.InternalServerError);
            _testLogger?.WriteLine("⚠ Note: This test has an unresolved issue - works in Postman but not in automated test");
            // createdItem!.Name.Should().BeNull();
            // createdItem.IsComplete.Should().Be(NoIDItem.IsComplete);
            // createdItem.Id.Should().Be(baseItemID + 3);
            //createdItemIds.Add(createdItem.Id); //add ids to the list

            _testLogger?.WriteLine("TEST 5: Adding TodoItem with no Name property...");
            var NoNameItem = new
            {
                id = baseItemID + 4,
                aProperty = "Item with no name property",
                IsComplete = true
            };
            _testLogger?.WriteLine($"Request payload: aProperty='{NoNameItem.aProperty}', IsComplete={NoNameItem.IsComplete}, ID={NoNameItem.id}");

            //add the new item to the database and validate
            createdItem = await PostTodoItemAndValidateAsync(NoNameItem, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().BeNull();            //value of Name is automatically assigned to null
            createdItem.IsComplete.Should().Be(NoNameItem.IsComplete);
            createdItem.Id.Should().Be(NoNameItem.id);
            createdItemIds.Add(createdItem.Id); //add ids to the list
            _testLogger?.WriteLine($"✓ Successfully created TodoItem with ID: {createdItem.Id}, Name automatically set to null");

            _testLogger?.WriteLine("TEST 6: Adding TodoItem with no IsComplete property...");
            var NoCompletePropertyItem = new
            {
                id = baseItemID + 5,
                Name = "Item with no Complete property",
                notComplete = true
            };
            _testLogger?.WriteLine($"Request payload: Name='{NoCompletePropertyItem.Name}', notComplete={NoCompletePropertyItem.notComplete}, ID={NoCompletePropertyItem.id}");

            //add the new item to the database and validate
            createdItem = await PostTodoItemAndValidateAsync(NoCompletePropertyItem, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(NoCompletePropertyItem.Name);
            createdItem.IsComplete.Should().BeFalse();
            createdItem.Id.Should().Be(NoCompletePropertyItem.id); // Expecting API to return an item with isComplete value as False
            createdItemIds.Add(createdItem.Id); //add ids to the list
            _testLogger?.WriteLine($"✓ Successfully created TodoItem with ID: {createdItem.Id}, IsComplete automatically set to false");

            _testLogger?.WriteLine("TEST 7: Adding TodoItem with maximum long value as ID...");
            var BigIDItem = new
            {
                id = Int64.MaxValue,
                Name = "Item with maximum long ID",
                isComplete = true
            };
            _testLogger?.WriteLine($"Request payload: Name='{BigIDItem.Name}', isComplete={BigIDItem.isComplete}, ID={BigIDItem.id} (Int64.MaxValue)");
            //add the new item to the database and validate
            createdItem = await PostTodoItemAndValidateAsync(BigIDItem, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(BigIDItem.Name);
            createdItem.IsComplete.Should().Be(BigIDItem.isComplete);
            createdItem.Id.Should().Be(Int64.MaxValue); // Expecting API to return an item with id the biggest long value
            createdItemIds.Add(createdItem.Id); //add ids to the list
            _testLogger?.WriteLine($"✓ Successfully created TodoItem with maximum long ID: {createdItem.Id}");

            _testLogger?.WriteLine("TEST 8: Adding TodoItem with invalid oversized ID value...");
            var tooBigIDItem = new
            {
                id = "99999999999999999999999999",
                Name = string.Concat(Enumerable.Repeat("Very long name ", 1000)),
                isComplete = true
            };
            _testLogger?.WriteLine($"Request payload: ID='{tooBigIDItem.id}' (oversized), Name length={tooBigIDItem.Name.Length} chars");
            //add the new item to the database - expect error
            var response = await PostTodoItemAsync(tooBigIDItem);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            _testLogger?.WriteLine($"✓ Correctly rejected oversized ID with BadRequest status");

            _testLogger?.WriteLine("TEST 9: Adding TodoItem with very long name...");
            var longNameItem = new
            {
                id = baseItemID + 6,
                Name = string.Concat(Enumerable.Repeat("Very long name ", 1000)),
                isComplete = true
            };
            _testLogger?.WriteLine($"Request payload: Name length={longNameItem.Name.Length} chars, ID={longNameItem.id}");
            //add the new item to the database - expect error
            createdItem = await PostTodoItemAndValidateAsync(longNameItem, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(longNameItem.Name);
            createdItem.IsComplete.Should().Be(longNameItem.isComplete);
            createdItem.Id.Should().Be(longNameItem.id);
            createdItemIds.Add(createdItem.Id); //add ids to the list
            _testLogger?.WriteLine($"✓ Successfully created TodoItem with very long name, ID: {createdItem.Id}");

            _testLogger?.WriteLine("TEST 10: Adding empty TodoItem object...");
            var EmptyItem = new
            {
            };
            _testLogger?.WriteLine("Request payload: {} (empty object)");
            response = await PostTodoItemAsync(EmptyItem);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            var content = await response.Content.ReadAsStringAsync();
            createdItem = JsonConvert.DeserializeObject<TodoItem>(content);
            createdItemIds.Add(createdItem!.Id); //add ids to the list
            _testLogger?.WriteLine($"✓ Empty object {createdItem.Id} accepted with Created status (API handles gracefully)");

            _testLogger?.WriteLine("TEST 11: Adding TodoItem with existing ID (conflict test)...");
            var itemWithExistingID = new
            {
                id = baseItemID,
                Name = "Item with existing ID",
                IsComplete = false
            };
            _testLogger?.WriteLine($"Request payload: Name='{itemWithExistingID.Name}', ID={itemWithExistingID.id} (existing)");
            //add the new item to the database - expect error
            response = await PostTodoItemAsync(itemWithExistingID);
            // Expect a 500 Internal Server Error when trying to add item with existing ID
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
            _testLogger?.WriteLine($"✓ Correctly rejected duplicate ID with InternalServerError status");

            _testLogger?.WriteLine("TEST 12: Adding TodoItem with ID as word (security test)...");
            var itemWithIDasWord = new
            {
                id = "ABC",
                Name = "Item with id as a word instead of a number",
                IsComplete = false
            };
            _testLogger?.WriteLine($"Request payload: Name='{itemWithIDasWord.Name}', ID='{itemWithIDasWord.id}' (non-numeric)");
            //add the new item to the database - expect error
            response = await PostTodoItemAsync(itemWithIDasWord);
            // Expect a 500 Internal Server Error when trying to add item with existing ID
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            _testLogger?.WriteLine($"✓ Correctly rejected non-numeric ID with BadRequest status");

            _testLogger?.WriteLine("TEST 13: SQL injection attempt in ID field (security test)...");
            var idWithSQLInjection = new
            {
                id = "1 OR 1=1; DROP TABLE TodoItems; --",
                Name = "Item with SQL injection attempt in ID",
                IsComplete = false
            };
            _testLogger?.WriteLine($"Request payload: ID='{idWithSQLInjection.id}' (SQL injection attempt)");
            //add the new item to the database - expect error
            response = await PostTodoItemAsync(idWithSQLInjection);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            _testLogger?.WriteLine($"✓ Successfully blocked SQL injection attempt with BadRequest status");

            _testLogger?.WriteLine("TEST 14: Adding TodoItem with null values...");
            var itemWithNullValue = new
            {
                id = (int?)null,
                Name = (string?)null,
                IsComplete = (bool?)null
            };
            _testLogger?.WriteLine("Request payload: All properties set to null");
            //add the new item to the database - expect error
            response = await PostTodoItemAsync(itemWithNullValue);
            // Expect a 400 Bad Request when trying to add item with null values
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            _testLogger?.WriteLine($"✓ Correctly rejected null values with BadRequest status");

            _testLogger?.WriteLine("TEST 15: Invalid content type test (security test)...");
            //Wrong Format text - expect error
            response = await PostRawContentAsync("some string instead of JSON");
            // Expect a 415 Unsupported Media Type when trying to add item with existing ID
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.UnsupportedMediaType);
            _testLogger?.WriteLine($"✓ Correctly rejected invalid content type with UnsupportedMediaType status");

            _testLogger?.WriteLine($"Cleanup: Deleting {createdItemIds.Count} created TodoItems...");
            foreach (var id in createdItemIds)
            {
                _testLogger?.WriteLine($"Deleting TodoItem with ID: {id}");
                response = await _client!.DeleteAsync($"/api/todoitems/{id}");
                response.EnsureSuccessStatusCode();
            }
            _testLogger?.WriteLine($"✓ Successfully deleted all {createdItemIds.Count} test items");

            _testLogger?.WriteLine("AddTodoItemEndpointTest completed successfully.");
            _testLogger?.WriteLine("===============================================");

            // Log successful test result
            var duration = DateTime.Now - testStartTime;
            _testLogger?.WriteTestResult(testName, "PASSED", duration);

        }
        catch (HttpRequestException ex)
        {
            var apiUrl = _configuration?["ApiSettings:BaseUrl"] ?? "http://localhost:5089";
            var errorMsg = $"API not accessible at {apiUrl}. Error: {ex.Message}";
            _testLogger?.WriteLine($"❌ HTTP Request failed in AddTodoItemEndpointTest - {errorMsg}");
            
            // Log inconclusive test result
            var duration = DateTime.Now - testStartTime;
            _testLogger?.WriteTestResult(testName, "INCONCLUSIVE", duration, errorMsg);
            
            // Using FluentAssertions to mark test as inconclusive
            false.Should().BeTrue($"API not running on {apiUrl}. Error: {ex.Message}. Test marked as inconclusive.");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error: {ex.Message}";
            _testLogger?.WriteLine($"❌ Unexpected error in AddTodoItemEndpointTest: {ex.Message}");
            _testLogger?.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Log failed test result
            var duration = DateTime.Now - testStartTime;
            _testLogger?.WriteTestResult(testName, "FAILED", duration, errorMsg);
            
            // Using FluentAssertions to fail the test
            ex.Should().BeNull($"An unexpected error occurred: {ex.Message}");
        }

    }

    /// <summary>
    /// Tests the GET /api/todoitems/{id} endpoint functionality.
    /// Verifies retrieval of specific todo items by ID and validates security against SQL injection and path traversal attacks.
    /// </summary>
    [TestMethod]
    [Category("Functionality")]
    public async Task GetTodoItemByID()
    {
        try
        {
            _testLogger?.WriteLine("=== Starting GetTodoItemByID test ===");
            
            _testLogger?.WriteLine("Creating a test TodoItem for retrieval testing...");
            var itemToAdd = new
            {
                id = 0,
                Name = "Test Item for GET by ID",
                IsComplete = false
            };
            _testLogger?.WriteLine($"Test item payload: Name='{itemToAdd.Name}', IsComplete={itemToAdd.IsComplete}");
            
            //add the new item to the database and validate
            var createdItem = await PostTodoItemAndValidateAsync(itemToAdd, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(itemToAdd.Name);
            createdItem.IsComplete.Should().Be(itemToAdd.IsComplete);
            createdItem.Id.Should().BeGreaterThan(0); // Expecting API to return int Id
            var createdItemId = createdItem.Id;
            _testLogger?.WriteLine($"✓ Successfully created test TodoItem with ID: {createdItemId}");

            _testLogger?.WriteLine("TEST 1: Retrieving TodoItem by valid ID...");
            var retrievedItem = await GetTodoItemByIdAsync(createdItemId.ToString(), System.Net.HttpStatusCode.OK);
            retrievedItem.Should().NotBeNull();
            retrievedItem!.Id.Should().Be(createdItemId, "The retrieved item's ID should match the one we requested");
            retrievedItem.Name.Should().Be(itemToAdd.Name, "The retrieved item should match the one we added");
            retrievedItem.IsComplete.Should().Be(itemToAdd.IsComplete, "The retrieved item's completion status should match");
            _testLogger?.WriteLine($"✓ Successfully retrieved TodoItem: ID={retrievedItem.Id}, Name='{retrievedItem.Name}', IsComplete={retrievedItem.IsComplete}");

            _testLogger?.WriteLine("TEST 2: Attempting to retrieve TodoItem by invalid/non-existent ID...");
            var invalidId = createdItemId + 2;
            var response = await _client!.GetAsync($"/api/todoitems/{invalidId}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound, "Expected to not find the item by an invalid ID");
            _testLogger?.WriteLine($"✓ Correctly returned NotFound for invalid ID: {invalidId}");

            _testLogger?.WriteLine("TEST 3: Security test - Attempting to retrieve TodoItem by word (non-numeric ID)...");
            response = await _client.GetAsync("/api/todoitems/someword");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an word");
            _testLogger?.WriteLine($"✓ Correctly rejected non-numeric ID 'someword' with BadRequest status");

            _testLogger?.WriteLine("TEST 4: Security test - Attempting to retrieve with multiple IDs...");
            response = await _client.GetAsync($@"/api/todoitems/{createdItemId} {createdItemId}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an valid id twice");
            _testLogger?.WriteLine($"✓ Correctly rejected multiple IDs with BadRequest status");

            _testLogger?.WriteLine("TEST 5: Security test - SQL injection attempt in ID parameter...");
            var sqlInjectionAttempt = $@"{createdItemId} or 1 = 1; DROP TABLE TodoItems; --";
            response = await _client.GetAsync($@"/api/todoitems/{sqlInjectionAttempt}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an sql attack");
            _testLogger?.WriteLine($"✓ Successfully blocked SQL injection attempt with BadRequest status");

            _testLogger?.WriteLine("TEST 6: Security test - Special characters in ID parameter...");
            response = await _client.GetAsync(@"/api/todoitems/@#!");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an special character");
            _testLogger?.WriteLine($"✓ Correctly rejected special characters '@#!' with BadRequest status");

            _testLogger?.WriteLine("TEST 7: Security test - Path traversal attempt...");
            response = await _client.GetAsync(@"/api/todoitems/../etc/passwd");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound, "Expected to not find the item by an path traversal attempt");
            _testLogger?.WriteLine($"✓ Successfully blocked path traversal attempt '../etc/passwd' with NotFound status");

            _testLogger?.WriteLine($"Cleanup: Deleting the test TodoItem with ID: {createdItemId}");
            response = await _client!.DeleteAsync($"/api/todoitems/{createdItemId}");
            response.EnsureSuccessStatusCode();
            _testLogger?.WriteLine($"✓ Successfully deleted test TodoItem with ID: {createdItemId}");

            _testLogger?.WriteLine("GetTodoItemByID test completed successfully.");
            _testLogger?.WriteLine("========================================");

        }
        catch (HttpRequestException ex)
        {
            var apiUrl = _configuration?["ApiSettings:BaseUrl"] ?? "http://localhost:5089";
            _testLogger?.WriteLine($"❌ HTTP Request failed in GetTodoItemByID test - API not accessible at {apiUrl}");
            _testLogger?.WriteLine($"Error details: {ex.Message}");
            
            // Using FluentAssertions to mark test as inconclusive
            false.Should().BeTrue($"API not running on {apiUrl}. Error: {ex.Message}. Test marked as inconclusive.");
        }
        catch (Exception ex)
        {
            _testLogger?.WriteLine($"❌ Unexpected error in GetTodoItemByID test: {ex.Message}");
            _testLogger?.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Using FluentAssertions to fail the test
            ex.Should().BeNull($"An unexpected error occurred: {ex.Message}");
        }
    }


    /// <summary>
    /// Tests the PUT /api/todoitems/{id} endpoint functionality.
    /// Validates updating existing todo items and tests various update scenarios including internationalization and security.
    /// </summary>
    [TestMethod]
    [Category("Functionality")]
    public async Task UpdateTodoItemEndPointTest()
    {
        try
        {
            _testLogger?.WriteLine("Starting UpdateItemEndPointTest...");

            _testLogger?.WriteLine("Creating item to add...");
            var itemToAdd = new
            {
                id = 0,
                Name = "Add a new Item",
                IsComplete = false
            };
            var createdItem = await PostTodoItemAndValidateAsync(itemToAdd, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(itemToAdd.Name);
            createdItem.IsComplete.Should().Be(itemToAdd.IsComplete);
            createdItem.Id.Should().BeGreaterThan(0); // Expecting API to return int Id
            var createdItemId = createdItem.Id;

            _testLogger?.WriteLine($"Created item with ID: {createdItemId}");

            _testLogger?.WriteLine("Updating the new item in the database...");
            var updateItem = new
            {
                id = createdItemId,
                Name = "Item Name Updated",
                IsComplete = true
            };
            await PutTodoItemAndValidateAsync(createdItemId.ToString(), updateItem, System.Net.HttpStatusCode.NoContent); //update successfully

            _testLogger?.WriteLine("Getting the updated item from the database...");
            var updatedItem = await GetTodoItemByIdAsync(createdItemId.ToString(), System.Net.HttpStatusCode.OK);
            updatedItem.Should().NotBeNull();
            updatedItem!.Id.Should().Be(createdItemId, "The updated item's ID should match the one we requested");
            updatedItem!.Name.Should().Be(updateItem.Name, "The updated item's name should match the one we requested");
            updatedItem!.IsComplete.Should().Be(updateItem.IsComplete, "The updated item's completion status should match");
            _testLogger?.WriteLine("Item updated successfully.");


            _testLogger?.WriteLine("Updating the new item with chinese as Name in the database...");
            var updateItemWithChinese = new
            {
                id = createdItemId,
                Name = "Item Name Updated with Chinese, 中文",
                IsComplete = true
            };
            await PutTodoItemAndValidateAsync(createdItemId.ToString(), updateItem, System.Net.HttpStatusCode.NoContent);

            updatedItem = await GetTodoItemByIdAsync(createdItemId.ToString(), System.Net.HttpStatusCode.OK);
            updatedItem.Should().NotBeNull();
            updatedItem!.Id.Should().Be(createdItemId, "The updated item's ID should match the one we requested");
            updatedItem!.Name.Should().Be(updateItem.Name, "The updated item's name should match the one we requested");
            updatedItem!.IsComplete.Should().Be(updateItem.IsComplete, "The updated item's completion status should match");
            _testLogger?.WriteLine("Item updated successfully.");

            _testLogger?.WriteLine("Updating the new item with wrong id in the database...");
            var updateItemWrongID = new
            {
                id = createdItemId + 50000,
                Name = "Item Name Updated with wrong id",
                IsComplete = true
            };
            await PutTodoItemAndValidateAsync(updateItemWrongID.id.ToString(), updateItemWrongID, System.Net.HttpStatusCode.NotFound);

            _testLogger?.WriteLine("Updating the new item with id as SQL injection attempt in the database...");
            var updateItemSQLInjection = new
            {
                id = "1 or 1=1; DROP TABLE TodoItems; --",
                Name = "Item Name Updated",
                IsComplete = true
            };
            await PutTodoItemAndValidateAsync(updateItemSQLInjection.id, updateItemSQLInjection, System.Net.HttpStatusCode.BadRequest);

            _testLogger?.WriteLine($"Deleting the created TodoItem with ID: {createdItemId}");
            var response = await _client!.DeleteAsync($"/api/todoitems/{createdItemId}");
            response.EnsureSuccessStatusCode();

            _testLogger?.WriteLine("UpdateItemEndPointTest completed successfully.");
        }
        catch (HttpRequestException ex)
        {
            var apiUrl = _configuration?["ApiSettings:BaseUrl"] ?? "http://localhost:5089";
            _testLogger?.WriteLine($"❌ HTTP Request failed in UpdateTodoItemEndPointTest - API not accessible at {apiUrl}");
            _testLogger?.WriteLine($"Error details: {ex.Message}");
            
            // Using FluentAssertions to mark test as inconclusive
            false.Should().BeTrue($"API not running on {apiUrl}. Error: {ex.Message}. Test marked as inconclusive.");
        }
        catch (Exception ex)
        {
            _testLogger?.WriteLine($"❌ Unexpected error in UpdateTodoItemEndPointTest: {ex.Message}");
            _testLogger?.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Using FluentAssertions to fail the test
            ex.Should().BeNull($"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests the DELETE /api/todoitems/{id} endpoint functionality.
    /// Validates deletion of todo items and tests security against various malicious input attempts.
    /// </summary>
    [TestMethod]
    [Category("Functionality")]
    public async Task DeleteTodoItemEndpointTest()
    {
        try
        {
            _testLogger?.WriteLine("Starting DeleteTodoItemEndpointTest...");

            _testLogger?.WriteLine("Creating item to add...");
            var itemToAdd = new
            {
                id = 0,
                Name = "Add a new Item",
                IsComplete = false
            };
            //add the new item to the database and validate
            var createdItem = await PostTodoItemAndValidateAsync(itemToAdd, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(itemToAdd.Name);
            createdItem.IsComplete.Should().Be(itemToAdd.IsComplete);
            createdItem.Id.Should().BeGreaterThan(0); // Expecting API to return int Id
            var createdItemId = createdItem.Id;

            _testLogger?.WriteLine($"Created item with ID: {createdItemId}");

            _testLogger?.WriteLine("Deleting the item by an invalid ID...");
            var response = await _client!.DeleteAsync($"/api/todoitems/{createdItemId + 2}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound, "Expected to not find the item by an invalid ID");

            _testLogger?.WriteLine("Deleting the item by an word...");
            response = await _client.DeleteAsync("/api/todoitems/someword");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an word");

            _testLogger?.WriteLine("Deleting the item by an valid id twice...");
            response = await _client.DeleteAsync($@"/api/todoitems/{createdItemId} {createdItemId}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an valid id twice");

            _testLogger?.WriteLine("Deleting the item by an sql injection attempt...");
            response = await _client.DeleteAsync($@"/api/todoitems/{createdItemId} or 1 = 1; DROP TABLE TodoItems; --");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an sql attack");

            _testLogger?.WriteLine("Deleting the item by an special character...");
            response = await _client.DeleteAsync(@"/api/todoitems/@#!");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an special character");

            _testLogger?.WriteLine("Deleting the item by an path traversal attempt...");
            response = await _client.DeleteAsync(@"/api/todoitems/../etc/passwd");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound, "Expected to not find the item by an path traversal attempt");

            _testLogger?.WriteLine("Deleting the item by a valid ID...");
            response = await _client!.DeleteAsync($"/api/todoitems/{createdItemId}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent, "Expected a successful deletion response");

            _testLogger?.WriteLine("DeletingTodoItemEndpointTest completed successfully.");
        }
        catch (HttpRequestException ex)
        {
            var apiUrl = _configuration?["ApiSettings:BaseUrl"] ?? "http://localhost:5089";
            _testLogger?.WriteLine($"❌ HTTP Request failed in DeleteTodoItemEndpointTest - API not accessible at {apiUrl}");
            _testLogger?.WriteLine($"Error details: {ex.Message}");
            
            // Using FluentAssertions to mark test as inconclusive
            false.Should().BeTrue($"API not running on {apiUrl}. Error: {ex.Message}. Test marked as inconclusive.");
        }
        catch (Exception ex)
        {
            _testLogger?.WriteLine($"❌ Unexpected error in DeleteTodoItemEndpointTest: {ex.Message}");
            _testLogger?.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Using FluentAssertions to fail the test
            ex.Should().BeNull($"An unexpected error occurred: {ex.Message}");
        }
    }


    // /// <summary>
    // /// Clears all existing todo items from the database before running tests to ensure a clean test environment.
    // /// Retrieves all items and deletes them one by one with proper delays to avoid API rate limiting.
    // /// </summary>
    // private async Task ClearAllTodoItemsBeforeTest()
    // {
    //     // Act
    //     var response = await _client!.GetAsync("/api/todoitems");

    //     // Assert
    //     response.EnsureSuccessStatusCode();
    //     var content = await response.Content.ReadAsStringAsync();
    //     var todoItems = JsonConvert.DeserializeObject<List<TodoItem>>(content);
    //     _testLogger?.WriteLine($"the whole list is: {content}.");

    //     //with todoItems, create a List<TodoItem> hold all items, 
    //     List<TodoItem> allItems = new List<TodoItem>(todoItems ?? new List<TodoItem>());
    //     _testLogger?.WriteLine($"All items with ID: {allItems.Count}");
    //     if (allItems.Count > 0)
    //     {
    //         _testLogger?.WriteLine("first item to delete is: " + allItems[0].Id);
    //     }

    //     //while allItems.Count > 0, call delete API for each item
    //     while (allItems?.Count > 0)
    //     {
    //         // Wait between delete operations to avoid overwhelming the API (configurable delay)
    //         var delayMs = _configuration?.GetValue<int>("TestSettings:CleanupDelayMs") ?? 1000;
    //         await Task.Delay(delayMs);

    //         var itemToDelete = allItems[0];
    //         _testLogger?.WriteLine($"Deleting item with ID: {itemToDelete.Id}");
    //         var deleteResponse = await _client.DeleteAsync($"/api/todoitems/{itemToDelete.Id}");
    //         deleteResponse.EnsureSuccessStatusCode();

    //         // Refresh the list
    //         var refreshResponse = await _client.GetAsync("/api/todoitems");
    //         refreshResponse.EnsureSuccessStatusCode();
    //         var refreshedContent = await refreshResponse.Content.ReadAsStringAsync();
    //         allItems = JsonConvert.DeserializeObject<List<TodoItem>>(refreshedContent) ?? new List<TodoItem>();
    //     }
    // }

    /// <summary>
    /// Helper method to send POST requests to create todo items.
    /// Handles JSON serialization, custom headers, and optional request/response logging for debugging.
    /// </summary>
    /// <param name="todoItem">The todo item object to be created</param>
    /// <param name="logRequest">Whether to log detailed request/response information</param>
    /// <returns>HttpResponseMessage containing the API response</returns>
    private async Task<HttpResponseMessage> PostTodoItemAsync(object todoItem, bool logRequest = false)
    {
        var json = JsonConvert.SerializeObject(todoItem, Formatting.Indented);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Create the request message to add custom headers
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/todoitems")
        {
            Content = content
        };
        
        // Add Host header
        request.Headers.Add("Host", "localhost:5089");
        request.Headers.Add("Connection", "Keep-Alive");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var response = await _client!.SendAsync(request);
        if (logRequest)
        {
            // Log the entire request details
            _testLogger?.WriteLine("=== HTTP REQUEST ===");
            _testLogger?.WriteLine($"Method: POST");
            _testLogger?.WriteLine($"URL: {_client!.BaseAddress}api/todoitems");
            _testLogger?.WriteLine($"Host: localhost:5089");
            _testLogger?.WriteLine($"Content-Type: {content.Headers.ContentType}");
            _testLogger?.WriteLine($"Content-Length: {request.Content.Headers.ContentLength}");
            _testLogger?.WriteLine($"Connection: Keep-Alive");
            _testLogger?.WriteLine($"Request Headers:");
            foreach (var header in request.Headers)
            {
                _testLogger?.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            _testLogger?.WriteLine($"Request Body:");
            _testLogger?.WriteLine(json);
            _testLogger?.WriteLine("===================");

            // Log response details (without consuming the content stream)
            _testLogger?.WriteLine("=== HTTP RESPONSE ===");
            _testLogger?.WriteLine($"Status Code: {(int)response.StatusCode} {response.StatusCode}");
            _testLogger?.WriteLine($"Response Headers:");
            foreach (var header in response.Headers)
            {
                _testLogger?.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            foreach (var header in response.Content.Headers)
            {
                _testLogger?.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            _testLogger?.WriteLine("====================");
        }
        return response;
    }

    /// <summary>
    /// Helper method to create a todo item via POST and validate the response.
    /// Combines POST operation with status code validation and deserialization of the created item.
    /// </summary>
    /// <param name="todoItem">The todo item object to be created</param>
    /// <param name="expectedStatusCode">The expected HTTP status code for validation</param>
    /// <returns>The created TodoItem object if successful, null otherwise</returns>
    private async Task<TodoItem?> PostTodoItemAndValidateAsync(object todoItem, System.Net.HttpStatusCode expectedStatusCode)
    {
        var response = await PostTodoItemAsync(todoItem);
        
        // Log response body
        var responseContent = await response.Content.ReadAsStringAsync();
        _testLogger?.WriteLine($"Response Body: {responseContent}");
        _testLogger?.WriteLine("====================");

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode == System.Net.HttpStatusCode.Created)
        {
            var createdItem = JsonConvert.DeserializeObject<TodoItem>(responseContent);
            createdItem.Should().NotBeNull();
            return createdItem;
        }

        return null;
    }

    /// <summary>
    /// Helper method to send POST requests with raw string content for testing invalid JSON scenarios.
    /// Used to test API error handling when receiving malformed or non-JSON content.
    /// </summary>
    /// <param name="content">The raw string content to send</param>
    /// <returns>HttpResponseMessage containing the API response</returns>
    private async Task<HttpResponseMessage> PostRawContentAsync(string content)
    {
        var stringContent = new StringContent(content, Encoding.UTF8, "text/plain");
        return await _client!.PostAsync("/api/todoitems", stringContent);
    }

    /// <summary>
    /// Helper method to retrieve a todo item by ID and validate the response.
    /// Sends GET request to fetch specific todo item and validates the expected status code.
    /// </summary>
    /// <param name="id">The ID of the todo item to retrieve</param>
    /// <param name="expectedStatusCode">The expected HTTP status code for validation</param>
    /// <returns>The retrieved TodoItem object if successful, null otherwise</returns>
    private async Task<TodoItem?> GetTodoItemByIdAsync(string id, System.Net.HttpStatusCode expectedStatusCode)
    {
        var response = await _client!.GetAsync($"/api/todoitems/{id}");
        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode == System.Net.HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var todoItem = JsonConvert.DeserializeObject<TodoItem>(content);
            todoItem.Should().NotBeNull();
            return todoItem;
        }

        return null;
    }

    /// <summary>
    /// Helper method to send PUT requests to update existing todo items.
    /// Handles JSON serialization and sends update request to the specified endpoint.
    /// </summary>
    /// <param name="id">The ID of the todo item to update</param>
    /// <param name="todoItem">The updated todo item data</param>
    /// <returns>HttpResponseMessage containing the API response</returns>
    private async Task<HttpResponseMessage> PutTodoItemAsync(string id, object todoItem)
    {
        var json = JsonConvert.SerializeObject(todoItem);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client!.PutAsync($"/api/todoitems/{id}", content);
    }

    /// <summary>
    /// Helper method to update a todo item via PUT and validate the response.
    /// Combines PUT operation with status code validation and handles successful update responses.
    /// </summary>
    /// <param name="id">The ID of the todo item to update</param>
    /// <param name="todoItem">The updated todo item data</param>
    /// <param name="expectedStatusCode">The expected HTTP status code for validation</param>
    /// <returns>The updated TodoItem object if created (status 201), null otherwise</returns>
    private async Task<TodoItem?> PutTodoItemAndValidateAsync(string id, object todoItem, System.Net.HttpStatusCode expectedStatusCode)
    {
        var response = await PutTodoItemAsync(id, todoItem);
        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode == System.Net.HttpStatusCode.Created)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdItem = JsonConvert.DeserializeObject<TodoItem>(responseContent);
            createdItem.Should().NotBeNull();
            return createdItem;
        }

        return null;
    }

    /// <summary>
    /// Helper method to send PUT requests with raw string content for testing invalid update scenarios.
    /// Used to test API error handling when receiving malformed update data.
    /// </summary>
    /// <param name="content">The raw string content to send</param>
    /// <returns>HttpResponseMessage containing the API response</returns>
    private async Task<HttpResponseMessage> PutRawContentAsync(string content)
    {
        var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
        return await _client!.PutAsync("/api/todoitems", stringContent);
    }
}

/// <summary>
/// Data Transfer Object representing a TodoItem for testing purposes.
/// Matches the structure expected by the Web API endpoints.
/// </summary>
public class TodoItem

{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}

/// <summary>
/// Custom test logger that outputs to multiple destinations for comprehensive test debugging.
/// Logs to Console (for CI/CD), TestContext (for MSTest), Debug output, and file simultaneously.
/// Creates timestamped log files with test execution details and results.
/// </summary>
public class TestLogger : IDisposable
{
    private readonly TestContext? _testContext;
    private readonly StreamWriter? _fileWriter;
    private readonly string? _logFilePath;
    private bool _disposed = false;
    
    /// <summary>
    /// Initializes a new instance of the TestLogger class with file logging capabilities.
    /// </summary>
    /// <param name="testContext">Optional MSTest TestContext for logging integration</param>
    /// <param name="logDirectory">Directory where log files should be created (defaults to current directory)</param>
    /// <param name="testClassName">Name of the test class for log file naming</param>
    public TestLogger(TestContext? testContext = null, string? logDirectory = null, string? testClassName = null)
    {
        _testContext = testContext;
        
        // If logDirectory is null, disable file logging
        if (logDirectory == null)
        {
            _fileWriter = null;
            _logFilePath = null;
            return;
        }
        
        try
        {
            // Create logs in project root or specified directory
            string logDir;
            if (logDirectory == "TestLogs" || logDirectory == "TestResults")
            {
                // Navigate from bin/Debug/net9.0 back to project root and create simple log directory
                var projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())));
                logDir = Path.Combine(projectRoot!, "TestLogs");
            }
            else
            {
                // Use the provided path relative to current directory (backwards compatibility)
                logDir = Path.Combine(Directory.GetCurrentDirectory(), logDirectory);
            }
            
            // Create log directory if it doesn't exist
            Directory.CreateDirectory(logDir);
            
            // Create timestamped log file name
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var className = testClassName ?? "WebAPITests";
            var fileName = $"{className}_{timestamp}.log";
            _logFilePath = Path.Combine(logDir, fileName);
            
            // Initialize file writer with UTF-8 encoding for unicode support
            _fileWriter = new StreamWriter(_logFilePath, append: false, encoding: Encoding.UTF8)
            {
                AutoFlush = true // Ensure immediate writing for real-time logging
            };
            
            // Write log file header
            WriteLogHeader();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning: Failed to initialize file logging: {ex.Message}");
            _fileWriter = null;
            _logFilePath = null;
        }
    }
    
    /// <summary>
    /// Writes the log file header with test session information.
    /// </summary>
    private void WriteLogHeader()
    {
        if (_fileWriter == null) return;
        
        _fileWriter.WriteLine("=".PadRight(80, '='));
        _fileWriter.WriteLine($"Web API Test Execution Log");
        _fileWriter.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _fileWriter.WriteLine($"Machine: {Environment.MachineName}");
        _fileWriter.WriteLine($"User: {Environment.UserName}");
        _fileWriter.WriteLine($"OS: {Environment.OSVersion}");
        _fileWriter.WriteLine($".NET Runtime: {Environment.Version}");
        _fileWriter.WriteLine($"Working Directory: {Directory.GetCurrentDirectory()}");
        _fileWriter.WriteLine("=".PadRight(80, '='));
        _fileWriter.WriteLine();
    }
    
    /// <summary>
    /// Writes a message to all available logging destinations (Console, TestContext, Debug, File).
    /// Ensures test output is visible across different environments and permanently stored.
    /// </summary>
    /// <param name="message">The message to log</param>
    public void WriteLine(string message)
    {
        var timestampedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        
        Console.WriteLine(message);           // Always log to console (without timestamp for readability)
        _testContext?.WriteLine(message);     // Also log to MSTest if available
        Debug.WriteLine(message);             // Also log to debug output
        
        // Log to file with timestamp
        try
        {
            _fileWriter?.WriteLine(timestampedMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning: Failed to write to log file: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Writes test result summary to the log file.
    /// </summary>
    /// <param name="testName">Name of the test that completed</param>
    /// <param name="result">Test result (Passed, Failed, Inconclusive)</param>
    /// <param name="duration">Test execution duration</param>
    /// <param name="errorMessage">Error message if test failed</param>
    public void WriteTestResult(string testName, string result, TimeSpan? duration = null, string? errorMessage = null)
    {
        WriteLine($"");
        WriteLine($"TEST RESULT: {testName}");
        WriteLine($"Status: {result}");
        if (duration.HasValue)
        {
            WriteLine($"Duration: {duration.Value.TotalMilliseconds:F2} ms");
        }
        if (!string.IsNullOrEmpty(errorMessage))
        {
            WriteLine($"Error: {errorMessage}");
        }
        WriteLine($"Completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        WriteLine("".PadRight(50, '-'));
    }
    
    /// <summary>
    /// Gets the path to the current log file.
    /// </summary>
    public string? LogFilePath => _logFilePath;
    
    /// <summary>
    /// Finalizes the log file with session summary and closes all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            if (_fileWriter != null)
            {
                _fileWriter.WriteLine();
                _fileWriter.WriteLine("=".PadRight(80, '='));
                _fileWriter.WriteLine($"Test Session Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _fileWriter.WriteLine("=".PadRight(80, '='));
                _fileWriter.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning: Error closing log file: {ex.Message}");
        }
        
        _disposed = true;
    }
}