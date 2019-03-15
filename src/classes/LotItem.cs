using System;
using System.Text.RegularExpressions;
using OpenQA.Selenium;

namespace PriceGrabber
{
  public class LotItem
    {
      public string Year { get; private set; } = "?";
      public string MakeModel { get; private set; } = "?";
      public string LotNumber { get; private set; } = "?";
      
      public string Bid { get; set; } = "??";
      public LotItem(){ }
      
      public LotItem(IWebElement lotdesc)
      {
        try
        {
          // Lot Description Text Example:
          // 2018 Toyta Tundra TRD \n
          // Lot: 1234456 Item: 999
          
          // Split the first line from the second.
          // First line is year, make, and model
          // Second line is lot and item numbers
          string[] details = lotdesc.Text.Split("\n");

          // split the year from the make/model details
          // details[0] = 2018 Toyota Tundra TRD
          string[] ymm = details[0].Split(" ", 2);

          // details[1] = Lot: 1234456 Item: 999
          Regex r = new Regex(@"(?<lotNum>\d+)");
          Match m = r.Match(details[1]);
          string lot = m.Groups["lotNum"].Value;
          
          // Get the year without white space
          string year = ymm[0].Trim();
          
          // Get the make and model details without white space
          string makeModel = ymm[1].Trim();
          
          this.Year = year;
          this.MakeModel = makeModel;
          this.LotNumber = lot;
        }
        catch(Exception ex)
        {
          Console.WriteLine($"Error creating lot item. {ex}");
        }
      }
      
      public override string ToString()
      {
        return $"{this.Year} - {MakeModel} (Lot: {LotNumber})";
      }
      
      public override bool Equals(object obj)
      {
        return this.LotNumber == ((LotItem)obj).LotNumber;
      }
    }
  }