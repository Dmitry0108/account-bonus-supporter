using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

var login = Environment.GetEnvironmentVariable("login");
if (string.IsNullOrEmpty(login))
{
    Console.WriteLine("Login is empty");
    return;
}
var pass = Environment.GetEnvironmentVariable("password");
if (string.IsNullOrEmpty(pass))
{
    Console.WriteLine("Password is empty");
    return;
}


// Initialize ChromeDriver
IWebDriver driver = new ChromeDriver();

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