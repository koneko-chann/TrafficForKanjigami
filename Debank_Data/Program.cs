using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using CaptchaSharp.Services;
using CaptchaSharp.Models;
using CaptchaSharp;
using SeleniumUndetectedChromeDriver;
using System.Collections.Generic;

class Program
{
    private static readonly string _apiKey = "";
    private CaptchaService captchaService;
    private CaptchaSharp.Models.Proxy captchaProxy;
    static public List<string> keyWords = new List<string> {"kanjigami pro","kanjigami login"};

    public Program()
    {
     
        captchaService = new TwoCaptchaService(_apiKey) 
        {
            Timeout = TimeSpan.FromMinutes(4)
        };
        captchaProxy = new CaptchaSharp.Models.Proxy();
    }

    static CaptchaSharp.Models.Proxy SetupProxy(string host, int port, string username, string password)
    {
        return new CaptchaSharp.Models.Proxy
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password
        };
    }

    static async Task Main(string[] args)
    {
        string proxyFilePath = "proxies.txt"; // Path to your proxy file
        string outputFilePath = "proxy_results.txt"; // Path to output file
        string testUrl = "http://www.google.com"; // URL to test the proxy

        if (File.Exists(proxyFilePath))
        {
            while (true)
            {

                var proxyList = File.ReadAllLines(proxyFilePath);
                ShuffleArray(proxyList);
                foreach (var proxy in proxyList)
                {
                    bool success = true;

                    for (int i = 0; i < 10 && success; i++) // Chạy mỗi proxy 10 lần
                    {
                        try
                        {
                            int randomNumber = new Random().Next(0, 2);
                            var result = await TestProxy(proxy, testUrl, randomNumber==0?0:1, i);
                            Console.WriteLine(result);
                            await File.AppendAllTextAsync(outputFilePath, result + Environment.NewLine);
                            success = result.Contains("is working.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An error occurred: {ex.Message}");
                            success = false;
                            break;
                        }

                        Thread.Sleep(3000);
                    }

                    if (!success)
                    {
                        Console.WriteLine($"Proxy {proxy} failed all attempts.");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Proxy file not found.");
        } 
    }

    static async Task<string> TestProxy(string proxyString, string testUrl, int randomNumber, int count)
    {
        var proxyParts = proxyString.Split(':');
        if (proxyParts.Length == 4)
        {
            string proxyAddress = proxyParts[0];
            int proxyPort = int.Parse(proxyParts[1]);
            string proxyUsername = proxyParts[2];
            string proxyPassword = proxyParts[3];

            var options = new ChromeOptions();
            options.AddArguments("start-maximized");
            options.AddArguments("disable-infobars");
            // options.AddArguments("--disable-extensions");
        //    options.AddArguments("--headless=new");
            options.AddArguments("--no-sandbox");
            options.AddArguments("--disable-dev-shm-usage");
            // options.AddArguments("--remote-debugging-port=9222");
            // options.AddArguments($"--proxy-server=http://{proxyAddress}:{proxyPort}");

            // Optional: Specify user data directory and profile directory if needed
            string userDataDir = @"C:\Users\ADMIN\AppData\Local\Google\Chrome\User Data";
            string profileDirectory = "Profile 2";
            options.AddArguments($"--user-data-dir={userDataDir}");
            options.AddArguments($"--profile-directory={profileDirectory}");

            new DriverManager().SetUpDriver(new ChromeConfig());

            try
            {
                using (var driver = UndetectedChromeDriver.Create(options, driverExecutablePath: await new ChromeDriverInstaller().Auto()))
                {
                    if (count == 0)
                    {
                        driver.Navigate().GoToUrl("chrome-extension://bnhejmehblohhaalgnnohedpijgkjeof/options.html");
                        await Task.Delay(5000);
                        driver.FindElement(By.XPath("/html/body/div/div/nav[2]/div/div/div[1]/p[2]/input")).SendKeys(proxyAddress);
                        driver.FindElement(By.XPath("/html/body/div/div/nav[2]/div/div/div[2]/p[2]/input")).Clear();
                        driver.FindElement(By.XPath("/html/body/div/div/nav[2]/div/div/div[2]/p[2]/input")).SendKeys(proxyPort.ToString());
                        driver.FindElement(By.XPath("/html/body/div/div/nav[2]/div/p/a")).Click();
                        await Task.Delay(5000);

                        if (driver.FindElements(By.XPath("/html/body/div/div/aside/ul/li[3]")).Count > 0)
                        {
                            var js1 = "document.querySelector(\"body > div > div > aside > ul > li:nth-child(1) > span\").click();document.querySelector(\"body > div > div > aside > ul > li:nth-child(2) > a\").click()";
                            IJavaScriptExecutor jsExe1 = (IJavaScriptExecutor)driver;
                            jsExe1.ExecuteScript(js1);
                            await Task.Delay(2000);
                        }

                        var js = "document.querySelector(\"body > div > div > aside > ul > li:nth-child(2) > span\").click()";
                        IJavaScriptExecutor jsExe = (IJavaScriptExecutor)driver;
                        jsExe.ExecuteScript(js);
                        await Task.Delay(2000);

                        driver.Navigate().GoToUrl("chrome-extension://ggmdpepbjljkkkdaklfihhngmmgmpggp/options.html");
                        driver.FindElement(By.Id("login")).Clear();
                        driver.FindElement(By.Id("login")).SendKeys(proxyUsername);
                        driver.FindElement(By.Id("password")).Clear();
                        driver.FindElement(By.Id("password")).SendKeys(proxyPassword);
                        driver.FindElement(By.Id("save")).Click();
                    }

                    driver.Navigate().GoToUrl(testUrl);
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(10);

                    bool isElementPresent = IsElementPresent(driver, By.Id("APjFqb"));
                    if (isElementPresent)
                    {
                        var chosenKeyword = keyWords[randomNumber];
                        driver.FindElement(By.Id("APjFqb")).SendKeys(chosenKeyword);
                        driver.FindElement(By.Id("APjFqb")).SendKeys(Keys.Enter);

                        await Task.Delay(10000);

                        var link = driver.FindElements(By.XPath("//a[contains(@href, 'kanjigami.pro')]")).FirstOrDefault();
                        if (link != null)
                        {
                            link.Click();
                            await Task.Delay(5000);
                        }
                        await Task.Delay(5000);

                        string pageTitle = driver.Title;
                        driver.Close();
                        driver.Quit();

                        return !string.IsNullOrEmpty(pageTitle) ? $"Proxy {proxyAddress}:{proxyPort} is working." : $"Proxy {proxyAddress}:{proxyPort} did not load the expected content.";
                    }
                    else
                    {
                        driver.Close();

                        driver.Quit();
                        return $"Proxy {proxyAddress}:{proxyPort} did not load the expected content.";
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Proxy {proxyAddress}:{proxyPort} encountered an error: {ex.Message}");
            }
        }
        else
        {
            throw new Exception($"Invalid proxy format: {proxyString}");
        }
    }

    static bool IsElementPresent(IWebDriver driver, By by)
    {
        return driver.FindElements(by).Count > 0;
    }

    public async Task<string> SolveCaptcha(string host, int port, string username, string password, string siteKey, string siteUrl)
    {
        CaptchaSharp.Models.Proxy proxy = SetupProxy(host, port, username, password);
        StringResponse solution = await captchaService.SolveRecaptchaV2Async(siteKey, siteUrl, proxy: proxy, invisible: false, cancellationToken: default);
        return solution.Response;
    }
    static void ShuffleArray<T>(T[] array)
    {
        Random rng = new Random();
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = array[k];
            array[k] = array[n];
            array[n] = value;
        }
    }
}
