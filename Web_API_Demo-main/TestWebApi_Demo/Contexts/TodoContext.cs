// Import the namespace for Entity Framework Core
using Microsoft.EntityFrameworkCore;

// Define a namespace for the models
namespace TodoApi.Models;

// Define a class named TodoContext that inherits from DbContext
public class TodoContext : DbContext
{
    // Define a constructor that takes a DbContextOptions<TodoContext> parameter and passes it to the base constructor
    public TodoContext(DbContextOptions<TodoContext> options)
        : base(options)
    {
    }

    // Define a property named TodoItems of type DbSet<TodoItem> that represents a collection of TodoItem entities in the database
    // Use the null-forgiving operator (!) to suppress the nullable warning as this property will be initialized by Entity Framework Core
    public DbSet<TodoItem> TodoItems { get; set; } = null!;
}