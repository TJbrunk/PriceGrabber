using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PriceGrabber
{
  public class Logger : IDisposable
  {
    
    string fileName;
    StreamWriter writer;
    
    public Logger(string auction)
    {
      // Set the logging directory to the execution folder/logs
      string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      dir = Path.Combine(dir, "logs");
      
      // Create the directory if it doesn't exist
      Directory.CreateDirectory(dir);
      
      // Check the auction name for bad characters
      Path.GetInvalidFileNameChars()
        .ToList()
        .ForEach(c => auction = auction.Replace(c, '_'));
      
      // Check for duplicate file anems
      fileName = Path.Combine(dir, $"{auction}.csv");
      
      Console.WriteLine($"Creating log file {fileName} ");
      writer = new StreamWriter(fileName, true);
      writer.AutoFlush = true;
    }
    
    ~Logger()
    {
      this.Dispose();
    }

    public void Dispose()
    {
      // Flush the buffer to disk and close the stream
      writer?.Dispose();
    }

    #pragma warning disable 4014, 1998
    public async void Log(LotItem item)
      {
        // Remove $ and , before saving
        string bid = item.Bid;
        int index = bid.IndexOf('$');

        if(index >= 0)
          bid = bid.Remove(0, 1);
        index = bid.IndexOf(',');
        if(index >= 0)
          bid = bid.Remove(index, 1);
        
        string logItem = $"{item.LotNumber}, {item.Year}, {item.MakeModel}, {bid}";
        writer.WriteLineAsync(logItem);
      }
    #pragma warning restore 4014, 1998
  }
}