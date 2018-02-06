namespace log4net.Appender.Splunk.Tests
{
    using System;
    using System.Reflection;
    using Core;
    using global::Splunk.Logging;
    using Newtonsoft.Json;
    using Repository.Hierarchy;
    using Xunit;

    public class CustomFormatterTest
    {
        private log4net.ILog _logger = null;

        public CustomFormatterTest()
        {
            // Step 1. Create repository object
            Hierarchy hierarchy = (Hierarchy) LogManager.GetRepository();

            // Step 2. Create appender
            var splunkHttpEventCollector = new SplunkHttpEventCollector();

            // Step 3. Set appender properties
            splunkHttpEventCollector.ServerUrl = "https://localhost:8088";
            splunkHttpEventCollector.Token = "ED9F5A37-BE9A-4782-B5F7-B6E31AC369CA";
            splunkHttpEventCollector.RetriesOnError = 0;

            log4net.Layout.PatternLayout patternLayout = new log4net.Layout.PatternLayout
            {
                ConversionPattern = "%message"
            };
            patternLayout.ActivateOptions();

            splunkHttpEventCollector.Layout = patternLayout;

            splunkHttpEventCollector.ActivateOptions(Formatter);

            // Step 4. Add appender to logger
            hierarchy.Root.AddAppender(splunkHttpEventCollector);
            hierarchy.Threshold = Level.All;
            hierarchy.Configured = true;

            // Step 5. Create logger
            _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public dynamic Formatter(HttpEventCollectorEventInfo info)
        {
            return CustomFormatter(info);
        }

        [Fact]
        public void TestCustomFormatter()
        {

            _logger.Debug($"{{\"date\":\"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffffffZ}\",\"threadId\":21,\"type\":\"LoadTest\",\"teststatus\":\"Fail\",\"action\":\"Test1\",\"ErrorMessage\":\"Failed!!\"}}");
        }

        // This is where the magic happens
        public CustomLogObject CustomFormatter(HttpEventCollectorEventInfo eventInfo)
        {
            var level = eventInfo.Event.GetType().GetProperty("Level").GetValue(eventInfo.Event, null);
            var renderedMessage = eventInfo.Event.GetType().GetProperty("RenderedMessage").GetValue(eventInfo.Event, null);
            var deserializedMessage = JsonConvert.DeserializeObject<RenderedMessage>(renderedMessage);

            var ret = new CustomLogObject
            {
                Level = level,
                RenderedMessage = deserializedMessage
            };
            return ret;
        }
    }

    public class CustomLogObject
    {
        public string Level { get; set; }
        public RenderedMessage RenderedMessage { get; set; }
    }

    public class RenderedMessage
    {
        public DateTime date { get; set; }
        public int threadId { get; set; }
        public string type { get; set; }
        public string teststatus { get; set; }
        public string action { get; set; }
    }
}