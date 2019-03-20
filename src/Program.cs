using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace PriceGrabber
{
    class Program
    {
        static List<PriceScraper> scrapers;
        static void Main(string[] args)
        {
            try
            {
                // Logger.ConvertCsvToDynamo(@"P:\Personal Projects\CopartPriceGrabber\Logs\43-B.csv");
                List<Task> tasks = new List<Task>();
                if(args.Count() > 0)
                {
                    scrapers = CreatePriceScrapers(args.ToList());
                    // Start each auction scraper
                    scrapers.ForEach(a => tasks.Add(a.Start()));
                }
                else
                {
                    List<Auction> auctions = GetTodaysAuctions();
                    // Subscribe to the Auction starting event
                    auctions.ForEach(a => a.AuctionIsStartingEvent += StartPriceGrabber);
                    // scrapers = CreatePriceScrapers(ids);
                }
                
                Task.WaitAll(tasks.ToArray());
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Fatal error. {ex}");
            }
        }

        private static void StartPriceGrabber(object sender, EventArgs e)
        {
            Auction auction = sender as Auction;
            PriceScraper s = new PriceScraper(auction.Id);
            scrapers.Add(s);
            s.Start();
            Console.WriteLine($"New auction started. {auction.Name} {auction.Id} {auction.StartTime}");
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
        
        private static List<Auction> GetTodaysAuctions()
        {
             // Create a webdriver
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.10240");
            IWebDriver driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
            driver.Navigate().GoToUrl("https://www.copart.com/todaysAuction/");

            // Wait until the page loads
            IWait<IWebDriver> wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30.00));
            wait.Until(d => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));

            // Get all the rows that define todays auctions
            var auctions = driver.FindElements(By.XPath("//*[@id='auctionLaterToday-datatable']/tbody/tr"));

            List<Auction> todaysAuctions = new List<Auction>();
            auctions.ToList().ForEach(a => {
                todaysAuctions.Add(new Auction(a.Text));
                Console.WriteLine(a.Text);
            });

            driver.Dispose();

            return todaysAuctions;
        }
    }
}
