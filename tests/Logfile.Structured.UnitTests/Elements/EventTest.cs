using FluentAssertions;
using Logfile.Core;
using Logfile.Core.Details;
using Logfile.Structured.Elements;
using Logfile.Structured.Formatters;
using Logfile.Structured.Misc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Logfile.Structured.UnitTests.Elements
{
	class EventTest
	{
		struct TestEvents
		{
			public enum Sub
			{
				[Parameters("first", "second", "third")]
				Event = 1,
			}
		}

		private static readonly IReadOnlyDictionary<Type, ILogEventDetailFormatter> DefaultLogEventDetailFormatters = new Dictionary<Type, ILogEventDetailFormatter>()
		{
			{ typeof(Logfile.Core.Details.Message), Logfile.Structured.Formatters.Message.Default },
		};

		static readonly IEnumerable<IStreamWriter> DefaultStreamWriters;

		static EventTest()
		{
			var streamWriters = new[] { Mock.Of<IStreamWriter>() };
			Mock.Get(streamWriters.Single())
				.Setup(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Callback(() => { });
			Mock.Get(streamWriters.Single())
				.Setup(m => m.Dispose())
				.Callback(() => { });
			DefaultStreamWriters = streamWriters;
		}

		static Configuration<StandardLoglevel> createConfiguration(
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
			IEnumerable<IStreamWriter> additionalStreamWriters = null,
			bool makeAdditionalStreamWritersNull = false)
		{
			return new Configuration<StandardLoglevel>(
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
				additionalStreamWriters ?? (makeAdditionalStreamWritersNull ? null : DefaultStreamWriters));
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
			logEvent = logEvent.Force.Developer.Event(TestEvents.Sub.Event, 1, 2, 3).Msg("Multi-line\r\nmessage");
			var logfileHierarchy = new LogfileHierarchy(new[] { "top", "sub" });
			logEvent.Details.Add(logfileHierarchy);
			var evt = new Event<StandardLoglevel>(logEvent);

			// Act
			var s = evt.Serialize(createConfiguration());

			// Assert
			var expected = $"{Constants.EntitySeparator}{Event.Identification}"
				+ $"{Event.RecordSeparator} {logEvent.Time.ToIso8601String()}"
				+ $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{logEvent.Loglevel.ToString()}"
				+ $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{string.Join(".", logfileHierarchy.Hierarchy)}"
				+ $"{Event.RecordSeparator}{Event.VisualRecordSeparator}1 Event"
				+ $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{Event.DeveloperFlag}"
				+ $"{Event.RecordSeparator}{Event.VisualRecordSeparator}{Event.QuotationSign}{Logfile.Structured.Formatters.Message.Identification}{Event.QuotationSign}={Event.QuotationSign}{ContentEncoding.Encode("Multi-line\r\nmessage")}{Event.QuotationSign}"
				+ $"{Constants.NewLine}";
			s.Should().Be(expected);
		}
	}
}
