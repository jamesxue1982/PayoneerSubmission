// Define a namespace for the models
namespace TodoApi.Models {
  // Define a class named TodoItem
  public class TodoItem {
    // Define a property named Id of type long
    public long Id { get; set; }
    // Define a property named Name of type string that can be null
    public string? Name { get; set; }
    // Define a property named IsComplete of type bool
    public bool IsComplete { get; set; }
  }
}