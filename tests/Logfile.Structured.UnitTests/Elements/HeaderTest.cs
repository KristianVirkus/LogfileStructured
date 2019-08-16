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
	class HeaderTest
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

		Header<StandardLoglevel> createHeader(
			string appName = "TestApp",
			DateTime? appStartUpTime = null,
			Guid? appInstanceID = null,
			int appInstanceSequenceNumber = 1,
			Dictionary<string, string> miscellaneous = null,
			bool makeMiscellaneousNull = false)
		{
			return new Header<StandardLoglevel>(
				appName,
				appStartUpTime ?? DateTime.Now,
				appInstanceID ?? Guid.NewGuid(),
				appInstanceSequenceNumber,
				miscellaneous ?? (makeMiscellaneousNull ? null : new Dictionary<string, string>()));
		}

		[Test]
		public void ConstructorWithAppNameNull_ShouldThrow_ArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => this.createHeader(appName: null));
		}

		[Test]
		public void ConstructorWithMiscellaneousInformationNull_ShouldThrow_ArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => this.createHeader(miscellaneous: null, makeMiscellaneousNull: true));
		}

		[Test]
		public void Constructor_Should_SetProperties()
		{
			var time = DateTime.Now;
			var instanceID = Guid.NewGuid();
			var obj = this.createHeader(
				appName: "TestApp",
				appStartUpTime: time,
				appInstanceID: instanceID,
				appInstanceSequenceNumber: 1,
				miscellaneous: new Dictionary<string, string>());
			obj.AppName.Should().Be("TestApp");
			obj.AppStartUpTime.Should().Be(time);
			obj.AppInstanceLogfileSequenceNumber.Should().Be(1);
			obj.Miscellaneous.Should().BeEmpty();
		}

		[Test]
		public void SerializeWithSimpleValues_Should_ProduceValidOutput()
		{
			var configuration = createConfiguration();
			var time = DateTime.Now;
			var instanceID = Guid.NewGuid();
			var miscellaneous = new Dictionary<string, string>
			{
				{ "key", "value" },
			};

			var serialized = this.createHeader(appStartUpTime: time, appInstanceID: instanceID, miscellaneous: miscellaneous).Serialize(configuration);

			var expected = $"{Constants.EntitySeparator}{Header<StandardLoglevel>.LogfileIdentity}"
				+ $"{Header<StandardLoglevel>.RecordSeparator}{Header<StandardLoglevel>.VisualRecordSeparator}{Header<StandardLoglevel>.AppNameRecord}={Header<StandardLoglevel>.QuotationSign}TestApp{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Header<StandardLoglevel>.RecordSeparator}{Header<StandardLoglevel>.VisualRecordSeparator}{Header<StandardLoglevel>.AppStartUpTimeRecord}={Header<StandardLoglevel>.QuotationSign}{time.ToIso8601String()}{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Header<StandardLoglevel>.RecordSeparator}{Header<StandardLoglevel>.VisualRecordSeparator}{Header<StandardLoglevel>.AppInstanceLogfileSequenceNumberRecord}=1"
				+ $"{Constants.NewLine}{Header<StandardLoglevel>.RecordSeparator}{Constants.Indent}{Header<StandardLoglevel>.QuotationSign}key{Header<StandardLoglevel>.QuotationSign}={Header<StandardLoglevel>.QuotationSign}value{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Constants.NewLine}";

			serialized.Should().Be(expected);
		}

		[Test]
		public void SerializeWithValuesToEscape_Should_ProduceEscapedOutput()
		{
			var configuration = createConfiguration();
			var time = DateTime.Now;
			var instanceID = Guid.NewGuid();
			var miscellaneous = new Dictionary<string, string>
			{
				{ "key", "value" },
				{ "key%text", @"value`text" },
			};

			var serialized = this.createHeader(appStartUpTime: time, appInstanceID: instanceID, miscellaneous: miscellaneous).Serialize(configuration);

			var expected = $"{Constants.EntitySeparator}{Header<StandardLoglevel>.LogfileIdentity}"
				+ $"{Header<StandardLoglevel>.RecordSeparator}{Header<StandardLoglevel>.VisualRecordSeparator}{Header<StandardLoglevel>.AppNameRecord}={Header<StandardLoglevel>.QuotationSign}TestApp{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Header<StandardLoglevel>.RecordSeparator}{Header<StandardLoglevel>.VisualRecordSeparator}{Header<StandardLoglevel>.AppStartUpTimeRecord}={Header<StandardLoglevel>.QuotationSign}{time.ToIso8601String()}{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Header<StandardLoglevel>.RecordSeparator}{Header<StandardLoglevel>.VisualRecordSeparator}{Header<StandardLoglevel>.AppInstanceLogfileSequenceNumberRecord}=1"
				+ $"{Constants.NewLine}{Header<StandardLoglevel>.RecordSeparator}{Constants.Indent}{Header<StandardLoglevel>.QuotationSign}key{Header<StandardLoglevel>.QuotationSign}={Header<StandardLoglevel>.QuotationSign}value{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Constants.NewLine}{Header<StandardLoglevel>.RecordSeparator}{Constants.Indent}{Header<StandardLoglevel>.QuotationSign}key%25text{Header<StandardLoglevel>.QuotationSign}={Header<StandardLoglevel>.QuotationSign}value%60text{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Constants.NewLine}";

			serialized.Should().Be(expected);
		}

		[Test]
		public void SerializeWithLineBreaksInValues_Should_ProduceMultiLineOutput()
		{
			var configuration = createConfiguration();
			var time = DateTime.Now;
			var instanceID = Guid.NewGuid();
			var miscellaneous = new Dictionary<string, string>
			{
				{ "key", "value" },
				{ "key text", @"value
multi-line
text" },
			};

			var serialized = this.createHeader(appStartUpTime: time, appInstanceID: instanceID, miscellaneous: miscellaneous).Serialize(configuration);

			var expected = $"{Constants.EntitySeparator}{Header<StandardLoglevel>.LogfileIdentity}"
				+ $"{Header<StandardLoglevel>.RecordSeparator}{Header<StandardLoglevel>.VisualRecordSeparator}{Header<StandardLoglevel>.AppNameRecord}={Header<StandardLoglevel>.QuotationSign}TestApp{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Header<StandardLoglevel>.RecordSeparator}{Header<StandardLoglevel>.VisualRecordSeparator}{Header<StandardLoglevel>.AppStartUpTimeRecord}={Header<StandardLoglevel>.QuotationSign}{time.ToIso8601String()}{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Header<StandardLoglevel>.RecordSeparator}{Header<StandardLoglevel>.VisualRecordSeparator}{Header<StandardLoglevel>.AppInstanceLogfileSequenceNumberRecord}=1"
				+ $"{Constants.NewLine}{Header<StandardLoglevel>.RecordSeparator}{Constants.Indent}{Header<StandardLoglevel>.QuotationSign}key{Header<StandardLoglevel>.QuotationSign}={Header<StandardLoglevel>.QuotationSign}value{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Constants.NewLine}{Header<StandardLoglevel>.RecordSeparator}{Constants.Indent}{Header<StandardLoglevel>.QuotationSign}key text{Header<StandardLoglevel>.QuotationSign}={Header<StandardLoglevel>.QuotationSign}value"
				+ $"\nmulti-line\ntext{Header<StandardLoglevel>.QuotationSign}"
				+ $"{Constants.NewLine}";

			serialized.Should().Be(expected);
		}
	}
}
