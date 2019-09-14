using FluentAssertions;
using Logfile.Core;
using Logfile.Core.Details;
using Logfile.Structured.Formatters;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logfile.Structured.UnitTests
{
	class StructuredLogfileConfigurationBuilderTest
	{
		public class Constructors
		{
			[Test]
			public void Construtor_Should_AddDefaultLogEventDetailFormatters()
			{
				// Arrange
				// Act
				var obj = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();

				// Assert
				obj.LogEventDetailFormatters.Keys.Should().Contain(typeof(Core.Details.Binary));
				obj.LogEventDetailFormatters.Keys.Should().Contain(typeof(Core.Details.EventID));
				obj.LogEventDetailFormatters.Keys.Should().Contain(typeof(Core.Details.ExceptionDetail));
				obj.LogEventDetailFormatters.Keys.Should().Contain(typeof(Core.Details.Message));
			}
		}

		public class Build
		{
			[Test]
			public void Unconfigured_Should_CreateDefaultConfiguration()
			{
				// Arrange
				// Act
				// Assert
				new StructuredLoglevelConfigurationBuilder<StandardLoglevel>().Build();
			}

			[Test]
			public void UnconfiguredButNecessarySettingsOnly_Should_CreateDefaultConfiguration()
			{
				// Arrange
				var additionalStreamWriter = Mock.Of<IStreamWriter>();

				// Act
				var configuration = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>()
					.UseWriter(additionalStreamWriter)
					.Build();

				// Assert
				configuration.LogEventDetailFormatters.Keys.Should().Contain(typeof(Core.Details.Binary));
				configuration.LogEventDetailFormatters.Keys.Should().Contain(typeof(Core.Details.EventID));
				configuration.LogEventDetailFormatters.Keys.Should().Contain(typeof(Core.Details.ExceptionDetail));
				configuration.LogEventDetailFormatters.Keys.Should().Contain(typeof(Core.Details.Message));
				configuration.SensitiveSettings.Should().BeNull();
				configuration.StreamWriters.Should().Contain(additionalStreamWriter);
			}

			[Test]
			public void FullyConfigured_Should_CreateCustomConfiguration()
			{
				// Arrange
				var additionalStreamWriters = new[] { Mock.Of<IStreamWriter>() };
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>
				{
					LogEventDetailFormatters = new Dictionary<Type, Structured.Formatters.ILogEventDetailFormatter>
						{
							{ typeof(Logfile.Core.Details.Message), Logfile.Structured.Formatters.Message.Default }
						},
					SensitiveSettings = new Aes256SensitiveSettings(new byte[32]),
					StreamWriters = additionalStreamWriters.ToList(),
				};

				// Act
				var configuration = builder.Build();

				// Assert
				configuration.LogEventDetailFormatters.Keys.Single().Should().Be(typeof(Logfile.Core.Details.Message));
				configuration.SensitiveSettings.Should().BeOfType<Aes256SensitiveSettings>();
				configuration.StreamWriters.Should().Contain(additionalStreamWriters.Single());
			}
		}

		public class Configure
		{
			[Test]
			public void UseLogEventDetailFormatterTypeNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();

				// Act
				// Assert
				Assert.Throws<ArgumentNullException>(() => builder.UseLogEventDetailFormatter(null, Mock.Of<ILogEventDetailFormatter>()));
			}

			[Test]
			public void UseLogEventDetailFormatterFormatterNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();

				// Act
				// Assert
				Assert.Throws<ArgumentNullException>(() => builder.UseLogEventDetailFormatter(typeof(object), null));
			}

			[Test]
			public void UseLogEventDetailFormatter_Should_AddFormatterForType()
			{
				// Arrange
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();

				// Act
				builder.UseLogEventDetailFormatter(typeof(object), Mock.Of<ILogEventDetailFormatter>());

				// Assert
				builder.LogEventDetailFormatters[typeof(object)].Should().NotBeNull();
			}

			[Test]
			public void UseLogEventDetailFormatterForExistingType_Should_ReplaceFormatterForType()
			{
				// Arrange
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();
				var initialFormatter = Mock.Of<ILogEventDetailFormatter>();
				var replacementFormatter = Mock.Of<ILogEventDetailFormatter>();

				// Act
				builder.UseLogEventDetailFormatter(typeof(object), initialFormatter);
				builder.UseLogEventDetailFormatter(typeof(object), replacementFormatter);

				// Assert
				builder.LogEventDetailFormatters[typeof(object)].Should().BeSameAs(replacementFormatter);
			}

			[Test]
			public void UseLogEventDetailFormatterWithConfigurationBuilderNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act
				// Assert
				Assert.Throws<ArgumentNullException>(() =>
				{
					ConfigurationBuilderExtensions.UseLogEventDetailFormatter<StandardLoglevel>(null, typeof(object), Mock.Of<ILogEventDetailFormatter>());
				});
			}

			[Test]
			public void UseSensitiveSettingsNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();

				// Act
				// Assert
				Assert.Throws<ArgumentNullException>(() => ConfigurationBuilderExtensions.UseSensitiveSettings(builder, null));
			}

			[Test]
			public void UseSensitiveSettings_Should_ApplySensitiveSettings()
			{
				// Arrange
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();

				// Act
				builder.UseSensitiveSettings(new Aes256SensitiveSettings(new byte[32]));

				// Assert
				builder.SensitiveSettings.Should().BeOfType<Aes256SensitiveSettings>();
			}

			[Test]
			public void UseSensitiveSettingsWithConfigurationBuilderNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act
				// Assert
				Assert.Throws<ArgumentNullException>(() =>
				{
					ConfigurationBuilderExtensions.UseSensitiveSettings<StandardLoglevel>(null, new Aes256SensitiveSettings(new byte[32]));
				});
			}

			[Test]
			public void AddWriterNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();

				// Act
				// Assert
				Assert.Throws<ArgumentNullException>(() => builder.UseWriter(null));
			}

			[Test]
			public void AddWriter_Should_Succeed()
			{
				// Arrange
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();
				var additionalStreamWriter = Mock.Of<IStreamWriter>();

				// Act
				builder.UseWriter(additionalStreamWriter);

				// Assert
				builder.StreamWriters.Should().Contain(additionalStreamWriter);
			}

			[Test]
			public void AddWriterWithConfigurationBuilderNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				var additionalStreamWriter = Mock.Of<IStreamWriter>();

				// Act
				// Assert
				Assert.Throws<ArgumentNullException>(() =>
				{
					ConfigurationBuilderExtensions.UseWriter<StandardLoglevel>(null, additionalStreamWriter);
				});
			}

			[Test]
			public void BeautifyConsoleOutput_Should_Succeed()
			{
				// Arrange
				var builder = new StructuredLoglevelConfigurationBuilder<StandardLoglevel>();

				// Assert
				builder.IsConsoleOutputBeautified.Should().BeFalse();

				// Act
				builder.BeautifyConsoleOutput();

				// Assert
				builder.IsConsoleOutputBeautified.Should().BeTrue();
			}
		}
	}
}
