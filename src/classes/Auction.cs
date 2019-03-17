using System;
using System.Timers;

namespace PriceGrabber
{
    public class Auction
    {
        public event EventHandler AuctionIsStartingEvent;

        public string Id { get; private set; }
        public DateTime StartTime { get; private set; }
        public string Name { get; private set; }
        public string Region { get; private set; }

        // Lane is based on Vehicle type and state 
        public string Lane { get; private set; }

        private Timer auctionStartTimer;
        public Auction(string auctionDetails)
        {
            // TODO:
            // parse the string into the various properties

            this.StartTime = DateTime.Parse("08:00 pm");
            TimeSpan diff = StartTime - DateTime.Now;
            double delay = diff.TotalMilliseconds;
            
            if(delay <=0)
            {
                // if the auction already started, set a fixed delay
                // for the event handlers to get setup
                delay = 10000;
            }

            // Create timer to go off when the auction starts
            auctionStartTimer = new Timer {
                AutoReset = false,
                Enabled = true,
                Interval = delay
            };

            auctionStartTimer.Elapsed += (object o, ElapsedEventArgs e) => this.AuctionIsStartingEvent?.Invoke(this, null);
        }
  }
}