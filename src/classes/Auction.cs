using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using OpenQA.Selenium;

namespace PriceGrabber
{
    public class Auction
    {
        public string YardNum { get; private set; }
        public DateTime StartTime { get; private set; }
        // Lane is based on Vehicle type and state 
        public string Lane { get; private set; }// A, B, C etc
        
        public Auction(IWebElement a, DateTime yardStartTime)
        {
            // Starting XPath for element
            //*[@id="auctionLaterToday-datatable"]/tbody/tr[1]
            
            this.Lane = a.FindElement(By.ClassName("laneno")).Text;
            if(!(Lane.ToUpper().Equals("A") || Lane.ToUpper().Equals("B")))
            {
                // Lots C, and up are salvage lots.
                // Don't have the processing power to handle these, so ignore them
                return;
            }
            
            // Auction ID Link
            //*[@id="auctionLaterToday-datatable"]/tbody/tr[1]/td[9]/ul/li/a
            var auction = a.FindElement(By.XPath("./td[9]/ul/li/a")).GetAttribute("href");
            // Example:
            // auction = "https://www.copart.com/saleListResult/58/2019-03-18/A?location=AL%20-%20Mobile&saleDate=1552928400000&liveAuction=false&from=&yardNum=58"
            this.YardNum = new Regex(@"yardNum=(\d+)").Match(auction).Groups[1].Value;
            
            // Sale Time XPath
            //*[@id="auctionLaterToday-datatable"]/tbody/tr[1]/td[1]
            var start = a.FindElement(By.XPath("./td[1]")).Text;
            // Example:
            // startTime = "11:00 AM MDT"
            var startTime = new Regex(@"(\d{2}:\d{2} [A,P]M)").Match(start).Groups[1].Value;
            
            // If a start time isn't avaible for this table row, use the provided start time
            this.StartTime = string.IsNullOrEmpty(startTime) ? yardStartTime : DateTime.Parse(startTime);
        }
    }
}