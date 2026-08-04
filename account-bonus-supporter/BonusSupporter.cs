using System.Text;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

public class BonusSupporter : IAsyncDisposable
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string? _ntfyTopic;
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger<BonusSupporter> _logger;

    public BonusSupporter(string webBrowserUrl, string baseUrl, string? ntfyTopic = null, ILogger<BonusSupporter>? logger = null)
    {
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BonusSupporter>();
        
        var options = new ChromeOptions();
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        _driver = new RemoteWebDriver(new Uri($"{webBrowserUrl}/wd/hub"), options.ToCapabilities());
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        _baseUrl = baseUrl;
        _ntfyTopic = ntfyTopic;
        _httpClient = new HttpClient();
        
        _logger.LogInformation("BonusSupporter initialized with base URL: {BaseUrl}", baseUrl);
    }

    private decimal GetBonusAccountValue()
    {
        _logger.LogDebug("Attempting to retrieve bonus account value");
        var bonusElement = _wait.Until(d =>
            d.FindElement(By.XPath("//p[contains(text(), 'Ваш бонусный счёт:')]//span[@class='text-success-dark']")));

        // Extract text and clean it up
        var bonusText = bonusElement.Text.Trim();
        _logger.LogDebug("Raw bonus text: {BonusText}", bonusText);
        
        // Remove the ruble symbol and any extra whitespace, then parse to decimal
        var bonusValue = decimal.Parse(bonusText.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        _logger.LogInformation("Bonus account value retrieved: {BonusValue:F2} ₽", bonusValue);
        return bonusValue;
    }

    private decimal GetPersonalAccountBalance()
    {
        _logger.LogDebug("Attempting to retrieve personal account balance");
        var balanceElement = _wait.Until(d =>
            d.FindElement(By.Id("balanceCountUp")));

        var balanceText = balanceElement.Text.Trim();
        _logger.LogDebug("Raw balance text: {BalanceText}", balanceText);
        
        // Remove the ruble symbol and any extra whitespace, then parse to decimal
        var balanceValue = decimal.Parse(balanceText.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        _logger.LogInformation("Personal account balance retrieved: {BalanceValue:F2} ₽", balanceValue);
        return balanceValue;
    }

    // Modify NotifySuccessAsync to handle both values
    private async Task NotifySuccessAsync(decimal initialValue, decimal finalValue, decimal personalBalance)
    {
        var message = $"Bonus balance: {initialValue:F2} ₽ → {finalValue:F2} ₽\n" +
                     $"Personal account balance: {personalBalance:F2} ₽";
        _logger.LogInformation("Operation completed successfully. {Message}", message);
        
        if (!string.IsNullOrEmpty(_ntfyTopic))
        {
            await SendNtfyNotificationAsync(message);
        }
    }

    public async Task ExecuteAsync(string login, string password)
    {
        _logger.LogInformation("Starting execution for user: {Login}", login);
        
        try
        {
            await LoginAsync(login, password);
            await _driver.Navigate().GoToUrlAsync($"{_baseUrl}/bonus");
            
            // Get initial bonus value
            var initialBonusValue = GetBonusAccountValue();
            
            // Perform support action
            await SupportBonusAccountAsync();
            
            // Wait for page reload and get new values
            _logger.LogDebug("Waiting for page to reload after support action");
            _wait.Until(d => d.FindElement(By.XPath("//p[contains(text(), 'Ваш бонусный счёт:')]//span[@class='text-success-dark']")).Displayed);
            var finalBonusValue = GetBonusAccountValue();
            var personalBalance = GetPersonalAccountBalance();
            
            // Notify with all values
            await NotifySuccessAsync(initialBonusValue, finalBonusValue, personalBalance);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
            throw;
        }
    }

    private async Task LoginAsync(string login, string password)
    {
        _logger.LogInformation("Attempting login for user: {Login}", login);
        await _driver.Navigate().GoToUrlAsync($"{_baseUrl}");

        _logger.LogDebug("Waiting for login form elements");
        var usernameField = _wait.Until(d => d.FindElement(By.Name("login")));
        var passwordField = _wait.Until(d => d.FindElement(By.Name("password")));
        var loginButton = _wait.Until(d => d.FindElement(By.ClassName("btn-primary")));

        _logger.LogDebug("Filling login form");
        usernameField.SendKeys(login);
        passwordField.SendKeys(password);
        loginButton.Click();

        // Wait for successful login
        _wait.Until(d => d.Url.StartsWith(_baseUrl));
        _logger.LogInformation("Login successful for user: {Login}", login);
    }

    private async Task SupportBonusAccountAsync()
    {
        _logger.LogInformation("Performing bonus account support action");
        await _driver.Navigate().GoToUrlAsync($"{_baseUrl}/bonus");

        _logger.LogDebug("Waiting for support button");
        var supportButton = _wait.Until(d => d.FindElement(By.ClassName("btn-subtle-success")));
        supportButton.Click();
        _logger.LogInformation("Support button clicked");
    }

    private async Task HandleErrorAsync(Exception ex)
    {
        _logger.LogError(ex, "An error occurred during execution: {ErrorMessage}", ex.Message);
        
        if (!string.IsNullOrEmpty(_ntfyTopic))
        {
            await SendNtfyNotificationAsync($"Error: {ex.Message}");
        }
    }

    private async Task SendNtfyNotificationAsync(string message)
    {
        _logger.LogDebug("Sending notification to ntfy topic: {Topic}", _ntfyTopic);
        var url = $"https://ntfy.vah-home.ru/{_ntfyTopic}";
        var content = new StringContent(message, Encoding.UTF8, "text/plain");
        await _httpClient.PostAsync(url, content);
        _logger.LogInformation("Notification sent successfully");
    }

    public ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing BonusSupporter resources");
        _driver.Quit();
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}