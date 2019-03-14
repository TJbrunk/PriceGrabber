using System;
using System.IO;
using System.Reflection;
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
            IWebDriver driver = null;
            try {
                // Create a webdriver
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.10240");
                driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
                // end
                
                // Go to the auction page
                driver.Navigate().GoToUrl("https://www.copart.com/auctionDashboard?auctionDetails=180-A-56028598");
                
                // Wait until the page loads
                IWait<IWebDriver> wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(30.00));
                wait.Until(driver1 => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
                
                // Set focus to the IFrame element on the page
                driver.SwitchTo().Frame(0);
                // Wait for the IFrame to load
                Thread.Sleep(5000);

                bool newLot = true;
                string lotNumber = "";
                string year = "";
                string makeModel = "";
                string bid = "";
                do
                {
                    if(newLot)
                    {
                        // Get the current lot number
                        (year, makeModel, lotNumber) = GetLotDetails(driver);
                        // Get the starting bid
                        bid = GetStartingBid(driver);
                        newLot = false;
                    }
                    
                    // Watch the Previous Bids window
                    string newBid = GetPreviousBid(driver);
                    bid = string.IsNullOrEmpty(newBid) ? bid : newBid;
                    Thread.Sleep(200);
                    
                    // Check if a new lot has started
                    if(GetLotNumber(driver) != lotNumber)
                    {
                        newLot = true;
                        Console.WriteLine($"Lot {lotNumber} high bid: {bid} \n");
                        bid = "??";
                    }
                    
                } while (true);
            
            }
            catch(Exception ex) {
                Console.WriteLine($"ERROR: {ex}");
            }
            finally{
                driver?.Dispose();
            }
        }
        
        
        // Returns Year, Make & Model, LotNumber
        private static (string, string, string) GetLotDetails(IWebDriver driver)
        {
            try
            {
                //*[@id="lotDesc-COPART078A"]
                string lotDescription = driver.FindElement(By.ClassName("lotdesc")).Text;
                string[] details = lotDescription.Split("\n");
                
                string lot = details[1].Split(" ", 2)[1];
                string[] ymm = details[0].Split(" ", 2);
                string year = ymm[0];
                string makeModel = ymm[1];

                return(year, makeModel, lot);
            }
            // IFrame might update while looking up the lot number
            catch (StaleElementReferenceException)
            {
                // Return an empty string, and that will trigger a new 
                return ("?", "?", "?");
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
                
                IWebElement bids = previousBids.FindElements(By.ClassName("prevBidStateName"));
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
                var bidder = driver.FindElement(By.ClassName("auctionrunningdiv-MACRO"));
                //*[@id="gridsterComp"]/gridster-item/widget/div/div/div/div/div/div[3]/section/section/bidding-area/bidding-dialer-area/div[2]/div/div/div[1]/bidding-dialer-refactor/svg/text[1]
                var x = bidder.FindElement(By.XPath("./*")).Text;
                return x.Split("\n", StringSplitOptions.RemoveEmptyEntries)[0];
            }
            catch(NoSuchElementException)
            {
                throw;
                // Might not be any previous bids yet if its a new lot
                return "???";
            }
        }
    }
}
