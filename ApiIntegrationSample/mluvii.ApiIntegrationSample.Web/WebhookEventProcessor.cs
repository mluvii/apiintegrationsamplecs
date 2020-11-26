using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using mluvii.ApiIntegrationSample.Web.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mluvii.ApiIntegrationSample.Web
{
    public class WebhookEventProcessor
    {
        private readonly MluviiClient mluviiClient;

        public WebhookEventProcessor(MluviiClient mluviiClient)
        {
            this.mluviiClient = mluviiClient;
        }

        public void ParseAndProcessEvent(string text)
        {
            ThreadPool.QueueUserWorkItem(ParseAndProcessWorkItem, text, true);
        }

        private void ParseAndProcessWorkItem(string text)
        {
            try
            {
                var payload = ParseWorkItem(text);
                if (payload == null)
                {
                    return;
                }

                ProcessPayload(payload);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error: " + ex);
            }
        }

        private object ParseWorkItem(string text)
        {
            using (var sre = new StringReader(text))
            using (var jre = new JsonTextReader(sre))
            {
                var jobj = JObject.Load(jre);
                var eventType = jobj["eventType"].Value<string>();
                var data = jobj["data"];

                switch (eventType)
                {
                    case "SessionStarted":
                        return data.ToObject<SessionStartedPayload>();
                    case "SessionForwarded":
                        return data.ToObject<SessionForwardedPayload>();
                    case "SessionEnded":
                        return data.ToObject<SessionEndedPayload>();
                    default:
                        return null;
                }
            }
        }

        private void ProcessPayload(object payload)
        {
            switch (payload)
            {
                case SessionStartedPayload sessionStarted:
                    Trace.WriteLine($@"Session {sessionStarted.Id} ({sessionStarted.Channel}) from ""{sessionStarted.Source}"" source has started at {sessionStarted.Started}");
                    break;
                case SessionForwardedPayload sessionForwarded:
                    Trace.WriteLine($"Session {sessionForwarded.Id} has been forwarded at {sessionForwarded.Time}");
                    break;
                case SessionEndedPayload sessionEnded:
                    Trace.WriteLine($"Session {sessionEnded.Id} has ended at {sessionEnded.Ended}");
                    Task.Run(ListClosedSessions);
                    break;
            }
        }

        private async Task ListClosedSessions()
        {
            var sessions = await mluviiClient.ListClosedSessions();
            // TODO: do something with the results
        }
    }
}
