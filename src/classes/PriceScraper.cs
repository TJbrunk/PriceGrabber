using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace PriceGrabber
{
    public class PriceScraper
    {
      
        IWebDriver webDriver;
        string auctionId;
        Logger logger;
        
        LotItem currentLot;
        Regex priceRegex = new Regex(@"\$(?<bid>[0-9]{0,3},*[0-9]{1,3})");
        
        System.Timers.Timer updateTimer = new System.Timers.Timer {
            AutoReset = true,
            Enabled = false,
            Interval = 200
        };
        
        public PriceScraper(string auctionId)
        {
            this.auctionId = auctionId;
            this.logger = new Logger(auctionId);
            this.updateTimer.Elapsed += UpdateTimer_Elapsed;
        }

        ~PriceScraper()
        {
            this.updateTimer.Dispose();
            this.webDriver.Dispose();
            this.logger.Dispose();
        }

        public void Start()
        {
            this.webDriver = this.CreateWebDriver();
            // Get all the info about the current lot
            this.currentLot = this.GetLotDetails();
            this.currentLot.Bid = this.GetBid();
            Console.Write($"First lot: {this.currentLot}");
            // Start the update timer to periodically check for new bids and new lots
            this.updateTimer.Enabled = true;
        }

        // Try's to find the LotDescription on the current page.
        // Returns NULL if details aren't found
        private LotItem GetLotDetails()
        {
            try
            {
                //*[@id="lotDesc-COPART078A"]
                IWebElement lotDescription = webDriver.FindElement(By.ClassName("lotdesc"));
                LotItem i = new LotItem(lotDescription);
                return i;
            }
            // IFrame might update while looking up the lot number
            catch (StaleElementReferenceException)
            {
                // Return an empty string, and that will trigger a new 
                return null;
            }
        }

        
        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {

                // Check if a new lot has started
                LotItem newLot = this.GetLotDetails();
                if(newLot != null && this.currentLot != newLot)
                {
                    // New lot, save the last lot details
                    this.logger.Log(this.currentLot);
                    
                    Console.WriteLine($"High bid: {currentLot.Bid}");
                    
                    newLot.Bid = GetBid();
                    
                    Console.Write($"New Lot: {newLot}");
                    // Update the current lot to the new lot
                    this.currentLot = newLot;
                }
                else
                {
                    // Same lot as last call, Check for a new bid
                    string newBid = this.GetBid();
                    currentLot.Bid = string.IsNullOrEmpty(newBid) ? currentLot.Bid : newBid;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
            }
        }

        // Checks for the last Previous Bid and returns it.
        // If no previous bid found, checks the running bid (Pre auction bid)
        private string GetBid()
        {
            try
            {
                IWebElement previousBids = 
                    webDriver.FindElement(By.ClassName("previous-bids-MACRO"));
                
                ReadOnlyCollection<IWebElement> bids = 
                    previousBids.FindElements(By.ClassName("prevBidStateName"));
                
                // If there are previous bids, get the most recent else return a '?'
                return bids != null ? 
                    bids[bids.Count - 1].Text.Trim() :
                    "?";
            }
            catch(NoSuchElementException)
            {
                // Might not be any previous bids yet if its a new lot
                try 
                {
                    IWebElement start = 
                    webDriver.FindElement(By.ClassName("auctionrunningdiv-MACRO"));
                    string bid = start.FindElement(By.XPath("./*")).Text;
                    
                    Match m = priceRegex.Match(bid);
                    return m.Success ? m.Groups["bid"].Value : "??";
                    
                }
                catch(NoSuchElementException)
                {
                    // Might not be any previous bids yet if its a new lot
                    return "???";
                }
            }
            catch(StaleElementReferenceException)
            {
                //IFrame may have updated
                return null;
            }
        }
        
        // Creates a new Chrome desktop web driver
        private IWebDriver CreateWebDriver()
        {
            // Create a webdriver
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.10240");
            return new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
        }
        
        // Goes to the Auction URL and waits for the page to be ready
        private void PrepareWebDriver()
        {
            // Go to the auction page
            webDriver.Navigate()
                .GoToUrl($"https://www.copart.com/auctionDashboard?auctionDetails={auctionId}");
            
            // Wait until the page loads
            IWait<IWebDriver> wait = new OpenQA.Selenium.Support.UI.WebDriverWait(webDriver, TimeSpan.FromSeconds(30.00));
            wait.Until(d => ((IJavaScriptExecutor)webDriver)
                                .ExecuteScript("return document.readyState")
                                .Equals("complete")
                        );
            
            // Set focus to the IFrame element on the page
            webDriver.SwitchTo().Frame(0);
            // Wait for the IFrame to load
            Thread.Sleep(5000);
        }
    }
}