using System;

namespace EmailSink
{
    public class Email
    {
        // system-level key

        /// <summary>
        /// Automatically set to "yyyyMMdd", do not submit it in the request
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Unique primary key
        /// </summary>
        public string RowKey { get; set; }

        // Add your own keys below

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
        public bool isHtml { get; set; }
        public string Body { get; set; }
    }
}
