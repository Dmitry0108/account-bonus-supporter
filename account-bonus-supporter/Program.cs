using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using System.Text;

var login = Environment.GetEnvironmentVariable("LOGIN");
if (string.IsNullOrEmpty(login))
{
    Console.WriteLine("Login is empty");
    return;
}
var pass = Environment.GetEnvironmentVariable("PASSWORD");
if (string.IsNullOrEmpty(pass))
{
    Console.WriteLine("Password is empty");
    return;
}
var webBrowserUrl = Environment.GetEnvironmentVariable("WEB_BROWSER_URL");
if (string.IsNullOrEmpty(webBrowserUrl))
{
    Console.WriteLine("Browser url is empty");
    return;
}

var ntfyTopic = Environment.GetEnvironmentVariable("NTFY_TOPIC");

// URL of the Chrome Docker container
var chromeOptions = new ChromeOptions();
chromeOptions.AddArgument("--no-sandbox");
chromeOptions.AddArgument("--disable-dev-shm-usage");

// Connect to the remote Chrome instance
var driver = new RemoteWebDriver(new Uri($"{webBrowserUrl}/wd/hub"), chromeOptions.ToCapabilities());

try
{
    // Step 1: Navigate to the login page
    driver.Navigate().GoToUrl("https://lk.ons24.ru");

    // Step 2: Enter login credentials
    IWebElement usernameField = driver.FindElement(By.Name("login"));
    IWebElement passwordField = driver.FindElement(By.Name("password"));
    IWebElement loginButton = driver.FindElement(By.ClassName("btn-primary"));

    usernameField.SendKeys(login);
    passwordField.SendKeys(pass);
    loginButton.Click();

    // Wait for login to complete
    Thread.Sleep(3000); // Replace with explicit wait for better reliability

    // Step 3: Navigate to another page
    driver.Navigate().GoToUrl("https://lk.ons24.ru/bonus");

    IWebElement supportBonusAccountButton = driver.FindElement(By.ClassName("btn-subtle-success"));
    supportBonusAccountButton.Click();

    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    IWebElement bonusValue = wait.Until(d =>
        d.FindElement(By.XPath("//*[contains(text(), 'Ваш бонусный счёт:')]/following::*[contains(@class, 'text-success-dark')][1]")));

    Console.WriteLine("Successfully supported!!");

    if (!string.IsNullOrEmpty(ntfyTopic))
        await SendNtfyNotificationAsync(ntfyTopic, $"Successfully supported bonus account! Bonus value: {bonusValue}");
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred: " + ex.Message);
    if (!string.IsNullOrEmpty(ntfyTopic))
        await SendNtfyNotificationAsync(ntfyTopic, $"Error: {ex.Message}");
}
finally
{
    // Close the browser
    driver.Quit();
}

// Helper method to send ntfy notification
async Task SendNtfyNotificationAsync(string topic, string message)
{
    using var client = new HttpClient();
    var url = $"https://ntfy.vah-home.ru/{topic}";
    var content = new StringContent(message, Encoding.UTF8, "text/plain");
    await client.PostAsync(url, content);
}