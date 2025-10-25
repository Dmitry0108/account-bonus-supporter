var login = Environment.GetEnvironmentVariable("LOGIN");
var pass = Environment.GetEnvironmentVariable("PASSWORD");
var webBrowserUrl = Environment.GetEnvironmentVariable("WEB_BROWSER_URL");
var ntfyTopic = Environment.GetEnvironmentVariable("NTFY_TOPIC");
var baseUrl = Environment.GetEnvironmentVariable("BASE_URL");

if (string.IsNullOrEmpty(login))
    throw new ArgumentException("Login is empty");
if (string.IsNullOrEmpty(pass))
    throw new ArgumentException("Password is empty");
if (string.IsNullOrEmpty(webBrowserUrl))
    throw new ArgumentException("Browser url is empty");
if (string.IsNullOrEmpty(baseUrl))
    throw new ArgumentException("Base url is empty");

await using var supporter = new BonusSupporter(webBrowserUrl, baseUrl, ntfyTopic);
await supporter.ExecuteAsync(login, pass);