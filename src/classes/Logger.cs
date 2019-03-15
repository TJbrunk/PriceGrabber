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
      string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      
      dir = Path.Combine(dir, "logs");
      
      Path.GetInvalidFileNameChars()
        .ToList()
        .ForEach(c => auction = auction.Replace(c, '_'));
      
      fileName = Path.Combine(dir, $"{auction}.csv");
      int v = 1;
      do
      {
        fileName = Path.Combine(dir, $"{auction}_{v}.csv");
        v++;
      } while (File.Exists(fileName));
      
      File.Create(fileName);
      
      Console.WriteLine($"Creating log file {fileName} ");
      writer = new StreamWriter(fileName);
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
        string logItem = $"{item.LotNumber}, {item.Year}, {item.MakeModel}, {item.Bid}";
        writer.WriteLineAsync(logItem);
      }
    #pragma warning restore 4014, 1998
  }
}