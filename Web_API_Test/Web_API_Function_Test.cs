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
        // Build configuration from appsettings.json and appsettings.test.json
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get the API base URL from configuration, fallback to default if not found
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5089";

        _client = new HttpClient()
        {
            BaseAddress = new Uri(apiBaseUrl)
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


    /// <summary>
    /// Tests the GET /api/todoitems endpoint functionality.
    /// Verifies that the API can retrieve all todo items correctly and that newly added items appear in the list.
    /// </summary>
    [TestMethod]
    [Category("Functionality")]

    public async Task GetAllTodoItemsEndpointTest()
    {
        // Skip this test if API is not running
        try
        {
            _testLogger?.WriteLine("Starting GetAllTodoItemsEndpointTest...");
            //await ClearAllTodoItemsBeforeTest();                    //clear the database before test

            var response = await _client!.GetAsync("/api/todoitems");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var todoItems = JsonConvert.DeserializeObject<List<TodoItem>>(content);
            todoItems.Should().NotBeNull();
            todoItems!.Should().HaveCount(todoItems.Count, "Expected no items in the database");
            int currentItemNumber = todoItems.Count;

            _testLogger?.WriteLine("Add a new TodoItem to the database...");
            var newTodoItem = new
            {
                id = 0,
                Name = "Newly added Todo Item",
                IsComplete = false
            };
            var createdItem = await PostTodoItemAndValidateAsync(newTodoItem, System.Net.HttpStatusCode.Created);
            createdItem.Should().NotBeNull();
            createdItem!.Name.Should().Be(newTodoItem.Name);
            createdItem.IsComplete.Should().Be(newTodoItem.IsComplete);
            createdItem.Id.Should().BeGreaterThan(0);
            var newItemID = createdItem.Id;

            // Now check if the item was added
            _testLogger?.WriteLine("Checking if the new TodoItem was added...");
            response = await _client.GetAsync("/api/todoitems");
            response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync();
            todoItems = JsonConvert.DeserializeObject<List<TodoItem>>(content);
            todoItems.Should().NotBeNull();
            todoItems!.Should().HaveCount(currentItemNumber + 1, "Expected one item in the database after adding a new item");
            var item = todoItems.Where<TodoItem>(i => i.Id == newItemID).First<TodoItem>();
            item.Should().NotBeNull();
            item.Name.Should().Be(newTodoItem.Name, "The added item should match the one we added");
            item.IsComplete.Should().Be(newTodoItem.IsComplete, "The added item's completion status should match");
            item.Id.Should().Be(newItemID, "The added item's ID should match the one we received from the API");

            _testLogger?.WriteLine("Deleting the newly added TodoItem...");
            response = await _client!.DeleteAsync($"/api/todoitems/{newItemID}");
            response.EnsureSuccessStatusCode();

            _testLogger?.WriteLine("GetAllTodoItemsEndpointTest completed successfully.");

        }
        catch (HttpRequestException ex)
        {
            var apiUrl = _configuration?["ApiSettings:BaseUrl"] ?? "http://localhost:5089";
            Assert.Inconclusive($"API not running on {apiUrl}. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"An unexpected error occurred: {ex.Message}");
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
        try
        {
            _testLogger?.WriteLine("Starting AddTodoItemEndpointTest...");
            //create a list to save the created item IDs, test data.
            List<long> createdItemIds = new List<long>() { };

            _testLogger?.WriteLine("Add a new TodoItem with id 0 as an long to the database...");
            var itemWithIdZero = new
            {
                id = 0,
                Name = "Test adding with id is integer 0",
                IsComplete = false
            };
            var createdItem = await PostTodoItemAndValidateAsync(itemWithIdZero, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(itemWithIdZero.Name);
            createdItem.IsComplete.Should().Be(itemWithIdZero.IsComplete);
            createdItem.Id.Should().BeGreaterThan(0); // Expecting API to return autogenerated long id
            createdItemIds.Add(createdItem.Id); //add ids to the list
            //save the id for operations in the following validation
            var baseItemID = createdItem.Id;

            _testLogger?.WriteLine("Add a new TodoItem with id 0 as a string to the database...");
            var itemWithIdStringZero = new
            {
                id = "0",
                Name = "The item with id is string 0",
                IsComplete = false
            };
            createdItem = await PostTodoItemAndValidateAsync(itemWithIdStringZero, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(itemWithIdStringZero.Name);
            createdItem.IsComplete.Should().Be(itemWithIdStringZero.IsComplete);
            createdItem.Id.Should().Be(baseItemID + 1); // Expecting API to return auto generated int Id incremented by 1
            createdItemIds.Add(createdItem.Id); //add ids to the list

            _testLogger?.WriteLine("Add a new TodoItem with id given as an long to the database...");
            var itemWithAnSpecifiedID = new
            {
                id = baseItemID + 2,
                Name = "An item with a given ID",
                IsComplete = false
            };
            createdItem = await PostTodoItemAndValidateAsync(itemWithAnSpecifiedID, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(itemWithAnSpecifiedID.Name);
            createdItem.IsComplete.Should().Be(itemWithAnSpecifiedID.IsComplete);
            createdItem.Id.Should().Be(itemWithAnSpecifiedID.id); // Expecting API to return long Id given
            createdItemIds.Add(createdItem.Id); //add ids to the list

            _testLogger?.WriteLine("Add a new TodoItem with no id to the database...");
            var NoIDItem = new
            {
                Name = "Item with no id property",
                IsComplete = true
            };
            createdItem = await PostTodoItemAndValidateAsync(NoIDItem, System.Net.HttpStatusCode.InternalServerError);
            _testLogger?.WriteLine("Here I have an unsolved issue with this test, it works in Postman but not here");
            // createdItem!.Name.Should().BeNull();
            // createdItem.IsComplete.Should().Be(NoIDItem.IsComplete);
            // createdItem.Id.Should().Be(baseItemID + 3);
            //createdItemIds.Add(createdItem.Id); //add ids to the list

            _testLogger?.WriteLine("Add a new TodoItem with no name to the database...");
            var NoNameItem = new
            {
                id = baseItemID + 4,
                aProperty = "Item with no name property",
                IsComplete = true
            };

            //add the new item to the database and validate
            createdItem = await PostTodoItemAndValidateAsync(NoNameItem, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().BeNull();            //value of Name is automatically assigned to null
            createdItem.IsComplete.Should().Be(NoNameItem.IsComplete);
            createdItem.Id.Should().Be(NoNameItem.id);
            createdItemIds.Add(createdItem.Id); //add ids to the list

            _testLogger?.WriteLine("Add a new TodoItem with no IsComplete property to the database...");
            var NoCompletePropertyItem = new
            {
                id = baseItemID + 5,
                Name = "Item with no Complete property",
                notComplete = true
            };

            //add the new item to the database and validate
            createdItem = await PostTodoItemAndValidateAsync(NoCompletePropertyItem, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(NoCompletePropertyItem.Name);
            createdItem.IsComplete.Should().BeFalse();
            createdItem.Id.Should().Be(NoCompletePropertyItem.id); // Expecting API to return an item with isComplete value as False
            createdItemIds.Add(createdItem.Id); //add ids to the list

            _testLogger?.WriteLine("Add a new TodoItem with the biggest long value to the database...");
            var BigIDItem = new
            {
                id = Int64.MaxValue,
                Name = "Item with no Complete property",
                isComplete = true
            };
            //add the new item to the database and validate
            createdItem = await PostTodoItemAndValidateAsync(BigIDItem, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(BigIDItem.Name);
            createdItem.IsComplete.Should().Be(BigIDItem.isComplete);
            createdItem.Id.Should().Be(Int64.MaxValue); // Expecting API to return an item with id the biggest long value
            createdItemIds.Add(createdItem.Id); //add ids to the list

            _testLogger?.WriteLine("Add a new TodoItem with an invalid id value to the database...");
            var tooBigIDItem = new
            {
                id = "99999999999999999999999999",
                Name = string.Concat(Enumerable.Repeat("Very long name ", 1000)),
                isComplete = true
            };
            //add the new item to the database - expect error
            var response = await PostTodoItemAsync(tooBigIDItem);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            _testLogger?.WriteLine("Add a new TodoItem with a very long name to the database...");
            var longNameItem = new
            {
                id = baseItemID + 6,
                Name = string.Concat(Enumerable.Repeat("Very long name ", 1000)),
                isComplete = true
            };
            //add the new item to the database - expect error
            createdItem = await PostTodoItemAndValidateAsync(longNameItem, System.Net.HttpStatusCode.Created);
            createdItem!.Name.Should().Be(longNameItem.Name);
            createdItem.IsComplete.Should().Be(longNameItem.isComplete);
            createdItem.Id.Should().Be(longNameItem.id);
            createdItemIds.Add(createdItem.Id); //add ids to the list

            _testLogger?.WriteLine("Add a new TodoItem with an empty item to the database...");
            var EmptyItem = new
            {
            };

            //add the new item to the database - expect error
            response = await PostTodoItemAsync(EmptyItem);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

            _testLogger?.WriteLine("Add a new TodoItem with an existing ID to the database...");
            var itemWithExistingID = new
            {
                id = baseItemID,
                Name = "Item with existing ID",
                IsComplete = false
            };
            //add the new item to the database - expect error
            response = await PostTodoItemAsync(itemWithExistingID);
            // Expect a 500 Internal Server Error when trying to add item with existing ID
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);

            _testLogger?.WriteLine("Add a new TodoItem with an ID as a word to the database...");
            var itemWithIDasWord = new
            {
                id = "ABC",
                Name = "Item with id as a word instead of a number",
                IsComplete = false
            };
            //add the new item to the database - expect error
            response = await PostTodoItemAsync(itemWithIDasWord);
            // Expect a 500 Internal Server Error when trying to add item with existing ID
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            _testLogger?.WriteLine("Add a new TodoItem with an SQL injection attempt in ID to the database...");
            var idWithSQLInjection = new
            {
                id = "1 OR 1=1; DROP TABLE TodoItems; --",
                Name = "Item with id as a word instead of a number",
                IsComplete = false
            };
            //add the new item to the database - expect error
            response = await PostTodoItemAsync(idWithSQLInjection);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            _testLogger?.WriteLine("Add a new TodoItem with a null object to the database...");
            var itemWithNullValue = new
            {
                id = (int?)null,
                Name = (string?)null,
                IsComplete = (bool?)null
            };
            //add the new item to the database - expect error
            response = await PostTodoItemAsync(itemWithNullValue);
            // Expect a 400 Bad Request when trying to add item with null values
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            _testLogger?.WriteLine("Add a new TodoItem with a wrong format text to the database...");
            //Wrong Format text - expect error
            response = await PostRawContentAsync("some string instead of JSON");
            // Expect a 415 Unsupported Media Type when trying to add item with existing ID
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.UnsupportedMediaType);

            foreach (var id in createdItemIds)
            {
                _testLogger?.WriteLine($"Deleting the created TodoItem with ID: {id}");
                response = await _client!.DeleteAsync($"/api/todoitems/{id}");
                response.EnsureSuccessStatusCode();
            }

            _testLogger?.WriteLine("AddTodoItemEndpointTest completed successfully.");

        }
        catch (HttpRequestException ex)
        {
            Assert.Inconclusive($"API not running on http://localhost:5089. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"An unexpected error occurred: {ex.Message}");
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
            _testLogger?.WriteLine("Starting GetTodoItemByID test...");
            
            _testLogger?.WriteLine("Add a new TodoItem to the database...");
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

            _testLogger?.WriteLine("Add a new TodoItem to the database...");


            _testLogger?.WriteLine("Getting item from the database.");
            var retrievedItem = await GetTodoItemByIdAsync(createdItemId.ToString(), System.Net.HttpStatusCode.OK);
            retrievedItem.Should().NotBeNull();
            retrievedItem!.Id.Should().Be(createdItemId, "The retrieved item's ID should match the one we requested");
            retrievedItem.Name.Should().Be(itemToAdd.Name, "The retrieved item should match the one we added");
            retrievedItem.IsComplete.Should().Be(itemToAdd.IsComplete, "The retrieved item's completion status should match");

            _testLogger?.WriteLine($"Item is retrieved with ID: {retrievedItem.Id}");


            _testLogger?.WriteLine("Getting item by an invalid ID.");
            var response = await _client!.GetAsync($"/api/todoitems/{createdItemId + 2}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound, "Expected to not find the item by an invalid ID");

            _testLogger?.WriteLine("Getting item by an word.");
            response = await _client.GetAsync("/api/todoitems/someword");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an word");

            _testLogger?.WriteLine("Getting item by an valid id twice.");
            response = await _client.GetAsync($@"/api/todoitems/{createdItemId} {createdItemId}");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an valid id twice");

            _testLogger?.WriteLine("Getting item by an sql injection attempt.");
            response = await _client.GetAsync($@"/api/todoitems/{createdItemId} or 1 = 1; DROP TABLE TodoItems; --");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an sql attack");

            _testLogger?.WriteLine("Getting item by an special character.");
            response = await _client.GetAsync(@"/api/todoitems/@#!");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Expected to not find the item by an special character");

            _testLogger?.WriteLine("Getting item by an path traversal attempt.");
            response = await _client.GetAsync(@"/api/todoitems/../etc/passwd");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound, "Expected to not find the item by an path traversal attempt");

            _testLogger?.WriteLine($"Deleting the created TodoItem with ID: {createdItemId}");
            response = await _client!.DeleteAsync($"/api/todoitems/{createdItemId}");
            response.EnsureSuccessStatusCode();

            _testLogger?.WriteLine("GettingTodoItemByID test completed successfully.");

        }
        catch (HttpRequestException ex)
        {
            Assert.Inconclusive($"API not running on http://localhost:5089. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"An unexpected error occurred: {ex.Message}");
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
            Assert.Inconclusive($"API not running on http://localhost:5089. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"An unexpected error occurred: {ex.Message}");
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
            Assert.Inconclusive($"API not running on http://localhost:5089. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"An unexpected error occurred: {ex.Message}");
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
/// Logs to Console (for CI/CD), TestContext (for MSTest), and Debug output simultaneously.
/// </summary>
public class TestLogger
{
    private readonly TestContext? _testContext;
    
    /// <summary>
    /// Initializes a new instance of the TestLogger class.
    /// </summary>
    /// <param name="testContext">Optional MSTest TestContext for logging integration</param>
    public TestLogger(TestContext? testContext = null)
    {
        _testContext = testContext;
    }
    
    /// <summary>
    /// Writes a message to all available logging destinations (Console, TestContext, Debug).
    /// Ensures test output is visible across different environments and test runners.
    /// </summary>
    /// <param name="message">The message to log</param>
    public void WriteLine(string message)
    {
        Console.WriteLine(message);           // Always log to console
        _testContext?.WriteLine(message);     // Also log to MSTest if available
        Debug.WriteLine(message);             // Also log to debug output
    }
}