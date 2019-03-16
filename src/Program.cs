using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace PriceGrabber
{
    class Program
    {
        static void Main(string[] args)
        {
            List<PriceScraper> scrapers;
            if(args.Count() > 0)
            {
                scrapers = CreatePriceScrapers(args.ToList());
            }
            else
            {
                List<string> ids = GetTodaysAuctions();
                scrapers = CreatePriceScrapers(ids);
            }
            
            // Start each auction scraper
            scrapers.ForEach(a => a.Start());
        }
        
        private static List<PriceScraper> CreatePriceScrapers(List<string> auctionIds)
        {
            // Create all the auction price scapers
            List<PriceScraper> auctions = new List<PriceScraper>();
            
            auctionIds.ForEach(a => {
                PriceScraper auction = new PriceScraper(a);
                auctions.Add(auction);
            });
            
            return auctions;
        }
        
        private static List<string> GetTodaysAuctions()
        {
             // Create a webdriver
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.10240");
            IWebDriver driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
            driver.Navigate().GoToUrl("https://www.copart.com/salesListResult/");

            // Wait until the page loads
            IWait<IWebDriver> wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30.00));
            wait.Until(d => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));

            var rows = driver.FindElements(By.XPath("//*[@id='clientSideDataTable']/tbody/tr"));

            // Date regex to match: 02/23/1999
            Regex reg = new Regex(@"\b\d{2}\/\d{2}\/\d{4}\b");
            foreach (IWebElement r in rows)
            {
                // Example rowText:
                //"08:00 AM MDT GA - Atlanta East SOUTH EAST 03/14/2019 03/18/2019 Future"
                Match m = reg.Match(r.Text);
                if(m.Success)
                {
                    // Time is always at the beginning, just rip it off
                    string time = r.Text.Substring(0, 8);
                    string date = m.Value;
                    DateTime dt = DateTime.ParseExact($"{date} {time}", "MM/dd/yyyy hh:mm tt", null);
                    if(dt > DateTime.Now)
                    {
                        Console.WriteLine($"Next Auction: {dt}.");
                    }
                }
            }

            driver.Dispose();

            return new List<string>();
        }
    }
}
