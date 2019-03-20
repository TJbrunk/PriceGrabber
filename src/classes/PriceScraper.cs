using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace PriceGrabber
{
    public class PriceScraper
    {
        string tab;
        readonly IWebDriver webDriver;
        string auctionId;
        Logger logger;
        
        LotItem currentLot;
        Regex priceRegex = new Regex(@"\$(?<bid>[0-9]{0,3},*[0-9]{1,3})");
        
        bool auctionRunning = true;
        private Task task;
        public PriceScraper(IWebDriver driver, string auctionId)
        {
            this.webDriver = driver;
            this.auctionId = auctionId;
            this.logger = new Logger(auctionId);
            this.task = new Task(() => ScraperAsync());
        }

        ~PriceScraper()
        {
            this.logger.Dispose();
        }

        public Task Start()
        {
            try
            {
                this.PrepareWebDriver();
                this.SetWebDriverFocus();
                // Get all the info about the current lot
                this.currentLot = this.GetLotDetails();
                if(currentLot != null)
                {
                    this.currentLot.Bid = this.GetBid();
                    Console.Write($"First lot: {this.currentLot}");
                    this.task.Start();
                    return this.task;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error in price scrapper. {ex}");
            }
            finally
            {
                lock (this.webDriver)
                {
                    this.SetWebDriverFocus();
                    this.webDriver.Close();
                    this.webDriver.SwitchTo().Window(this.webDriver.WindowHandles[0]);
                }
            }
            // return a completed task immediatly
            return Task.Run(() => {});
        }

        private void ScraperAsync()
        {
            while(this.auctionRunning)
            {
                Scrape();
                Thread.Sleep(500);
            }
            this.logger.Dispose();
        }

        // Try's to find the LotDescription on the current page.
        // Will stop Scaper task if 'sale-end' element is found
        // Returns NULL if details aren't found
        private LotItem GetLotDetails()
        {
            LotItem i;
            try
            {
                //*[@id="lotDesc-COPART078A"]
                IWebElement lotDescription = webDriver.FindElement(By.ClassName("lotdesc"));
                i = new LotItem(lotDescription);
            }
            // IFrame might update while looking up the lot number. Continue
            catch (StaleElementReferenceException)
            {
                i = null;
            }
            catch (NoSuchElementException)
            {
                try
                {
                    // Couldn't find the lot description.
                    // Check if the sale is over
                    IWebElement ended = webDriver.FindElement(By.ClassName("sale-end"));
                    i = null;
                    if(ended != null)
                        Console.WriteLine($"Auction {this.auctionId} Ended");
                        this.auctionRunning = false;
                }
                catch (System.Exception)
                {
                    i = null;
                    this.auctionRunning = false;
                }
            }
            return i;
        }

        private void Scrape()
        {
            lock (this.webDriver)
            {
                try
                {
                    this.SetWebDriverFocus();
                    // Check if a new lot has started
                    LotItem newLot = this.GetLotDetails();
                    if(newLot != null && this.currentLot.LotNumber != newLot.LotNumber)
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
                    this.auctionRunning = false;
                }
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
                return (bids != null && bids.Count > 0) ? 
                    bids[bids.Count - 1].Text.Trim() :
                    null;
            }
            catch(NoSuchElementException)
            {
                // Might not be any previous bids yet if its a new lot
                try 
                {
                    //*[@id="gridsterComp"]/gridster-item/widget/div/div/div/div/div/div[3]/section/section/bidding-area/bidding-dialer-area/div[2]/div/div/div[1]/bidding-dialer-refactor/svg/text[1]
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
        
        // Goes to the Auction URL and waits for the page to be ready
        private void PrepareWebDriver()
        {
            this.webDriver.SwitchTo().Window(this.webDriver.WindowHandles[0]);
            // create a new tab for this auction
            ((IJavaScriptExecutor) this.webDriver).ExecuteScript($"window.open('https://www.copart.com/auctionDashboard?auctionDetails={auctionId}','_blank');");
            
            // Switch to the new tab
            this.tab = webDriver.WindowHandles[webDriver.WindowHandles.Count - 1];
            webDriver.SwitchTo().Window(this.tab);
            
            // Go to the auction page
            // webDriver.Navigate()
            //     .GoToUrl($"https://www.copart.com/auctionDashboard?auctionDetails={auctionId}");
            
            // Wait until the page loads
            IWait<IWebDriver> wait = new OpenQA.Selenium.Support.UI.WebDriverWait(webDriver, TimeSpan.FromSeconds(30.00));
            wait.Until(d => ((IJavaScriptExecutor)webDriver)
                                .ExecuteScript("return document.readyState")
                                .Equals("complete")
                        );
            // Wait for the IFrame to load
            Thread.Sleep(5000);
        }

        private void SetWebDriverFocus()
        {
            try
            {
                // Switch to the current this auction tab
                webDriver.SwitchTo().Window(this.tab);
                // Set focus to the IFrame element on the page
                webDriver.SwitchTo().Frame(0);
            }
            catch(Exception ex)
            {
                ;
            }
        }
    }
}