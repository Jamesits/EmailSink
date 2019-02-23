using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace EmailSink
{
    public class Entry
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
    }
    public static class Debug
    {
        [FunctionName("Debug")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequest req,
            [Table("Debug")]
            IAsyncCollector<Entry> tableBinding,
            ILogger log)
        {
            try
            {
                await tableBinding.AddAsync(new Entry()
                {
                    PartitionKey = "1111",
                    RowKey = "1111",
                });
                await tableBinding.FlushAsync();
            }
            catch (StorageException)
            {
                // we expect an Exception "The specified entity already exists"
                return new OkObjectResult("This passes test");
            }
            catch (Exception)
            {
                return new OkObjectResult("test");
            }

            return new OkObjectResult("This passes test too");
        }
    }
}
