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
using System.Text;
using System.Threading;

namespace Logfile.Structured.UnitTests.Elements
{
	static class HeaderTest
	{
		static readonly IReadOnlyDictionary<Type, ILogEventDetailFormatter> DefaultLogEventDetailFormatters = new Dictionary<Type, ILogEventDetailFormatter>()
		{
			{ typeof(Logfile.Core.Details.Message), Logfile.Structured.Formatters.Message.Default },
		};

		static readonly IEnumerable<IStreamWriter> DefaultStreamWriters;

		static HeaderTest()
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

		static StructuredLogfileConfiguration<StandardLoglevel> createConfiguration(
			string appName = "app",
			bool writeToConsole = false,
			bool writeToDebugConsole = false,
			bool writeToDisk = false,
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
				false);
		}

		static Header<StandardLoglevel> createHeader(
			string appName = "TestApp",
			DateTime? appStartUpTime = null,
			int appInstanceSequenceNumber = 1,
			Dictionary<string, string> miscellaneous = null,
			bool makeMiscellaneousNull = false)
		{
			return new Header<StandardLoglevel>(
				appName,
				appStartUpTime ?? DateTime.Now,
				appInstanceSequenceNumber,
				miscellaneous ?? (makeMiscellaneousNull ? null : new Dictionary<string, string>()));
		}

		public class Constructors
		{
			[Test]
			public void ConstructorWithAppNameNull_ShouldThrow_ArgumentNullException()
			{
				Assert.Throws<ArgumentNullException>(() => createHeader(appName: null));
			}

			[Test]
			public void ConstructorWithMiscellaneousInformationNull_ShouldThrow_ArgumentNullException()
			{
				Assert.Throws<ArgumentNullException>(() => createHeader(miscellaneous: null, makeMiscellaneousNull: true));
			}

			[Test]
			public void Constructor_Should_SetProperties()
			{
				var time = DateTime.Now;
				var instanceID = Guid.NewGuid();
				var obj = createHeader(
					appName: "TestApp",
					appStartUpTime: time,
					appInstanceSequenceNumber: 1,
					miscellaneous: new Dictionary<string, string>());
				obj.AppName.Should().Be("TestApp");
				obj.AppStartUpTime.Should().Be(time);
				obj.AppInstanceLogfileSequenceNumber.Should().Be(1);
				obj.Miscellaneous.Should().BeEmpty();
			}
		}

		public class Serialization
		{
			[Test]
			public void SerializeWithSimpleValues_Should_ProduceValidOutput()
			{
				var configuration = createConfiguration();
				var time = DateTime.Now;
				var miscellaneous = new Dictionary<string, string>
				{
					{ "key", "value" },
				};

				var serialized = createHeader(appStartUpTime: time, miscellaneous: miscellaneous).Serialize(configuration);

				var expected = $"{Header<StandardLoglevel>.LogfileIdentity}"
					+ $"{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{Header<StandardLoglevel>.AppNameRecord}={Constants.QuotationMark}TestApp{Constants.QuotationMark}"
					+ $"{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{Header<StandardLoglevel>.AppStartUpTimeRecord}={Constants.QuotationMark}{time.ToIso8601String()}{Constants.QuotationMark}"
					+ $"{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{Header<StandardLoglevel>.AppInstanceLogfileSequenceNumberRecord}=1"
					+ $"{Constants.NewLine}{Constants.RecordSeparator}{Constants.Indent}{Constants.QuotationMark}key{Constants.QuotationMark}={Constants.QuotationMark}value{Constants.QuotationMark}"
					+ $"{Constants.EntitySeparator}";

				serialized.Should().Be(expected);
			}

