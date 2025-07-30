

# Web API Demo

This is a sample web API project using ASP.NET Core and Entity Framework. It allows you to create, read, update and delete (CRUD) todo items from a PostgreSQL database. For testing, the InMemory database can be used.

The project demonstrates how to use ASP.NET Core MVC controllers to handle HTTP requests and responses, how to use Entity Framework Core as an object-relational mapper (ORM) to interact with the database.
The project follows the RESTful design principles and uses JSON as the data format. The project also uses Swagger UI to generate interactive API documentation.

## Getting Started

To run this project locally, you will need:

- .NET 7 SDK
- PostgreSQL server
- Visual Studio Code or Visual Studio
- EF Core - .NET Core CLI

### Terminal Commands

- To install ef cli `dotnet tool install --global dotnet-ef`
- Restore the dependencies using `dotnet restore`
- To create a database migrations `dotnet ef migrations add "Migration_Name"`
- Apply the database migrations using `dotnet ef database update`
- Run the project using `dotnet run`


## Usage

The web API exposes the following endpoints:

| HTTP Method | Route            | Description                      |
| ----------- | ---------------- | -------------------------------- |
| GET         | /api/todoitems   | Get all todo items               |
| GET         | /api/todoitems/1 | Get a specific todo item by id   |
| POST        | /api/todoitems   | Create a new todo item           |
| PUT         | /api/todoitems/1 | Update an existing todo item     |
| DELETE      | /api/todoitems/1 | Delete an existing todo item     |

You can use Postman or Swagger(localhost:port/swagger) to test these endpoints.

<img width="350" alt="image" src="https://user-images.githubusercontent.com/110940406/225851953-cdf7d971-f7ed-4be6-b636-11783467ac65.png">


## Example Response

```bash
curl -X GET https://localhost:5089/api/todoitems
```

The response will look something like this:

```json
[
  {
    "id": 1,
    "name": "Test1",
    "isComplete": false
  },
  {
    "id": 2,
    "name": "Test2",
    "isComplete": true
  }
]
```

## NuGet Packages

- Microsoft.AspNetCore.OpenApi                 7.0.4       
- Microsoft.EntityFrameworkCore                7.0.4       
- Microsoft.EntityFrameworkCore.Design         7.0.4       
- Microsoft.EntityFrameworkCore.InMemory       7.0.4       
- Microsoft.EntityFrameworkCore.SqlServer      7.0.4        
- Microsoft.EntityFrameworkCore.Tools          7.0.4          
- Npgsql.EntityFrameworkCore.PostgreSQL        7.0.3          
- Swashbuckle.AspNetCore                       6.4.0        

## Reference links

- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli)
- [Entity Framework Core Database Providers](https://learn.microsoft.com/en-us/ef/core/providers/?tabs=dotnet-core-cli)
- [Entity Framework Core tools reference - .NET Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
- [Tutorial: Create a web API with ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-7.0&tabs=visual-studio-code)
- [Tutorial - Persist and retrieve relational data with Entity Framework Core](https://learn.microsoft.com/en-us/training/modules/persist-data-ef-core/)


## License

[MIT](https://github.com/seymenbahtiyar/Web_API_Demo/blob/main/LICENSE)


