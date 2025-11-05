using System.Text;
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

    public BonusSupporter(string webBrowserUrl, string baseUrl, string? ntfyTopic = null)
    {
        var options = new ChromeOptions();
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        _driver = new RemoteWebDriver(new Uri($"{webBrowserUrl}/wd/hub"), options.ToCapabilities());
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        _baseUrl = baseUrl;
        _ntfyTopic = ntfyTopic;
        _httpClient = new HttpClient();
    }

    private decimal GetBonusAccountValue()
    {
        var bonusElement = _wait.Until(d =>
            d.FindElement(By.XPath("//p[contains(text(), 'Ваш бонусный счёт:')]//span[@class='text-success-dark']")));

        // Extract text and clean it up
        var bonusText = bonusElement.Text.Trim();
        // Remove the ruble symbol and any extra whitespace, then parse to decimal
        var bonusValue = decimal.Parse(bonusText.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        return bonusValue;
    }

    private decimal GetPersonalAccountBalance()
    {
        var balanceElement = _wait.Until(d =>
            d.FindElement(By.Id("balanceCountUp")));

        var balanceText = balanceElement.Text.Trim();
        // Remove the ruble symbol and any extra whitespace, then parse to decimal
        var balanceValue = decimal.Parse(balanceText.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        return balanceValue;
    }

    // Modify NotifySuccessAsync to handle both values
    private async Task NotifySuccessAsync(decimal initialValue, decimal finalValue, decimal personalBalance)
    {
        var message = $"Bonus balance: {initialValue:F2} ₽ → {finalValue:F2} ₽\n" +
                     $"Personal account balance: {personalBalance:F2} ₽";
        Console.WriteLine(message);
        if (!string.IsNullOrEmpty(_ntfyTopic))
        {
            await SendNtfyNotificationAsync(message);
        }
    }

    public async Task ExecuteAsync(string login, string password)
    {
        try
        {
            await LoginAsync(login, password);
            await _driver.Navigate().GoToUrlAsync($"{_baseUrl}/bonus");
            
            // Get initial bonus value
            var initialBonusValue = GetBonusAccountValue();
            
            // Perform support action
            await SupportBonusAccountAsync();
            
            // Wait for page reload and get new values
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
        await _driver.Navigate().GoToUrlAsync($"{_baseUrl}");

        var usernameField = _wait.Until(d => d.FindElement(By.Name("login")));
        var passwordField = _wait.Until(d => d.FindElement(By.Name("password")));
        var loginButton = _wait.Until(d => d.FindElement(By.ClassName("btn-primary")));

        usernameField.SendKeys(login);
        passwordField.SendKeys(password);
        loginButton.Click();

        // Wait for successful login
        _wait.Until(d => d.Url.StartsWith(_baseUrl));
    }

    private async Task SupportBonusAccountAsync()
    {
        await _driver.Navigate().GoToUrlAsync($"{_baseUrl}/bonus");

        var supportButton = _wait.Until(d => d.FindElement(By.ClassName("btn-subtle-success")));
        supportButton.Click();
    }

    private async Task NotifySuccessAsync(string bonusValue)
    {
        Console.WriteLine($"Current bonus balance: {bonusValue} ₽");
        if (!string.IsNullOrEmpty(_ntfyTopic))
        {
            await SendNtfyNotificationAsync($"Bonus account value: {bonusValue} ₽");
        }
    }

    private async Task HandleErrorAsync(Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
        if (!string.IsNullOrEmpty(_ntfyTopic))
        {
            await SendNtfyNotificationAsync($"Error: {ex.Message}");
        }
    }

    private async Task SendNtfyNotificationAsync(string message)
    {
        var url = $"https://ntfy.vah-home.ru/{_ntfyTopic}";
        var content = new StringContent(message, Encoding.UTF8, "text/plain");
        await _httpClient.PostAsync(url, content);
    }

    public ValueTask DisposeAsync()
    {
        _driver.Quit();
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}