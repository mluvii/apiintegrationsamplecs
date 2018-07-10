using System;

namespace mluvii.ApiIntegrationSample.Web.Payloads
{
    public class SessionEndedPayload
    {
        public long Id { get; set; }

        public string Channel { get; set; }

        public string Source { get; set; }

        public DateTimeOffset? Started { get; set; }

        public DateTimeOffset Ended { get; set; }
    }
}
