using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace EmailSink
{
    public class Email : TableEntity
    {
        public string ReceivedTime { get; set; }
        public DateTime? HookTime { get; set; }
        public string From { get; set; }
        public string InReplyTo { get; set; }
        public string References { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public string UserAgent { get; set; }
        public int AttachmentCount { get; set; }
        public string Recipient { get; set; }
        public string StrippedText { get; set; }
        public string StrippedSignature { get; set; }
        public string BodyPlain { get; set; }
        public string BodyHtml { get; set; }
        public string MailgunVariables { get; set; }
    }
}
