using System.ComponentModel;
using Microsoft.Playwright;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace AOS_UI_Automation;

/// <summary>
/// Test class for validating Advantage Online Shopping cart functionality.
/// Tests product addition, pricing calculations, and cart validation scenarios.
/// </summary>
[TestClass]
public class ShoppingCartTests
{
    public TestContext TestContext { get; set; } = null!;

    private TestLogger _testLogger = null!;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IPlaywright? _playwright;

    public enum ProductCategory
    {
        LaptopsCategory,
        MiceCategory,
        TabletsCategory
    }

    #region Constants

    // CSS Selectors
    private const string PRICE_SELECTOR = "#Description > h2";
    private const string QUANTITY_INPUT_SELECTOR = "input[name=\"quantity\"]";
    private const string SHOPPING_CART_SELECTOR = "#shoppingCart";
    private const string CART_TOTAL_SELECTOR = "#shoppingCart > table span.roboto-medium.ng-binding";
    private const string PRODUCT_ROW_SELECTOR = "tr[ng-repeat*='product in cart.productsInCart'].ng-scope";

    #endregion

    /// <summary>
    /// Test initialization method that runs before each test.
    /// Sets up the browser, context, and page for testing.
    /// </summary>
    [TestInitialize]
    public async Task TestInitialize()
    {
        _testLogger = new TestLogger(TestContext);
        _testLogger.WriteLine("Initializing test environment...");
        
        var config = TestConfiguration.Instance;
        
        // Initialize Playwright with browser configuration
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = config.LaunchOptions.Headless,
            Args = config.LaunchOptions.Args,
            SlowMo = config.LaunchOptions.SlowMo
        });

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = null // Use full screen
        });
        _context.SetDefaultTimeout(config.DefaultTimeout);
        _page = await _context.NewPageAsync();
        
        _testLogger.WriteLine("Test environment initialized successfully.");
    }

    /// <summary>
    /// Test cleanup method that runs after each test.
    /// Closes the page, context, and browser to free up resources.
    /// </summary>
    [TestCleanup]
    public async Task TestCleanup()
    {
        _testLogger.WriteLine("Cleaning up test environment...");
        
        if (_page != null)
        {
            await _page.CloseAsync();
            _page = null;
        }

        if (_context != null)
        {
            await _context.CloseAsync();
            _context = null;
        }

        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
        
        _testLogger.WriteLine("Test environment cleaned up successfully.");
        
        // Finalize the log file
        _testLogger?.FinalizeLog();
    }

    /// <summary>
    /// Test method to validate the shopping cart functionality including:
    /// - Adding multiple products to cart
    /// - Verifying quantities and prices
    /// - Validating total cart amount
    /// </summary>
    [TestMethod]
    [Category("ShoppingCart")]
    public async Task AOS_AddToCart_PriceValidation()
    {
        // Read data from CSV file
        var csvFilePath = Path.Combine(GetProjectRootDirectory(), "shopping_items.csv");
        var products = ReadShoppingItemsFromCsv(csvFilePath);
        
        if (products.Count == 0)
        {
            Assert.Fail("No products loaded from CSV file");
        }

        decimal totalPrice;

        _testLogger.WriteLine("Test started: AOS_AddToCart_PriceValidation");

        // Navigate to the Advantage Online Shopping website
        _testLogger.WriteLine($"Navigating to: {TestConfiguration.Instance.BaseUrl}");
        await _page!.GotoAsync(TestConfiguration.Instance.BaseUrl);
        await Assertions.Expect(_page).ToHaveTitleAsync(new Regex("Advantage Shopping"));
        _testLogger.WriteLine("Successfully navigated to AOS website and verified title");

        // Add products from CSV to the cart
        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];
            _testLogger.WriteLine($"Adding {product.Category} product to cart...");
            var updatedProduct = await AddProductToCartAsync(_page, product.Category.ToString(), product);
            products[i] = updatedProduct; // Update the list with pricing information
        }

        _testLogger.WriteLine("Navigating to shopping cart...");
        await _page.GetByRole(AriaRole.Link, new() { Name = "ShoppingCart" }).ClickAsync();
        await _page.Locator(SHOPPING_CART_SELECTOR).WaitForAsync();

        // Assert: Validate the content in shopping cart
        _testLogger.WriteLine("Start Shopping Cart and price validation");

        // Get the shopping cart element and validate product count
        var shoppingCartDiv = _page.Locator(SHOPPING_CART_SELECTOR);
        var productRows = await shoppingCartDiv.Locator(PRODUCT_ROW_SELECTOR).CountAsync();
        _testLogger.WriteLine($"Found {productRows} products in shopping cart");
        Assert.AreEqual(products.Count, productRows, $"Expected exactly {products.Count} products in the cart");

        // Validate each product in the cart
        for (int i = 0; i < productRows; i++)
        {
            var row = shoppingCartDiv.Locator(PRODUCT_ROW_SELECTOR).Nth(i);

            // Get product name from this row
            var productName = await row.Locator("label.roboto-regular.productName").InnerTextAsync();
            _testLogger.WriteLine($"\n--- Validating Product {i + 1}: {productName} ---");

            // Get color from this row - color is in the title attribute of the element with class "productColor"
            var colorElement = row.Locator(".productColor");
            var cartColor = "";
            try
            {
                cartColor = await colorElement.GetAttributeAsync("title") ?? "";
                _testLogger.WriteLine($"Cart color: {cartColor}");
            }
            catch (Exception ex)
            {
                _testLogger.WriteLine($"⚠️ Could not extract color from cart row: {ex.Message}");
            }

            // Get quantity from this row
            var quantityText = await row.Locator("td.smollCell.quantityMobile label.ng-binding").InnerTextAsync();
            var cartQuantity = int.Parse(quantityText);
            _testLogger.WriteLine($"Cart quantity: {cartQuantity}");

            // Get price from this row
            var cartPriceText = await row.Locator("p.price.roboto-regular").InnerTextAsync();
            var cartPrice = ConvertToDecimal(cartPriceText);
            _testLogger.WriteLine($"Cart price: {cartPriceText} (${cartPrice})");

            // Find all matching products from our list (matching both model AND color)
            var matchingProducts = products.Where(p => 
                productName.Equals(p.Model, StringComparison.OrdinalIgnoreCase) &&
                cartColor.Equals(p.Color, StringComparison.OrdinalIgnoreCase)).ToList();

            if (matchingProducts.Any())
            {
                // Calculate expected total quantity and price for all matching products (same model + color)
                var expectedQuantity = matchingProducts.Sum(p => p.Quantity);
                var expectedTotalPrice = matchingProducts.Sum(p => p.TotalPrice);
                
                _testLogger.WriteLine($"Expected quantity: {expectedQuantity} (from {matchingProducts.Count} CSV entries with model='{productName}' and color='{cartColor}'), Cart quantity: {cartQuantity}");
                _testLogger.WriteLine($"Expected total: ${expectedTotalPrice}, Cart total: ${cartPrice}");

                Assert.AreEqual(expectedQuantity, cartQuantity, 
                    $"{matchingProducts.First().Category} quantity mismatch for {productName} ({cartColor}): expected {expectedQuantity}, got {cartQuantity}");
                Assert.AreEqual(expectedTotalPrice, cartPrice, 
                    $"{matchingProducts.First().Category} price mismatch for {productName} ({cartColor}): expected ${expectedTotalPrice}, got ${cartPrice}");
                _testLogger.WriteLine($"✅ {matchingProducts.First().Category} validation passed for {productName} ({cartColor}) - aggregated from {matchingProducts.Count} CSV entries");
            }
            else
            {
                _testLogger.WriteLine($"❌ Unexpected product found: {productName} with color {cartColor}");
                Assert.Fail($"Unexpected product in cart: {productName} with color {cartColor}");
            }
        }

        // Validate overall cart total price against the sum of individual product totals
        var cartTotalPriceText = await _page.Locator(CART_TOTAL_SELECTOR).InnerTextAsync();
        var cartTotalPrice = ConvertToDecimal(cartTotalPriceText);
        totalPrice = products.Sum(p => p.TotalPrice);
        _testLogger.WriteLine($"\n--- Overall Cart Total Validation ---");
        _testLogger.WriteLine($"Expected total: ${totalPrice}");
        _testLogger.WriteLine($"Cart total: {cartTotalPriceText} (${cartTotalPrice})");

        Assert.AreEqual(totalPrice, cartTotalPrice, "Overall cart total validation failed");
        _testLogger.WriteLine("✅ Overall cart total validation passed - Test completed successfully!");
    }

    #region Helper Methods

    /// <summary>
    /// Reads shopping items from CSV file and returns a list of products.
    /// </summary>
    /// <param name="csvFilePath">Path to the CSV file</param>
    /// <returns>List of products from the CSV file</returns>
    private List<Product> ReadShoppingItemsFromCsv(string csvFilePath)
    {
        var products = new List<Product>();
        
        try
        {
            _testLogger.WriteLine($"Reading shopping items from: {csvFilePath}");
            
            if (!File.Exists(csvFilePath))
            {
                throw new FileNotFoundException($"CSV file not found: {csvFilePath}");
            }

            var lines = File.ReadAllLines(csvFilePath);
            
            if (lines.Length < 2) // Must have header + at least one data row
            {
                throw new InvalidDataException("CSV file must contain header and at least one product row");
            }

            // Skip header line and process data rows
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue; // Skip empty lines
                
                var parts = line.Split(',');
                if (parts.Length < 4)
                {
                    _testLogger.WriteLine($"⚠️ Skipping invalid row {i}: {line}");
                    continue;
                }

                try
                {
                    var categoryString = parts[0].Trim();
                    var model = parts[1].Trim();
                    var quantityString = parts[2].Trim();
                    var colorString = parts[3].Trim();

                    // Parse category enum
                    if (!Enum.TryParse<ProductCategory>(categoryString, out var category))
                    {
                        _testLogger.WriteLine($"⚠️ Invalid category '{categoryString}' in row {i}, skipping");
                        continue;
                    }

                    // Parse quantity
                    if (!int.TryParse(quantityString, out var quantity) || quantity <= 0)
                    {
                        _testLogger.WriteLine($"⚠️ Invalid quantity '{quantityString}' in row {i}, skipping");
                        continue;
                    }

                    var product = new Product
                    {
                        Category = category,
                        Model = model,
                        Quantity = quantity,
                        Color = colorString.Trim(),
                        IndividualPrice = 0, // Will be populated when adding to cart
                        TotalPrice = 0 // Will be calculated when adding to cart
                    };

                    products.Add(product);
                    _testLogger.WriteLine($"  ✅ Loaded: {category} - {model} (Qty: {quantity})");
                }
                catch (Exception ex)
                {
                    _testLogger.WriteLine($"⚠️ Error parsing row {i}: {ex.Message}");
                }
            }

            _testLogger.WriteLine($"Successfully loaded {products.Count} products from CSV");
            return products;
        }
        catch (Exception ex)
        {
            _testLogger.WriteLine($"❌ Error reading CSV file: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the project root directory by navigating up from current directory until finding .csproj file.
    /// </summary>
    /// <returns>The absolute path to the project root directory</returns>
    private string GetProjectRootDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directoryInfo = new DirectoryInfo(currentDirectory);
        
        // Navigate up until we find the project root (contains .csproj file)
        while (directoryInfo != null && !directoryInfo.GetFiles("*.csproj").Any())
        {
            directoryInfo = directoryInfo.Parent;
        }
        
        return directoryInfo?.FullName ?? currentDirectory;
    }

    /// <summary>
    /// Converts a price string (e.g., "$1,234.56") to a decimal value.
    /// </summary>
    /// <param name="priceText">The price text to convert (including currency symbols and formatting)</param>
    /// <returns>The decimal value of the price</returns>
    /// <exception cref="FormatException">Thrown when the price text cannot be parsed</exception>
    private static decimal ConvertToDecimal(string priceText)
    {
        try
        {
            // Remove currency symbols, commas, and whitespace, then convert to decimal
            var cleanPriceText = priceText.Replace("$", "").Replace(",", "").Trim();
            return decimal.Parse(cleanPriceText, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (FormatException ex)
        {
            throw new FormatException($"Unable to parse price text '{priceText}'. Expected format: $X.XX or $X,XXX.XX", ex);
        }
    }

    /// <summary>
    /// Adds a product to the cart with specified parameters.
    /// </summary>
    /// <param name="page">The page instance</param>
    /// <param name="categoryName">The category link name</param>
    /// <param name="product">The product to add</param>
    /// <param name="colorTitle">The color to select</param>
    /// <returns>The updated product with price information</returns>
    private async Task<Product> AddProductToCartAsync(IPage page, string categoryName, Product product)
    {
        _testLogger.WriteLine($"  → Navigating to homepage for {product.Model}");
        await page.GotoAsync(TestConfiguration.Instance.BaseUrl);

        _testLogger.WriteLine($"  → Clicking category: {categoryName}");
        await page.GetByRole(AriaRole.Link, new() { Name = categoryName, Exact = true }).ClickAsync();
        
        _testLogger.WriteLine($"  → Selecting product: {product.Model}");
        await page.GetByText(product.Model).ClickAsync();
        
        _testLogger.WriteLine($"  → Selecting color: {product.Color}");
        await page.GetByTitle(product.Color).ClickAsync();

        var priceText = await page.Locator(PRICE_SELECTOR).InnerTextAsync();
        _testLogger.WriteLine($"  → Product price: {priceText}");
        
        _testLogger.WriteLine($"  → Setting quantity to: {product.Quantity}");
        await page.Locator(QUANTITY_INPUT_SELECTOR).FillAsync(product.Quantity.ToString());

        var updatedProduct = product;
        updatedProduct.IndividualPrice = ConvertToDecimal(priceText);
        updatedProduct.TotalPrice = updatedProduct.IndividualPrice * updatedProduct.Quantity;

        _testLogger.WriteLine($"  → Adding to cart...");
        await page.GetByRole(AriaRole.Button, new() { Name = "ADD TO CART" }).ClickAsync();

        _testLogger.WriteLine($"  ✅ Added {product.Model} - Qty: {product.Quantity}, Price: ${updatedProduct.IndividualPrice}, Total: ${updatedProduct.TotalPrice}");
        return updatedProduct;
    }

    #endregion
}

/// <summary>
/// Represents a product with its details for testing shopping cart functionality.
/// </summary>
public struct Product
{

    public ShoppingCartTests.ProductCategory Category { get; set; }

    /// <summary>
    /// The product model/name as displayed on the website.
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// The quantity of this product to add to cart.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The color of the product to select.
    /// </summary>
    public string Color { get; set; }

    /// <summary>
    /// The individual price of the product as retrieved from the website.
    /// </summary>
    public decimal IndividualPrice { get; set; }

    /// <summary>
    /// The calculated total price (IndividualPrice * Quantity).
    /// </summary>
    public decimal TotalPrice { get; set; }
}

public class TestLogger
{
    private readonly TestContext? _testContext;
    private readonly string _logFilePath;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the TestLogger class.
    /// </summary>
    /// <param name="testContext">Optional MSTest TestContext for logging integration</param>
    public TestLogger(TestContext? testContext = null)
    {
        _testContext = testContext;
        
        // Create Logs directory in the project root (not in bin folder)
        var projectRoot = GetProjectRootDirectory();
        var logsDirectory = Path.Combine(projectRoot, "Logs");
        Directory.CreateDirectory(logsDirectory);
        
        // Create log file with timestamp
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var testName = testContext?.TestName ?? "Unknown";
        var fileName = $"{testName}_{timestamp}.log";
        _logFilePath = Path.Combine(logsDirectory, fileName);
        
        // Write initial log entry
        WriteToFile($"=== Test Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        WriteToFile($"Test Name: {testName}");
        WriteToFile($"Log File: {_logFilePath}");
        WriteToFile("".PadRight(60, '='));
    }
    
    /// <summary>
    /// Gets the project root directory by looking for the .csproj file.
    /// </summary>
    /// <returns>The project root directory path</returns>
    private static string GetProjectRootDirectory()
    {
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var directory = new DirectoryInfo(currentDirectory);
        
        // Walk up the directory tree until we find a .csproj file
        while (directory != null && !directory.GetFiles("*.csproj").Any())
        {
            directory = directory.Parent;
        }
        
        // If we found a .csproj file, return that directory
        if (directory != null)
        {
            return directory.FullName;
        }
        
        // Fallback: try to find project root by looking for typical project files
        directory = new DirectoryInfo(currentDirectory);
        while (directory != null && 
               !directory.GetFiles("*.sln").Any() && 
               !directory.GetFiles("*.csproj").Any())
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? currentDirectory;
    }

    /// <summary>
    /// Writes a message to all available logging destinations (Console, TestContext, Debug, and File).
    /// Ensures test output is visible across different environments and test runners.
    /// </summary>
    /// <param name="message">The message to log</param>
    public void WriteLine(string message)
    {
        var timestampedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        
        Console.WriteLine(timestampedMessage);           // Always log to console
        _testContext?.WriteLine(timestampedMessage);     // Also log to MSTest if available
        Debug.WriteLine(timestampedMessage);             // Also log to debug output
        WriteToFile(timestampedMessage);                 // Also log to file
    }
    
    /// <summary>
    /// Writes a message directly to the log file with thread safety.
    /// </summary>
    /// <param name="message">The message to write to the file</param>
    private void WriteToFile(string message)
    {
        try
        {
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, message + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            // Log file writing failed, but don't break the test
            Debug.WriteLine($"Failed to write to log file: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Finalizes the log file with test completion information.
    /// </summary>
    public void FinalizeLog()
    {
        WriteToFile("".PadRight(60, '='));
        WriteToFile($"=== Test Log Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
    }
}
