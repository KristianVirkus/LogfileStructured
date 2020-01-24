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

		internal static StructuredLogfileConfiguration<StandardLoglevel> CreateConfiguration(
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
				var configuration = CreateConfiguration();
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
				var configuration = CreateConfiguration();
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
				var configuration = CreateConfiguration();
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
			public void DataNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(() => Header<StandardLoglevel>.Parse(data: null, timeZone: null));
			}

			[Test]
			public void ParseWithSimpleValues_Should_UseUtf8AndProduceValidOutput()
			{
				var configuration = CreateConfiguration();
				var time = DateTime.Now;
				var miscellaneous = new Dictionary<string, string>
				{
					{ "key", "value" },
				};

				var header = createHeader(appStartUpTime: time, miscellaneous: miscellaneous);
				var serialized = header.Serialize(configuration);
				var data = ContentEncoding.Encoding.GetBytes(serialized);
				var parsed = Header<StandardLoglevel>.Parse(
					data: data,
					timeZone: TimeZoneInfo.Local);

				parsed.MoreDataRequired.Should().BeFalse();
				parsed.ConsumedData.Should().Be(data.Length);
				var parsedHeader = (Header<StandardLoglevel>)parsed.Element;
				parsedHeader.AppName.Should().Be(header.AppName);
				parsedHeader.AppStartUpTime.Should().Be(time.ToUniversalTime());
				parsedHeader.AppInstanceLogfileSequenceNumber.Should().Be(1);
				parsedHeader.Miscellaneous.Single().Key.Should().Be("key");
				parsedHeader.Miscellaneous.Single().Value.Should().Be("value");
			}

			[Test]
			public void ParseWithMiscellaneousKeyValueQuotationStyles_Should_ProduceValidOutput()
			{
				var configuration = CreateConfiguration();
				var time = DateTime.Now;

				var header = createHeader(appStartUpTime: time);
				var serialized = header.Serialize(configuration);
				var tempSerialized = serialized.Remove(serialized.Length - 1);
				tempSerialized = tempSerialized + $"{Constants.RecordSeparator}`key`=`value`{Constants.RecordSeparator}key2=`value2`{Constants.RecordSeparator}`key3`=value3{Constants.RecordSeparator}   key4  =  value4  {Constants.RecordSeparator}`key5`  =  `value5`" + Constants.EntitySeparator;
				var data = ContentEncoding.Encoding.GetBytes(tempSerialized);
				var parsed = Header<StandardLoglevel>.Parse(
					data: data,
					timeZone: TimeZoneInfo.Local);

				parsed.MoreDataRequired.Should().BeFalse();
				parsed.ConsumedData.Should().Be(data.Length);
				var parsedHeader = (Header<StandardLoglevel>)parsed.Element;
				parsedHeader.AppName.Should().Be(header.AppName);
				parsedHeader.AppStartUpTime.Should().Be(time.ToUniversalTime());
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
				var configuration = CreateConfiguration();
				var time = DateTime.Now;

				var header = createHeader();
				var serialized = header.Serialize(configuration);
				serialized = 'X' + serialized.Substring(1);
				var data = ContentEncoding.Encoding.GetBytes(serialized);

				// Act & Assert
				Assert.Throws<FormatException>(
					() => Header<StandardLoglevel>.Parse(
						data: data,
						timeZone: TimeZoneInfo.Local));
			}

			[Test]
			public void EmptyAppName_Should_Ignore()
			{
				// Arrange
				var configuration = CreateConfiguration();

				var header = createHeader(appName: "");
				var serialized = header.Serialize(configuration);
				var data = ContentEncoding.Encoding.GetBytes(serialized);

				// Act
				var parsed = Header<StandardLoglevel>.Parse(
					data: data,
					timeZone: TimeZoneInfo.Local);

				// Assert
				parsed.MoreDataRequired.Should().BeFalse();
				parsed.ConsumedData.Should().Be(ContentEncoding.Encoding.GetBytes(serialized.TrimEnd('\n')).Length);
				var parsedHeader = (Header<StandardLoglevel>)parsed.Element;
				parsedHeader.AppName.Should().Be(header.AppName);
				parsedHeader.AppStartUpTime.Should().Be(header.AppStartUpTime.ToUniversalTime());
				parsedHeader.AppInstanceLogfileSequenceNumber.Should().Be(header.AppInstanceLogfileSequenceNumber);
				parsedHeader.Miscellaneous.Should().BeEmpty();
			}

			[Test]
			public void FewerRecordsThanExpected_ShouldThrow_NotSupportedException()
			{
				// Arrange
				var configuration = CreateConfiguration();

				var header = createHeader();
				var serialized = header.Serialize(configuration).Replace($" == app={Constants.QuotationMark}{header.AppName}{Constants.QuotationMark}{Constants.RecordSeparator}", "");
				var data = ContentEncoding.Encoding.GetBytes(serialized);

				// Act & Assert
				Assert.Throws<NotSupportedException>(
					() => Header<StandardLoglevel>.Parse(
						data: data,
						timeZone: TimeZoneInfo.Local));
			}

			[Test]
			public void ReplacedAppNameByUnknownRecord_ShouldThrow_FormatException()
			{
				// Arrange
				var configuration = CreateConfiguration();

				var header = createHeader();
				var serialized = header.Serialize(configuration).Replace($"app=", "test=");
				var data = ContentEncoding.Encoding.GetBytes(serialized);

				// Act & Assert
				Assert.Throws<FormatException>(
					() => Header<StandardLoglevel>.Parse(
						data: data,
						timeZone: TimeZoneInfo.Local));
			}

			[Test]
			public void ReplacedStartUpTimeByUnknownRecord_ShouldThrow_FormatException()
			{
				// Arrange
				var configuration = CreateConfiguration();

				var header = createHeader();
				var serialized = header.Serialize(configuration).Replace($"start-up=", "test=");
				var data = ContentEncoding.Encoding.GetBytes(serialized);

				// Act & Assert
				Assert.Throws<FormatException>(
					() => Header<StandardLoglevel>.Parse(
						data: data,
						timeZone: TimeZoneInfo.Local));
			}

			[Test]
			public void ReplacedSequenceNumberByUnknownRecord_ShouldThrow_FormatException()
			{
				// Arrange
				var configuration = CreateConfiguration();

				var header = createHeader();
				var serialized = header.Serialize(configuration).Replace($"seq-no=", "test=");
				var data = ContentEncoding.Encoding.GetBytes(serialized);

				// Act & Assert
				Assert.Throws<FormatException>(
					() => Header<StandardLoglevel>.Parse(
						data: data,
						timeZone: TimeZoneInfo.Local));
			}

			[Test]
			public void LocalStartUpTime_Should_GetConvertedToUtcIndependentOfTimeZoneArgument()
			{
				// Arrange
				var configuration = CreateConfiguration();

				var header = createHeader();
				var serialized = header.Serialize(configuration);
				var data = ContentEncoding.Encoding.GetBytes(serialized);

				// Act
				var result = Header<StandardLoglevel>.Parse(
					data: data,
					timeZone: TimeZoneInfo.FindSystemTimeZoneById("Mauritius Standard Time"));

				// Assert
				var parsedHeader = (Header<StandardLoglevel>)result.Element;
				parsedHeader.AppStartUpTime.Kind.Should().Be(DateTimeKind.Utc);
				parsedHeader.AppStartUpTime.Should().Be(header.AppStartUpTime.ToUniversalTime());
			}

			[Test]
			public void UnspecifiedStartUpTime_Should_GetConvertedToUtcBasedOnTimeZoneArgument()
			{
				// Arrange
				var configuration = CreateConfiguration();

				var header = createHeader();
				var localTimeZoneOffset = TimeZoneInfo.Local.BaseUtcOffset;
				var serialized = header.Serialize(configuration).Replace($"{(localTimeZoneOffset < TimeSpan.Zero ? "-" : "+")}{localTimeZoneOffset.ToString(@"hh\:mm")}", "");
				var data = ContentEncoding.Encoding.GetBytes(serialized);
				var mauritanianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Mauritius Standard Time");

				// Act
				var result = Header<StandardLoglevel>.Parse(
					data: data,
					timeZone: mauritanianTimeZone);

				// Assert
				var parsedHeader = (Header<StandardLoglevel>)result.Element;
				parsedHeader.AppStartUpTime.Kind.Should().Be(DateTimeKind.Utc);
				parsedHeader.AppStartUpTime.Should().Be(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(header.AppStartUpTime, DateTimeKind.Unspecified), mauritanianTimeZone));
			}
		}

		public class Identification
		{
			[Test]
			public void DataNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(() => Header<StandardLoglevel>.Identify(data: null));
			}

			[Test]
			public void IdentifiableData_ShouldReturn_NoMoreDataRequiredAndIsCompatible()
			{
				// Arrange
				var data = ContentEncoding.Encoding.GetBytes(Header<StandardLoglevel>.LogfileIdentity + Constants.RecordSeparator + "abc");

				// Act
				var result = Header<StandardLoglevel>.Identify(data: data);

				// Assert
				result.MoreDataRequired.Should().BeFalse();
				result.IsCompatible.Should().BeTrue();
			}

			[Test]
			public void IncompleteData_ShouldReturn_MoreDataRequiredAndNotIsCompatible()
			{
				// Arrange
				var data = ContentEncoding.Encoding.GetBytes(Header<StandardLoglevel>.LogfileIdentity);

				// Act
				var result = Header<StandardLoglevel>.Identify(data: data);

				// Assert
				result.MoreDataRequired.Should().BeTrue();
				result.IsCompatible.Should().BeFalse();
			}

			[Test]
			public void IncompatibleData_ShouldReturn_NoMoreDataRequiredAndNotIsCompatible()
			{
				// Arrange
				var data = ContentEncoding.Encoding.GetBytes(new string('x', Header<StandardLoglevel>.LogfileIdentity.Length) + Constants.RecordSeparator);

				// Act
				var result = Header<StandardLoglevel>.Identify(data: data);

				// Assert
				result.MoreDataRequired.Should().BeFalse();
				result.IsCompatible.Should().BeFalse();
			}
		}
	}
}
