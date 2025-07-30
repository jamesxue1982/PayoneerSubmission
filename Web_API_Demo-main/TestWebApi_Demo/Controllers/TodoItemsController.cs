// Import the namespace for ASP.NET Core MVC
using Microsoft.AspNetCore.Mvc;
// Import the namespace for Entity Framework Core
using Microsoft.EntityFrameworkCore;
// Import the namespace for your models
using TodoApi.Models;

// Define a namespace for your controllers
namespace TodoApi.Controllers
{
    // Annotate the class with Route attribute to specify the route template for this controller
    [Route("api/[controller]")]
    // Annotate the class with ApiController attribute to enable API-specific features such as model validation and automatic 400 responses
    [ApiController]
    // Define a controller class named TodoItemsController that inherits from ControllerBase
    public class TodoItemsController : ControllerBase
    {
        // Define a private readonly field to store a reference to the database context
        private readonly TodoContext _context;

        // Define a constructor that takes a TodoContext parameter and assigns it to the field
        public TodoItemsController(TodoContext context)
        {
            _context = context;
        }

        // Annotate the method with HttpGet attribute to handle GET requests without parameters
        [HttpGet]
        // Define an async method that returns an ActionResult of IEnumerable of TodoItem
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            // Return all the todo items from the database as an asynchronous operation
            return await _context.TodoItems.ToListAsync();
        }

        // Annotate the method with HttpGet attribute to handle GET requests with an id parameter
        [HttpGet("{id}")]
        // Define an async method that takes a long parameter named id and returns an ActionResult of TodoItem
        public async Task<ActionResult<TodoItem>> GetTodoItem(long id)
        {
            // Find a todo item by its id from the database as an asynchronous operation
            var todoItem = await _context.TodoItems.FindAsync(id);

            // If the todo item is not found, return a 404 Not Found response
            if (todoItem == null)
            {
                return NotFound();
            }

            // Otherwise, return the todo item as a 200 OK response
            return todoItem;
        }

        // Annotate the method with HttpPut attribute to handle PUT requests with an id parameter
        [HttpPut("{id}")]
        // Define an async method that takes a long parameter named id and a TodoItem parameter named todoItem and returns an IActionResult
        public async Task<IActionResult> PutTodoItem(long id, TodoItem todoItem)
        {
            // If the id does not match the Id property of todoItem, return a 400 Bad Request response
            if (id != todoItem.Id)
            {
                return BadRequest();
            }

            // Mark the todo item entity as modified in the database context
            _context.Entry(todoItem).State = EntityState.Modified;

            try
            {
                // Save the changes to the database as an asynchronous operation
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // If there is a concurrency exception, check if the todo item exists in the database
                if (!TodoItemExists(id))
                {
                    // If not, return a 404 Not Found response
                    return NotFound();
                }
                else
                {
                    // Otherwise, rethrow the exception
                    throw;
                }
            }
            // Return a 204 No Content response to indicate success
            return NoContent();
        }

        // Annotate the method with HttpPost attribute to handle POST requests without parameters
        [HttpPost]
        // Define an async method that takes a TodoItem parameter named todoItem and returns an ActionResult of TodoItem
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
        {
            // Add the todo item entity to the database context
            _context.TodoItems.Add(todoItem);
            // Save the changes to the database as an asynchronous operation
            await _context.SaveChangesAsync();

            // Return a 201 Created response with the location and value of the created todo item
            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }

        // Annotate the method with HttpDelete attribute to handle DELETE requests with an id parameter
        [HttpDelete("{id}")]
        // Define an async method that takes a long parameter named id and returns an IActionResult
        public async Task<IActionResult> DeleteTodoItem(long id)
        {
            // Find a todo item by its id from the database as an asynchronous operation
            var todoItem = await _context.TodoItems.FindAsync(id);

            // If the todo item is not found, return a 404 Not Found response
            if (todoItem == null)
            {
                return NotFound();
            }

            // Remove the todo item entity from the database context
            _context.TodoItems.Remove(todoItem);
            // Save the changes to the database as an asynchronous operation
            await _context.SaveChangesAsync();

            // Return a 204 No Content response to indicate success
            return NoContent();
        }

        // Define a private method that takes a long parameter named id and returns a bool indicating whether a todo item with that id exists in the database
        private bool TodoItemExists(long id)
        {
            // Use LINQ to check if any entity in TodoItems has an Id property equal to id
            return _context.TodoItems.Any(e => e.Id == id);
        }
    }
}
