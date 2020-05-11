using FluentAssertions;
using Logfile.Core;
using Logfile.Core.Details;
using Logfile.Structured.Elements;
using Logfile.Structured.Formatters;
using Logfile.Structured.Misc;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Logfile.Structured.UnitTests.Elements
{
    class EventTest
    {
        public struct TestEvents
        {
            public enum Sub
            {
                [Parameters("first", "second", "third")]
                Event = 1,
            }
        }

        private static readonly IReadOnlyDictionary<Type, ILogEventDetailFormatter> DefaultLogEventDetailFormatters = new Dictionary<Type, ILogEventDetailFormatter>()
        {
            { typeof(Logfile.Core.Details.EventID), Logfile.Structured.Formatters.EventID.Default },
            { typeof(Logfile.Core.Details.Message), Logfile.Structured.Formatters.Message.Default },
        };

        static readonly IEnumerable<ITextWriter> DefaultStreamWriters;

        static EventTest()
        {
            var streamWriters = new[] { Mock.Of<ITextWriter>() };
            Mock.Get(streamWriters.Single())
                .Setup(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback(() => { });
            Mock.Get(streamWriters.Single())
                .Setup(m => m.Dispose())
                .Callback(() => { });
            DefaultStreamWriters = streamWriters;
        }

        internal static StructuredLogfileConfiguration<StandardLoglevel> CreateConfiguration(
            string appName = "app",
            bool writeToConsole = false,
            bool writeToDebugConsole = false,
            bool writeToDisk = true,
            string path = "./",
            string fileNameFormat = "logfile.log",
            int? maximumLogfileSize = 1024,
            int? keepLogfiles = 5,
            IReadOnlyDictionary<Type, ILogEventDetailFormatter> logEventDetailFormatters = null,
            bool makeLogEventDetailFormattersNull = false,
            ISensitiveSettings sensitiveSettings = null,
            IEnumerable<ITextWriter> additionalStreamWriters = null,
            bool makeAdditionalStreamWritersNull = false,
            bool isConsoleOutputBeautified = false)
        {
            return new StructuredLogfileConfiguration<StandardLoglevel>(
                appName,
                writeToConsole,
                writeToDebugConsole,
                writeToDisk,
                path,
                fileNameFormat,
                maximumLogfileSize,
                keepLogfiles,
                logEventDetailFormatters ?? (makeLogEventDetailFormattersNull ? null : DefaultLogEventDetailFormatters),
                sensitiveSettings,
                additionalStreamWriters ?? (makeAdditionalStreamWritersNull ? null : DefaultStreamWriters),
                isConsoleOutputBeautified);
        }

        [Test]
        public void ConstructorLogEventNull_ShouldThrow_ArgumentNullException()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => new Event<StandardLoglevel>(null));
        }

        [Test]
        public void Constructor_Should_SetProperties()
        {
            // Arrange
            var logEvent = new Logfile<StandardLoglevel>().New(StandardLoglevel.Warning);

            // Act
            var evt = new Event<StandardLoglevel>(logEvent);

            // Assert
            evt.LogEvent.Should().BeSameAs(logEvent);
        }

        [Test]
        public void SerializeComplete_Should_IncludeAllEventDetails()
        {
            // Arrange
            var logEvent = new Logfile<StandardLoglevel>().New(StandardLoglevel.Warning);
            logEvent = logEvent.Force.Developer.Event(TestEvents.Sub.Event, 1, "abc`def", "3").Msg("Multi-line\r\nmessage\r\nwith ` character to escape");
            var logfileHierarchy = new LogfileHierarchy(new[] { "to`p", "sub" });
            logEvent.Details.Add(logfileHierarchy);
            var evt = new Event<StandardLoglevel>(logEvent);

            // Act
            var s = evt.Serialize(CreateConfiguration());

            var eventID = logEvent.Details.OfType<Logfile.Core.Details.EventID>().Single();
            JToken eventIDJson = null;
            if (eventID.StringArguments?.Any() == true)
                eventIDJson = Logfile.Structured.Formatters.EventID.AsJson(eventID);

            // Assert
            var expected = $"{Event.Identification}"
                + $"{Event.RecordSeparator} {logEvent.Time.ToIso8601String()}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{logEvent.Loglevel.ToString()}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}to%60p.sub"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}1 Event {{first=`1`, second=`abc%60def`, third=`3`}}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{Event.DeveloperFlag}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{Event.QuotationMark}{Logfile.Structured.Formatters.EventID.Identification}{Event.QuotationMark}={Event.QuotationMark}{ContentEncoding.Encode(eventIDJson.ToString(Formatting.None), additionalCharactersToEscape: (byte)Event.QuotationMark)}{Event.QuotationMark}"
                + $"{Constants.NewLine}{Event.RecordSeparator}{Constants.Indent}{Event.QuotationMark}{Logfile.Structured.Formatters.Message.Identification}{Event.QuotationMark}={Event.QuotationMark}{ContentEncoding.Encode("Multi-line\r\nmessage\r\nwith ` character to escape", additionalCharactersToEscape: (byte)Event.QuotationMark)}{Event.QuotationMark}"
                + $"{Constants.NewLine}{Constants.EntitySeparator}";
            s.Should().Be(expected);
        }

        [Test]
        public void SerializeCompleteWithoutEventArguments_Should_IncludeNewLineAtTheEnd()
        {
            // Arrange
            var logEvent = new Logfile<StandardLoglevel>().New(StandardLoglevel.Warning);
            logEvent = logEvent.Force.Developer.Event(TestEvents.Sub.Event);
            var logfileHierarchy = new LogfileHierarchy(new[] { "to`p", "sub" });
            logEvent.Details.Add(logfileHierarchy);
            var evt = new Event<StandardLoglevel>(logEvent);

            // Act
            var s = evt.Serialize(CreateConfiguration());

            // Assert
            var expected = $"{Event.Identification}"
                + $"{Event.RecordSeparator} {logEvent.Time.ToIso8601String()}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{logEvent.Loglevel.ToString()}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}to%60p.sub"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}1 Event"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{Event.DeveloperFlag}"
                + $"{Constants.NewLine}{Constants.EntitySeparator}";
            s.Should().Be(expected);
        }
    }
}
