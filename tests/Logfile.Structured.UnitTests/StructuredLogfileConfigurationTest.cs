using EventRouter.Core;
using FluentAssertions;
using Logfile.Core;
using Logfile.Core.Details;
using Logfile.Structured.Formatters;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Logfile.Structured.UnitTests
{
	class StructuredLogfileConfigurationTest
	{
		static readonly IReadOnlyDictionary<Type, ILogEventDetailFormatter> DefaultLogEventDetailFormatters = new Dictionary<Type, ILogEventDetailFormatter>()
		{
			{ typeof(Logfile.Core.Details.Message), Logfile.Structured.Formatters.Message.Default },
		};

		static readonly ISensitiveSettings DefaultSensitiveSettings = new Aes256SensitiveSettings(new byte[32]);

		static readonly IEnumerable<IStreamWriter> DefaultStreamWriters;

		static StructuredLogfileConfigurationTest()
		{
			DefaultStreamWriters = new[] { Mock.Of<IStreamWriter>() };
			Mock.Get(DefaultStreamWriters.Single())
				.Setup(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Callback(() => { });
			Mock.Get(DefaultStreamWriters.Single())
				.Setup(m => m.Dispose())
				.Callback(() => { });
		}

		static StructuredLogfileConfiguration<StandardLoglevel> createConfiguration(
			string appName = "app",
			bool writeToConsole = true,
			bool writeToDebugConsole = true,
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
				additionalStreamWriters ?? (makeAdditionalStreamWritersNull ? null : DefaultStreamWriters));
		}

		[Test]
		public void Constructor_Should_SetProperties()
		{
			// Arrange
			var routers = new IRouter<LogEvent<StandardLoglevel>>[0];
			var preprocessors = new IRoutablePreprocessor<LogEvent<StandardLoglevel>>[0];

			// Act
			var configuration = createConfiguration(
				appName: "test-app",
				writeToConsole: true,
				writeToDebugConsole: true,
				writeToDisk: true,
				path: "./logs",
				fileNameFormat: "custom format",
				maximumLogfileSize: 1024,
				keepLogfiles: 5,
				logEventDetailFormatters: DefaultLogEventDetailFormatters,
				sensitiveSettings: DefaultSensitiveSettings,
				additionalStreamWriters: DefaultStreamWriters);

			// Assert
			configuration.AppName.Should().Be("test-app");
			configuration.WriteToConsole.Should().BeTrue();
			configuration.WriteToDebugConsole.Should().BeTrue();
			configuration.WriteToDisk.Should().BeTrue();
			configuration.Path.Should().Be("./logs");
			configuration.FileNameFormat.Should().Be("custom format");
			configuration.MaximumLogfileSize.Should().Be(1024);
			configuration.KeepLogfiles.Should().Be(5);
			configuration.LogEventDetailFormatters.Should().BeSameAs(DefaultLogEventDetailFormatters);
			configuration.SensitiveSettings.Should().BeSameAs(DefaultSensitiveSettings);
			configuration.StreamWriters.Should().BeSameAs(DefaultStreamWriters);
		}

		[Test]
		public void AppNameNull_Should_UseDefault()
		{
			// Arrange
			// Act
			var configuration = createConfiguration(appName: null);

			// Assert
			configuration.AppName.Should().BeOneOf("testhost", StructuredLogfileConfiguration<StandardLoglevel>.DefaultAppName);
		}

		[Test]
		public void PathNull_Should_UseDefault()
		{
			// Arrange
			// Act
			var configuration = createConfiguration(path: null);

			// Assert
			configuration.Path.Should().Be(StructuredLogfileConfiguration<StandardLoglevel>.DefaultPath);
		}

		[Test]
		public void FileNameFormatNull_Should_UseDefault()
		{
			// Arrange
			// Act
			var configuration = createConfiguration(fileNameFormat: "logfile.log");

			// Assert
			configuration.FileNameFormat.Should().Be("logfile.log");
		}

		[Test]
		public void MaximumLogfilesZero_ShouldThrow_ArgumentOutOfRangeException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => createConfiguration(maximumLogfileSize: 0));
		}

		[Test]
		public void MaximumLogfilesNegative_ShouldThrow_ArgumentOutOfRangeException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => createConfiguration(maximumLogfileSize: -1));
		}

		[Test]
		public void KeepLogfilesZero_Should_Succeed()
		{
			// Arrange
			// Act
			var configuration = createConfiguration(keepLogfiles: 0);

			// Assert
			configuration.KeepLogfiles.Should().Be(0);
		}

		[Test]
		public void KeepLogfilesNull_Should_UseZeroLogfiles()
		{
			// Arrange

			var configuration = createConfiguration(keepLogfiles: null);

			// Assert
			configuration.KeepLogfiles.Should().NotBeNull();
		}

		[Test]
		public void KeepLogfilesNegative_ShouldThrow_ArgumentOutOfRangeException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => createConfiguration(keepLogfiles: -1));
		}

		[Test]
		public void LogEventDetailFormattersNull_ShouldThrow_ArgumentNullException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentNullException>(() => createConfiguration(makeLogEventDetailFormattersNull: true));
		}

		[Test]
		public void LogEventDetailFormattersEmpty_Should_Succeed()
		{
			// Arrange
			// Act
			var configuration = createConfiguration(logEventDetailFormatters: new Dictionary<Type, ILogEventDetailFormatter>());

			// Assert
			configuration.LogEventDetailFormatters.Should().BeEmpty();
		}

		[Test]
		public void SensitiveSettingsNull_Should_KeepNull()
		{
			// Arrange
			// Act
			var configuration = createConfiguration(sensitiveSettings: null);

			// Assert
			configuration.SensitiveSettings.Should().BeNull();
		}

		[Test]
		public void StreamWritersNull_ShouldThrow_ArgumentNullException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentNullException>(() => createConfiguration(makeAdditionalStreamWritersNull: true));
		}
	}
}
