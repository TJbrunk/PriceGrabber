using System;
using System.IO;
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
            string auction = args[0];
            IWebDriver driver = null;
            Logger logger = null;
            try {
                // GetTodaysAuctions();

                // Create a webdriver
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.10240");
                driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
                // end

                // Go to the auction page
                driver.Navigate().GoToUrl($"https://www.copart.com/auctionDashboard?auctionDetails={auction}");
                
                // Wait until the page loads
                IWait<IWebDriver> wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(30.00));
                wait.Until(driver1 => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
                
                // Set focus to the IFrame element on the page
                driver.SwitchTo().Frame(0);
                // Wait for the IFrame to load
                Thread.Sleep(5000);

                bool newLot = true;
                LotItem item = new LotItem();
                logger = new Logger(auction);
                do
                {
                    if(newLot)
                    {
                        // Get the current lot number
                        item = GetLotDetails(driver);
                        // Get the starting bid
                        item.Bid = GetStartingBid(driver);
                        newLot = false;
                    }
                    
                    // Watch the Previous Bids window
                    string newBid = GetPreviousBid(driver);
                    item.Bid = string.IsNullOrEmpty(newBid) ? item.Bid : newBid;
                    Thread.Sleep(200);
                    
                    // Check if a new lot has started
                    if(GetLotNumber(driver) != item.LotNumber)
                    {
                        logger.Log(item);
                        newLot = true;
                        Console.WriteLine($"Lot {item.LotNumber} high bid: {item.Bid} \n");
                    }
                    
                } while (true);
            
            }
            catch(Exception ex) {
                Console.WriteLine($"ERROR: {ex}");
            }
            finally{
                driver?.Dispose();
                logger?.Dispose();
            }
        }
        
        
        // Returns Year, Make & Model, LotNumber
        private static LotItem GetLotDetails(IWebDriver driver)
        {
            try
            {
                //*[@id="lotDesc-COPART078A"]
                IWebElement lotDescription = driver.FindElement(By.ClassName("lotdesc"));
                LotItem i = new LotItem(lotDescription);
                Console.Write(i);
                return i;
            }
            // IFrame might update while looking up the lot number
            catch (StaleElementReferenceException)
            {
                // Return an empty string, and that will trigger a new 
                return new LotItem();
            }
        }
        
        private static string GetLotNumber(IWebDriver driver)
        {
            IWebElement lotDescription = driver.FindElement(By.ClassName("lotdesc"));
            //*[@id="lotDesc-COPART078A"]/div[1]/div[2]/a
            return lotDescription.FindElement(By.XPath("./div[1]/div[2]/a")).Text;
        }
        
        private static string GetPreviousBid(IWebDriver driver)
        {
            string bid = null;
            try
            {
                IWebElement previousBids = driver.FindElement(By.ClassName("previous-bids-MACRO"));
                //*[@id="gridsterComp"]/gridster-item/widget/div/div[1]/div/div/div/div[3]/section/section/bidding-previous-bids
                string x = previousBids.Text;
                
                var bids = previousBids.FindElements(By.ClassName("prevBidStateName"));
                if(bids.Count == 4)
                {
                    bid = bids[3].Text;
                }
            }
            catch(NoSuchElementException)
            {
                // Might not be any previous bids yet if its a new lot
                return null;
            }
            catch(StaleElementReferenceException)
            {
                //IFrame may have updated
                return null;
            }
            return bid;
        }
        
        private static string GetStartingBid(IWebDriver driver)
        {
            try 
            {
                //*[@id="gridsterComp"]/gridster-item/widget/div/div/div/div/div/div[3]/section/section/bidding-area/bidding-dialer-area/div[2]/div/div/div[1]
                IWebElement bidder = driver.FindElement(By.ClassName("auctionrunningdiv-MACRO"));
                //*[@id="gridsterComp"]/gridster-item/widget/div/div/div/div/div/div[3]/section/section/bidding-area/bidding-dialer-area/div[2]/div/div/div[1]/bidding-dialer-refactor/svg/text[1]
                string x = bidder.FindElement(By.XPath("./*")).Text;
                if(x.Contains("Get"))
                {
                    return "??";
                }
                return x.Split("\n", StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            }
            catch(NoSuchElementException)
            {
                // Might not be any previous bids yet if its a new lot
                return "???";
            }
        }
    
        private static void GetTodaysAuctions()
        {
             // Create a webdriver
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.10240");
            IWebDriver driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
            driver.Navigate().GoToUrl("https://www.copart.com/salesListResult/");

            // Wait until the page loads
            IWait<IWebDriver> wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(30.00));
            wait.Until(driver1 => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));

            var rows = driver.FindElements(By.XPath("//*[@id='clientSideDataTable']/tbody/tr"));

            Regex reg = new Regex(@"\b\d{2}\/\d{2}\/\d{4}\b");
            foreach (IWebElement r in rows)
            {
                // Example rowText:
                //"08:00 AM MDT GA - Atlanta East SOUTH EAST 03/14/2019 03/18/2019 Future"
                Match m = reg.Match(r.Text);
                if(m.Success)
                {
                    string date = m.Value;
                    string time = r.Text.Substring(0, 8);
                    DateTime dt = DateTime.ParseExact($"{date} {time}", "MM/dd/yyyy hh:mm tt", null);
                    if(dt > DateTime.Now)
                    {
                        Console.WriteLine($"Next Auction: {dt}.");
                    }
                }
            }

            driver.Dispose();
        }
    }
}
