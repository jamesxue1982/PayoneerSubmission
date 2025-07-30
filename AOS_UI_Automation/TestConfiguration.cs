using System.Text.Json;

namespace AOS_UI_Automation;

/// <summary>
/// Configuration class that reads settings from PlaywrightSettings.json
/// </summary>
public class TestConfiguration
{
    /// <summary>
    /// The base URL for the Advantage Online Shopping website.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Default timeout in milliseconds for page operations.
    /// </summary>
    public int DefaultTimeout { get; set; }
    
    /// <summary>
    /// Browser slow motion delay in milliseconds for visual debugging.
    /// </summary>
    public int BrowserSlowMo { get; set; }
    
    /// <summary>
    /// Browser name to use for testing.
    /// </summary>
    public string BrowserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Launch options for the browser.
    /// </summary>
    public LaunchOptions LaunchOptions { get; set; } = new();

    private static TestConfiguration? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the singleton instance of TestConfiguration, loading from PlaywrightSettings.json if needed.
    /// </summary>
    public static TestConfiguration Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= LoadConfiguration();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Loads configuration from PlaywrightSettings.json file.
    /// </summary>
    /// <returns>The loaded configuration</returns>
    private static TestConfiguration LoadConfiguration()
    {
        try
        {
            var jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlaywrightSettings.json");
            var jsonString = File.ReadAllText(jsonFilePath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<TestConfiguration>(jsonString, options) 
                   ?? new TestConfiguration();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load configuration from PlaywrightSettings.json: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Launch options for the browser configuration.
/// </summary>
public class LaunchOptions
{
    /// <summary>
    /// Whether to run the browser in headless mode.
    /// </summary>
    public bool Headless { get; set; }
    
    /// <summary>
    /// Command line arguments to pass to the browser.
    /// </summary>
    public string[] Args { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Slow motion delay in milliseconds.
    /// </summary>
    public int SlowMo { get; set; }
}