			[Test]
			public void SerializeWithValuesToEscape_Should_ProduceEscapedOutput()
			{
				var configuration = createConfiguration();
				var time = DateTime.Now;
				var miscellaneous = new Dictionary<string, string>
				{
					{ "key", "value" },
					{ "key%text", @"value`text" },
				};

				var serialized = createHeader(appStartUpTime: time, miscellaneous: miscellaneous).Serialize(configuration);

				var expected = $"{Header<StandardLoglevel>.LogfileIdentity}"
					+ $"{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{Header<StandardLoglevel>.AppNameRecord}={Constants.QuotationMark}TestApp{Constants.QuotationMark}"
					+ $"{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{Header<StandardLoglevel>.AppStartUpTimeRecord}={Constants.QuotationMark}{time.ToIso8601String()}{Constants.QuotationMark}"
					+ $"{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{Header<StandardLoglevel>.AppInstanceLogfileSequenceNumberRecord}=1"
					+ $"{Constants.NewLine}{Constants.RecordSeparator}{Constants.Indent}{Constants.QuotationMark}key{Constants.QuotationMark}={Constants.QuotationMark}value{Constants.QuotationMark}"
					+ $"{Constants.NewLine}{Constants.RecordSeparator}{Constants.Indent}{Constants.QuotationMark}key%25text{Constants.QuotationMark}={Constants.QuotationMark}value%60text{Constants.QuotationMark}"
					+ $"{Constants.EntitySeparator}";

				serialized.Should().Be(expected);
			}

			[Test]
			public void SerializeWithLineBreaksInValues_Should_ProduceMultiLineOutput()
			{
				var configuration = createConfiguration();
				var time = DateTime.Now;
				var miscellaneous = new Dictionary<string, string>
			{
				{ "key", "value" },
				{ "key text", "value\nmulti-line\ntext" },
			};

				var serialized = createHeader(appStartUpTime: time, miscellaneous: miscellaneous).Serialize(configuration);

				var expected = $"{Header<StandardLoglevel>.LogfileIdentity}"
					+ $"{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{Header<StandardLoglevel>.AppNameRecord}={Constants.QuotationMark}TestApp{Constants.QuotationMark}"
					+ $"{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{Header<StandardLoglevel>.AppStartUpTimeRecord}={Constants.QuotationMark}{time.ToIso8601String()}{Constants.QuotationMark}"
					+ $"{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{Header<StandardLoglevel>.AppInstanceLogfileSequenceNumberRecord}=1"
					+ $"{Constants.NewLine}{Constants.RecordSeparator}{Constants.Indent}{Constants.QuotationMark}key{Constants.QuotationMark}={Constants.QuotationMark}value{Constants.QuotationMark}"
					+ $"{Constants.NewLine}{Constants.RecordSeparator}{Constants.Indent}{Constants.QuotationMark}key text{Constants.QuotationMark}={Constants.QuotationMark}value"
					+ $"\nmulti-line\ntext{Constants.QuotationMark}"
					+ $"{Constants.EntitySeparator}";

				serialized.Should().Be(expected);
			}
		}

		public class Parsing
		{
			[Test]
			public void ParseWithSimpleValues_Should_ProduceValidOutput()
			{
				var configuration = createConfiguration();
				var time = DateTime.Now;
				var miscellaneous = new Dictionary<string, string>
				{
					{ "key", "value" },
				};

				var header = createHeader(appStartUpTime: time, miscellaneous: miscellaneous);
				var serialized = header.Serialize(configuration);
				var data = Encoding.UTF8.GetBytes(serialized);
				var parsed = Header<StandardLoglevel>.Parse(
					data: data,
					encoding: Encoding.UTF8,
					timeZone: TimeZoneInfo.Local);

				parsed.MoreDataRequired.Should().BeFalse();
				parsed.ConsumedData.Should().Be(data.Length);
				var parsedHeader = (Header<StandardLoglevel>)parsed.Element;
				parsedHeader.AppName.Should().Be(header.AppName);
				parsedHeader.AppStartUpTime.Should().Be(time);
				parsedHeader.AppInstanceLogfileSequenceNumber.Should().Be(1);
				parsedHeader.Miscellaneous.Single().Key.Should().Be("key");
				parsedHeader.Miscellaneous.Single().Value.Should().Be("value");
			}

			// With undefined time zone
			// Key without quotation signs
			// Value without quotation signs

			//public void ParseWithMiscellaneousFlags_Should_ProduceValidOutput()

