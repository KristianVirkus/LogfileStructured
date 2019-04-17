using FluentAssertions;
using Logfile.Core;
using NUnit.Framework;
using System;
using System.Linq;

namespace Logfile.Structured.UnitTests
{
	class LogfileConfigurationBuilderExtensionsTest
	{
		public class AddStructuredLogfile
		{
			[Test]
			public void LogfileConfigurationBuilderNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act
				// Assert
				Assert.Throws<ArgumentNullException>(() =>
					LogfileConfigurationBuilderExtensions.AddStructuredLogfile<StandardLoglevel>(null, (_builder) => { }));
			}

			[Test]
			public void CallWithLogfileConfigurationBuilder_ShouldReturn_SameInstance()
			{
				// Arrange
				var logfileConfigurationBuilder = new LogfileConfigurationBuilder<StandardLoglevel>();

				// Act
				// Assert
				logfileConfigurationBuilder.AddStructuredLogfile((_builder) => { }).Should().BeSameAs(logfileConfigurationBuilder);
			}

			[Test]
			public void LogfileConfigurationConfigureCallbackNull_Should_SucceedAnyway()
			{
				// Arrange
				var logfileConfigurationBuilder = new LogfileConfigurationBuilder<StandardLoglevel>();

				// Act
				logfileConfigurationBuilder.AddStructuredLogfile(null);

				// Assert
				var router = logfileConfigurationBuilder.Routers.OfType<Router<StandardLoglevel>>().Any().Should().BeTrue();
			}

			[Test]
			public void ExceptionInConfigureCallback_ShouldThrow_Exception()
			{
				// Arrange
				var logfileConfigurationBuilder = new LogfileConfigurationBuilder<StandardLoglevel>();

				// Act
				// Assert
				Assert.Throws<InvalidOperationException>(() =>
					logfileConfigurationBuilder.AddStructuredLogfile((_builder) =>
						{
							throw new InvalidOperationException();
						}));
			}

			[Test]
			public void WorkOnBuilderInCallback_Should_ReflectConfiguration()
			{
				// Arrange
				var logfileConfigurationBuilder = new LogfileConfigurationBuilder<StandardLoglevel>();

				// Act
				logfileConfigurationBuilder.AddStructuredLogfile((_builder) =>
				{
					_builder.UseAppName("test-app");
				});

				// Assert
				var router = logfileConfigurationBuilder.Routers.OfType<Router<StandardLoglevel>>().Single();
				var configurationField = ((Configuration<StandardLoglevel>)router
						.GetType()
						.GetField("configuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
						.GetValue(router));
				configurationField.AppName.Should().Be("test-app");
			}
		}
	}
}
