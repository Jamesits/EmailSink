using System;
using System.Net.Http;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.DataContracts;

namespace EmailSink
{
    public static class EmailIngress
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        [FunctionName("EmailIngress")]
        public static async System.Threading.Tasks.Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            [Table("Emails")]
            IAsyncCollector<Email> tableBinding,
            ExecutionContext context,
            ILogger log
            )
        {
            try
            {
                // parse header
                //log.LogWarning("Headers");
                //foreach (var kvpair in req.Headers)
                //{
                //    log.LogInformation($"{kvpair.Key} = {kvpair.Value}");
                //}

                // check if the request is coming from Mailgun
                if (!req.Headers["User-Agent"].ToString().StartsWith("mailgun"))
                {
                    return new BadRequestObjectResult("Unidentifiable content");
                }

                // parse body
                //log.LogWarning("RequestBody");
                var stream = new StreamContent(req.Body);
                stream.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(req.Headers["Content-Type"]);
                var result = await stream.ReadAsMultipartAsync();

                var email = new Email()
                {
                    PartitionKey = DateTime.UtcNow.Date.ToString("yyyyMMdd"),
                };
                var bodyPlain = "";
                var bodyHtml = "";

                foreach (var part in result.Contents)
                {
                    var key = part.Headers.ContentDisposition.Name;
                    var value = await part.ReadAsStringAsync();
                    //log.LogInformation($"{key} = {value}");

                    switch (key)
                    {
                        case "timestamp":
                            email.HookTime = UnixTimeStampToDateTime(int.Parse(value));
                            break;
                        case "Date":
                            email.ReceivedTime = value;
                            break;
                        case "from":
                            email.From = value;
                            break;
                        case "In-Reply-To":
                            email.InReplyTo = value;
                            break;
                        case "Message-Id":
                            email.RowKey = value;
                            break;
                        case "References":
                            email.References = value;
                            break;
                        case "sender":
                            email.Sender = value;
                            break;
                        case "subject":
                            email.Subject = value;
                            break;
                        case "recipient":
                            email.Recipient = value;
                            break;
                        case "user-agent":
                            email.UserAgent = value;
                            break;
                        case "attachment-count":
                            email.AttachmentCount = int.Parse(value);
                            break;
                        case "body-plain":
                            bodyPlain = value;
                            break;
                        case "body-html":
                            bodyHtml = value;
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(bodyHtml))
                {
                    email.Body = bodyPlain;
                    email.isHtml = false;
                }
                else
                {
                    email.Body = bodyHtml;
                    email.isHtml = true;
                }

                if (string.IsNullOrWhiteSpace(email.RowKey))
                {
                    log.LogWarning("Email Message-Id is empty");
                    email.RowKey = Guid.NewGuid().ToString();
                }

                // write to table
                using (var operation = Telemetry.Client.StartOperation<DependencyTelemetry>("WriteTable"))
                {
                    operation.Telemetry.Context.Operation.Id = context.InvocationId.ToString();

                    await tableBinding.AddAsync(email);
                }

                return new OkObjectResult("Success");
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                Telemetry.Client.TrackException(ex);

                // tell Mailgun to not retry
                return new StatusCodeResult(406);
            }


        }
    }
}