			[Test]
			public void ParseWithMiscellaneousKeyValueQuotationStyles_Should_ProduceValidOutput()
			{
				var configuration = createConfiguration();
				var time = DateTime.Now;

				var header = createHeader(appStartUpTime: time);
				var serialized = header.Serialize(configuration);
				var tempSerialized = serialized.Remove(serialized.Length - 1);
				tempSerialized = tempSerialized + $"{Constants.RecordSeparator}`key`=`value`{Constants.RecordSeparator}key2=`value2`{Constants.RecordSeparator}`key3`=value3{Constants.RecordSeparator}   key4  =  value4  {Constants.RecordSeparator}`key5`  =  `value5`" + Constants.EntitySeparator;
				var data = Encoding.UTF8.GetBytes(tempSerialized);
				var parsed = Header<StandardLoglevel>.Parse(
					data: data,
					encoding: Encoding.UTF8,
					timeZone: TimeZoneInfo.Local);

				parsed.MoreDataRequired.Should().BeFalse();
				parsed.ConsumedData.Should().Be(data.Length);
				var parsedHeader = (Header<StandardLoglevel>)parsed.Element;
				parsedHeader.AppName.Should().Be(header.AppName);
				parsedHeader.AppStartUpTime.Should().Be(time);
				parsedHeader.AppInstanceLogfileSequenceNumber.Should().Be(1);
				parsedHeader.Miscellaneous.Count.Should().Be(5);
				parsedHeader.Miscellaneous.ElementAt(0).Key.Should().Be("key");
				parsedHeader.Miscellaneous.ElementAt(0).Value.Should().Be("value");
				parsedHeader.Miscellaneous.ElementAt(1).Key.Should().Be("key2");
				parsedHeader.Miscellaneous.ElementAt(1).Value.Should().Be("value2");
				parsedHeader.Miscellaneous.ElementAt(2).Key.Should().Be("key3");
				parsedHeader.Miscellaneous.ElementAt(2).Value.Should().Be("value3");
				parsedHeader.Miscellaneous.ElementAt(3).Key.Should().Be("key4");
				parsedHeader.Miscellaneous.ElementAt(3).Value.Should().Be("value4");
				parsedHeader.Miscellaneous.ElementAt(4).Key.Should().Be("key5");
				parsedHeader.Miscellaneous.ElementAt(4).Value.Should().Be("value5");
			}

			[Test]
			public void InvalidHeaderIdentity_ShouldThrow_FormatException()
			{
				// Arrange
				var configuration = createConfiguration();
				var time = DateTime.Now;

				var header = createHeader();
				var serialized = header.Serialize(configuration);
				serialized = 'X' + serialized.Substring(1);
				var data = Encoding.UTF8.GetBytes(serialized);

				// Act & Assert
				Assert.Throws<FormatException>(
					() => Header<StandardLoglevel>.Parse(
						data: data,
						encoding: Encoding.UTF8,
						timeZone: TimeZoneInfo.Local));
			}

			[Test]
			public void EmptyAppName_Should_Ignore()
			{
				// Arrange
				var configuration = createConfiguration();

				var header = createHeader(appName: "");
				var serialized = header.Serialize(configuration);
				var data = Encoding.UTF8.GetBytes(serialized);

				// Act
				var parsed = Header<StandardLoglevel>.Parse(
					data: data,
					encoding: Encoding.UTF8,
					timeZone: TimeZoneInfo.Local);

				// Assert
				parsed.MoreDataRequired.Should().BeFalse();
				parsed.ConsumedData.Should().Be(Encoding.UTF8.GetBytes(serialized.TrimEnd('\n')).Length);
				var parsedHeader = (Header<StandardLoglevel>)parsed.Element;
				parsedHeader.AppName.Should().Be(header.AppName);
				parsedHeader.AppStartUpTime.Should().Be(header.AppStartUpTime);
				parsedHeader.AppInstanceLogfileSequenceNumber.Should().Be(header.AppInstanceLogfileSequenceNumber);
				parsedHeader.Miscellaneous.Should().BeEmpty();
			}

			[Test]
			public void MissingAppName_ShouldThrow_FormatException()
			{
				// Arrange
				var configuration = createConfiguration();

				var header = createHeader();
				var serialized = header.Serialize(configuration).Replace($"app={Constants.QuotationMark}{header.AppName}{Constants.QuotationMark}", "");
				var data = Encoding.UTF8.GetBytes(serialized);

				// Act & Assert
				Assert.Throws<FormatException>(
					() => Header<StandardLoglevel>.Parse(
						data: data,
						encoding: Encoding.UTF8,
						timeZone: TimeZoneInfo.Local));
			}

			// invalid app name record literal
			// invalid start-up time record literal
			// invalid seq-no record literal
		}
	}
}
