// Import the namespace for Entity Framework Core
using Microsoft.EntityFrameworkCore;
// Import the namespace for your models
using TodoApi.Models;

// Create a WebApplicationBuilder instance with the command-line arguments
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add controllers as a service
builder.Services.AddControllers();

// Uncomment this line to use an in-memory database for testing purposes
// builder.Services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase("TodoList"));

// Add a DbContext service using Npgsql as the database provider and get the connection string from the appsettings.json file
builder.Services.AddDbContext<TodoContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("TodoAPIConnectionString")));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// Add services to enable API documentation and UI generation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build the WebApplication instance from the builder
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Use Swagger middleware to serve generated JSON document
    app.UseSwagger();
    // Use Swagger UI middleware to serve interactive UI
    app.UseSwaggerUI();
}

// Use HTTPS redirection middleware to redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

// Use authorization middleware to enforce authentication and authorization policies on endpoints
app.UseAuthorization();

// Map controllers to endpoints using attribute routing
app.MapControllers();

// Run the web application and listen for incoming requests
app.Run();