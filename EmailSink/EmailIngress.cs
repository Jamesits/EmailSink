using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Net;
using Microsoft.ApplicationInsights.DataContracts;
using System.Text;
using System.Web;

namespace EmailSink
{
    public static class EmailIngress
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private static ActionResult StaticActionResult(HttpStatusCode statusCode, string reason) => new ContentResult
        {
            StatusCode = (int)statusCode,
            Content = $"HTTP {(int)statusCode} {statusCode}: {reason}",
            ContentType = "text/plain",
        };

        private static void Fill(string key, string value, Email email)
        {
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
                case "List-Id":
                    email.ListId = value;
                    break;
                case "Reply-To":
                    email.ReplyTo = value;
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
                    email.BodyPlain = value;
                    break;
                case "body-html":
                    email.BodyHtml = value;
                    break;
                case "X-Mailgun-Variables":
                    email.MailgunVariables = value;
                    break;
                case "X-Mailgun-SFlag":
                    email.MailgunSFlag = value;
                    break;
                case "X-Mailgun-SScore":
                    email.MailgunSScore = value;
                    break;
            }
        }

        [FunctionName("EmailIngress")]
        public static async System.Threading.Tasks.Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            [Table("Emails")]
            CloudTable tableBinding,
            ExecutionContext context,
            ILogger log
            )
        {
            try
            {
                // parse header

                // check if the request is coming from Mailgun
                if (!req.Headers["User-Agent"].ToString().StartsWith("mailgun"))
                {
                    return new BadRequestObjectResult("Unidentifiable content");
                }

                var email = new Email()
                {
                    PartitionKey = DateTime.UtcNow.Date.ToString("yyyyMMdd"),
                };

                // parse body
                var stream = new StreamContent(req.Body);
                var contentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(req.Headers["Content-Type"]);
                stream.Headers.ContentType = contentType;
                if (contentType.ToString().StartsWith("multipart"))
                {
                    var result = await stream.ReadAsMultipartAsync();

                    foreach (var part in result.Contents)
                    {
                        var key = part.Headers.ContentDisposition.Name;
                        var value = await part.ReadAsStringAsync();
                        //log.LogInformation($"{key} = {value}");

                        Fill(key.Trim('\"'), value, email);
                    }
                }
                else if (contentType.ToString() == "application/x-www-form-urlencoded")
                {
                    var coll = HttpUtility.ParseQueryString(await stream.ReadAsStringAsync());
                    foreach (var key in coll.AllKeys)
                    {
                        var value = coll[key];
                        Fill(key.Trim('\"'), value, email);
                    }
                }
                else
                {
                    throw new ArgumentException($"Unknown content type {contentType}");
                }

                if (string.IsNullOrWhiteSpace(email.RowKey))
                {
                    log.LogWarning("Email Message-Id is empty");
                    email.RowKey = Guid.NewGuid().ToString();
                }

                // write to table
                try
                {
                    await tableBinding.ExecuteAsync(TableOperation.Insert(email));
                }
                catch (StorageException ex)
                {
                    // tell Mailgun not to retry
                    // this currently doesn't work and I don't know wtf is happening
                    return StaticActionResult(HttpStatusCode.NotAcceptable, $"Possible duplicate email: {ex.RequestInformation.ExtendedErrorInformation.ErrorMessage}");
                }
                log.LogInformation("End Operation");
                return new OkObjectResult("Success");
            }
            
            catch (Exception ex)
            {
                var t = new ExceptionTelemetry
                {
                    Exception = ex,
                };

                var sb = new StringBuilder();
                foreach (var kvpair in req.Headers)
                {
                    sb.Append($"{kvpair.Key} = {kvpair.Value}\n");
                }

                t.Properties["Header"] = sb.ToString();

                var stream = new StreamContent(req.Body);
                t.Properties["Body"] = await stream.ReadAsStringAsync();
                Telemetry.Client.TrackException(t);

                // tell Mailgun to retry
                return StaticActionResult(HttpStatusCode.InternalServerError, $"Unhandled exception: {ex}");
            }


        }
    }
}
