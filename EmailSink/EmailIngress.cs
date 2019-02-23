using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace EmailSink
{
    public static class EmailIngress
    {
        [FunctionName("EmailIngress")]
        public static async System.Threading.Tasks.Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            [Table("Emails")]
            IAsyncCollector<Email> tableBinding,
            ILogger log
            )
        {
            try
            {
                string name = req.Query["name"];

                // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // parse header
                log.LogWarning("Headers");
                foreach (var kvpair in req.Headers)
                {
                    log.LogInformation($"{kvpair.Key} = {kvpair.Value}");
                }

                // parse body
                log.LogWarning("RequestBody");
                var stream = new StreamContent(req.Body);
                stream.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(req.Headers["Content-Type"]);
                var result = await stream.ReadAsMultipartAsync();

                foreach (var part in result.Contents)
                {
                    var key = part.Headers.ContentDisposition.Name;
                    var value = await part.ReadAsStringAsync();
                    log.LogInformation($"{key} = {value}");
                }

                return new OkObjectResult("Success");
                // dynamic data = JsonConvert.DeserializeObject(requestBody);
                // name = name ?? data?.name;

                // return name != null
                //     ? (ActionResult)new OkObjectResult($"Hello, {name}")
                //     : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            }
            catch (Exception ex)
            {
                Telemetry.Client.TrackException(ex);

                // tell Mailgun to not retry
                return new StatusCodeResult(406);
            }


        }
    }
}
