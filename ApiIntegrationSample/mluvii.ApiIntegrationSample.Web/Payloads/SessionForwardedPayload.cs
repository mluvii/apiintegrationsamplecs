using System;

namespace mluvii.ApiIntegrationSample.Web.Payloads
{
    public class SessionForwardedPayload
    {
        public long Id { get; set; }

        public string Channel { get; set; }

        public string Source { get; set; }

        public DateTimeOffset Time { get; set; }

        public int? UserId { get; set; }

        public int? OperatorGroupId { get; set; }

        public int? ChatbotId { get; set; }
    }
}
