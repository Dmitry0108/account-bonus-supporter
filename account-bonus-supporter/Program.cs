using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

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

    Console.WriteLine("Successfully supported!!");
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred: " + ex.Message);
}
finally
{
    // Close the browser
    driver.Quit();
}