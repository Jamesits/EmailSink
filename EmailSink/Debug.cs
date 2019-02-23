using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace EmailSink
{
    public class Entry: TableEntity
    {
    }
    public static class Debug
    {
        [FunctionName("Debug")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequest req,
            [Table("Debug")]
            CloudTable tableBinding,
            ILogger log)
        {
            try
            {
                var op = TableOperation.Insert(new Entry()
                {
                    PartitionKey = "1111",
                    RowKey = "1111",
                });
                await tableBinding.ExecuteAsync(op);
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
