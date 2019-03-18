using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;

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
        int index = -1;
        string bid = "??";
        // Remove $ and , before saving
        if(item.Bid != null)
        {
          bid = item.Bid;
          index = bid.IndexOf('$');
          if(index >= 0)
            bid = bid.Remove(0, 1);

          index = bid.IndexOf(',');
          if(index >= 0)
            bid = bid.Remove(index, 1);
        }
        
        string logItem = $"{item.LotNumber}, {item.Year}, {item.MakeModel}, {bid}";
        await writer.WriteLineAsync(logItem);
      }
    #pragma warning restore 4014, 1998
    
    public static void ConvertCsvToDynamo(string path)
    {
      StreamReader reader = null;
      try 
      {
        AmazonDynamoDBClient client = Logger.createClient();
        Table priceTable = Table.LoadTable(client, "LotPrices");
        reader = new StreamReader(path, true);
        do
        {
          string item = reader.ReadLine();
          LotItem i = new LotItem(item);
          
          Document newItemDocument = new Document();
          newItemDocument["Id"] = i.LotNumber;
          newItemDocument["Year"] = i.Year;
          newItemDocument["Info"] = i.MakeModel;
          newItemDocument["Price"] = i.Bid;

          priceTable.PutItemAsync(newItemDocument).Wait();
          
        } while (reader.Peek() > 0);
        reader?.Close();
      }
      catch
      {
        reader?.Close();
      }
    }
    
    private static AmazonDynamoDBClient createClient( )
    {
        var credentials = new BasicAWSCredentials("<ID>", "<PRIVATE KEY>");
        try 
        {
            return new AmazonDynamoDBClient(credentials, RegionEndpoint.USWest2 ); 
        }
        catch( Exception ex )
        {
            Console.WriteLine( " FAILED to create a DynamoDB client; " + ex.Message );
            return null;
        }
    }
  }
}