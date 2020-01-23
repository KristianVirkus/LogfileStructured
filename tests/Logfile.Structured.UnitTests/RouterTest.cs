using FluentAssertions;
using Logfile.Core;
using Logfile.Core.Details;
using Logfile.Structured.Elements;
using Logfile.Structured.Formatters;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Logfile.Structured.UnitTests
{
	class RouterTest
	{
		class TestHelpers
		{
			public static StructuredLogfileConfiguration<StandardLoglevel> CreateConfiguration(
				IEnumerable<IStreamWriter> additionalStreamWriters = null,
				bool makeAdditionalStreamWritersNull = false)
			{
				return new StructuredLogfileConfiguration<StandardLoglevel>(
					"test",
					false,
					false,
					false,
					"./logs",
					"{app-name}.log",
					256,
					0,
					new Dictionary<Type, ILogEventDetailFormatter>
					{
						{ typeof(Core.Details.Binary), Structured.Formatters.Binary.Default },
						{ typeof(Core.Details.EventID), Structured.Formatters.EventID.Default },
						{ typeof(Core.Details.Message), Structured.Formatters.Message.Default },
						// TODO { typeof(Core.Details.ExceptionDetail), Structured.Formatters.ExceptionDetail.Instance },
					},
					new Aes256SensitiveSettings(ContentEncoding.Encoding.GetBytes(new string(' ', 32))),
					additionalStreamWriters ?? (makeAdditionalStreamWritersNull ? null : new IStreamWriter[0]),
					false);
			}

			public static Router<StandardLoglevel> CreateRouter(
				StructuredLogfileConfiguration<StandardLoglevel> configuration = null,
				bool makeConfigurationNull = false,
				Stream outputStream = null,
				bool makeOutputStreamNull = false)
			{
				var router = new Router<StandardLoglevel>();
				router.ReconfigureAsync(configuration ?? (makeConfigurationNull ? null : TestHelpers.CreateConfiguration()), default).GetAwaiter().GetResult();
				return router;
			}
		}

		class Setup
		{
			public Logfile<StandardLoglevel> Logfile { get; set; }
			public MemoryStream Stream { get; set; }
			public Router<StandardLoglevel> Router { get; set; }
			public StructuredLogfileConfiguration<StandardLoglevel> Configuration { get; set; }
			public IEnumerable<IStreamWriter> AdditionalStreamWriters { get; set; }

			public Setup(
				StructuredLogfileConfiguration<StandardLoglevel> configuration = null)
			{
				this.Logfile = new Logfile<StandardLoglevel>();
				this.Stream = new MemoryStream();
				this.Configuration = configuration ?? TestHelpers.CreateConfiguration();
				this.Router = TestHelpers.CreateRouter(
					configuration: this.Configuration,
					outputStream: this.Stream);
				this.AdditionalStreamWriters = this.Configuration.StreamWriters;
			}
		}

		public class Lifecycle
		{
			[Test]
			public async Task StartWhileAlreadyStarted_Should_Ignore()
			{
				// Arrange
				var setup = new Setup();

				// Act
				// Assert
				await setup.Router.StartAsync(default);
				await setup.Router.StartAsync(default);
			}

			[Test]
			public async Task StopWhileAlreadyStopped_Should_Ignore()
			{
				// Arrange
				var setup = new Setup();

				// Act
				// Assert
				await setup.Router.StartAsync(default);
				await setup.Router.StopAsync(default);
				await setup.Router.StopAsync(default);
			}

			[Test]
			public async Task ForwardLogEventsWhileStopped_Should_ForwardLogEvents()
			{
				// Arrange
				var setup = new Setup(configuration: TestHelpers.CreateConfiguration(additionalStreamWriters: new[] { Mock.Of<IStreamWriter>() }));
				var logEvent = setup.Logfile.New(StandardLoglevel.Warning);
				var unblockWriteAsync = new ManualResetEventSlim(true);
				var written = 0;
				Mock.Get(setup.AdditionalStreamWriters.Single())
					.Setup(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
					.Returns<string, CancellationToken>((_text, _cancellationToken) =>
					{
						unblockWriteAsync.Wait();
						++written;
						return Task.CompletedTask;
					});

				// Act
				// Assert
				await setup.Router.StartAsync(default);

				await setup.Router.ForwardAsync(new[] { logEvent }, default);
				written.Should().Be(1);
				await setup.Router.StopAsync(default);

				await setup.Router.ForwardAsync(new[] { logEvent }, default);
				written.Should().Be(2);
			}
		}

		public class Forwarding
		{
			[Test]
			public void RoutablesNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				var setup = new Setup();

				// Act
				// Assert
				Assert.ThrowsAsync<ArgumentNullException>(
					async () => await setup.Router.ForwardAsync(null, default));
			}

			[Test]
			public void CancellationTokenAlreadyCanceled_ShouldThrow_OperationCanceledException()
			{
				// Arrange
				var setup = new Setup();
				var cts = new CancellationTokenSource();
				cts.Cancel();

				// Act
				// Assert
				Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(),
					async () => await setup.Router.ForwardAsync(new[] { setup.Logfile.New(StandardLoglevel.Warning) }, cts.Token));
			}

			[Test]
			public async Task RoutablesEmpty_Should_NotInvokeWriter()
			{
				// Arrange
				var setup = new Setup(configuration: TestHelpers.CreateConfiguration(additionalStreamWriters: new[] { Mock.Of<IStreamWriter>() }));
				var written = false;
				Mock.Get(setup.AdditionalStreamWriters.Single())
					.Setup(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
					.Returns<string, CancellationToken>((_text, _cancellationToken) =>
					{
						written = true;
						return Task.CompletedTask;
					});

				// Act
				await setup.Router.ForwardAsync(new LogEvent<StandardLoglevel>[0], default);

				// Assert
				written.Should().BeFalse();
			}

			[Test]
			public async Task ExceptionInWriter_Should_Ignore()
			{
				// Arrange
				var setup = new Setup(configuration: TestHelpers.CreateConfiguration(additionalStreamWriters: new[] { Mock.Of<IStreamWriter>() }));
				var written = false;
				Mock.Get(setup.AdditionalStreamWriters.Single())
					.Setup(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
					.Throws<InvalidOperationException>();

				// Act
				await setup.Router.ForwardAsync(new[] { setup.Logfile.New(StandardLoglevel.Warning) }, default);

				// Assert
				written.Should().BeFalse();
			}

			[Test]
			public async Task ForwardLogEvent_Should_SendItToWrite()
			{
				// Arrange
				var setup = new Setup(configuration: TestHelpers.CreateConfiguration(additionalStreamWriters: new[] { Mock.Of<IStreamWriter>() }));
				var written = false;
				Mock.Get(setup.AdditionalStreamWriters.Single())
					.Setup(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
					.Returns<string, CancellationToken>((_text, _cancellationToken) =>
					{
						written = true;
						return Task.CompletedTask;
					});

				// Act
				await setup.Router.ForwardAsync(new[] { setup.Logfile.New(StandardLoglevel.Warning) }, default);

				// Assert
				written.Should().BeTrue();
			}
		}
	}
}
