using FluentAssertions;
using Logfile.Core;
using Logfile.Structured.Elements;
using Logfile.Structured.Formatters;
using Logfile.Structured.Misc;
using Logfile.Structured.UnitTests.Elements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Logfile.Structured.UnitTests.Elements.EventTest;

namespace Logfile.Structured.UnitTests.Formatters
{
    class EventIDTest
    {
        [Test]
        public void ID_Should_BeEventID()
        {
            // Arrange
            // Act
            // Assert
            EventID.Identification.Should().Be("EventID");
        }

        [Test]
        public void FormatNull_ShouldThrow_ArgumentNullException()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => EventID.Default.Format(null));
        }

        [Test]
        public void FormatUnsupportedData_ShouldThrow_NotSupportedException()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<NotSupportedException>(() => EventID.Default.Format(123));
        }

        [Test]
        public void FormatEvent_Should_CombineNumbersAndNames()
        {
            // Arrange
            var detail = new Logfile.Core.Details.EventID(new[] { "a", "b" }, new[] { 1, 2 });

            // Act
            var s = EventID.Default.Format(detail);

            // Assert
            s.Should().Be(EventID.AsJson(detail).ToString(Formatting.None));
        }

        [Test]
        public void SerializeComplete_Should_IncludeAllEventArguments()
        {
            // Arrange
            var logEvent = new Logfile<StandardLoglevel>().New(StandardLoglevel.Warning);
            logEvent = Logfile.Core.Details.EventIDExtensions.Event(logEvent.Force.Developer, TestEvents.Sub.Event, 1, 2, 3);
            var logfileHierarchy = new Logfile.Core.Details.LogfileHierarchy(new[] { "top", "sub" });
            logEvent.Details.Add(logfileHierarchy);
            var evt = new Event<StandardLoglevel>(logEvent);

            // Act
            var s = evt.Serialize(EventTest.CreateConfiguration());

            var eventID = logEvent.Details.OfType<Logfile.Core.Details.EventID>().Single();
            JToken eventIDJson = null;
            if (eventID.StringArguments?.Any() == true)
                eventIDJson = EventID.AsJson(eventID);

            // Assert
            var expected = $"{Event.Identification}"
                + $"{Event.RecordSeparator} {logEvent.Time.ToIso8601String()}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{logEvent.Loglevel.ToString()}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{string.Join(".", logfileHierarchy.Hierarchy)}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}1 Event {{first=`1`, second=`2`, third=`3`}}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{Event.DeveloperFlag}"
                + $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{Event.QuotationMark}{Logfile.Structured.Formatters.EventID.Identification}{Event.QuotationMark}={Event.QuotationMark}{ContentEncoding.Encode(eventIDJson.ToString(Formatting.None), additionalCharactersToEscape: (byte)Event.QuotationMark)}{Event.QuotationMark}"
                + $"{Constants.NewLine}{Constants.EntitySeparator}";
            s.Should().Be(expected);
        }
    }
}
