using System;

namespace PriceGrabber
{
    public class Auction
    {
        public DateTime StartTime { get; private set; }
        public string Name { get; private set; }
        public string Region { get; private set; }

        public Auction(string auctionDetails)
        {
            // TODO parse the string into the various properties
        }
    }
}