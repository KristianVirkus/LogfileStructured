using FluentAssertions;
using Logfile.Core;
using Logfile.Core.Details;
using Logfile.Structured.Elements;
using Logfile.Structured.Formatters;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
				IEnumerable<ITextWriter> additionalStreamWriters = null,
				bool makeAdditionalStreamWritersNull = false,
				int maximumLogfileSize = 256,
				int? keepLogfiles = 0,
				string fileNameFormat = "{app-name}-{start-up-time}-{seq-no}.log",
				string path = "")
			{
				return new StructuredLogfileConfiguration<StandardLoglevel>(
					"test",
					false,
					false,
					false,
					path,
					fileNameFormat,
					maximumLogfileSize,
					keepLogfiles,
					new Dictionary<Type, ILogEventDetailFormatter>
					{
						{ typeof(Core.Details.Binary), Structured.Formatters.Binary.Default },
						{ typeof(Core.Details.EventID), Structured.Formatters.EventID.Default },
						{ typeof(Core.Details.Message), Structured.Formatters.Message.Default },
						// TODO { typeof(Core.Details.ExceptionDetail), Structured.Formatters.ExceptionDetail.Instance },
					},
					new Aes256SensitiveSettings(ContentEncoding.Encoding.GetBytes(new string(' ', 32))),
					additionalStreamWriters ?? (makeAdditionalStreamWritersNull ? null : new ITextWriter[0]),
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
			public IEnumerable<ITextWriter> AdditionalStreamWriters { get; set; }

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
				var setup = new Setup(configuration: TestHelpers.CreateConfiguration(additionalStreamWriters: new[] { Mock.Of<ITextWriter>() }));
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
				var setup = new Setup(configuration: TestHelpers.CreateConfiguration(additionalStreamWriters: new[] { Mock.Of<ITextWriter>() }));
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
				var setup = new Setup(configuration: TestHelpers.CreateConfiguration(additionalStreamWriters: new[] { Mock.Of<ITextWriter>() }));
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
				var setup = new Setup(configuration: TestHelpers.CreateConfiguration(additionalStreamWriters: new[] { Mock.Of<ITextWriter>() }));
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

		public class FindingCommonBeginning
		{
			[Test]
			public void ANull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(() => Router<StandardLoglevel>.FindCommonBeginning(a: null, b: "test2"));
			}

			[Test]
			public void BNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(() => Router<StandardLoglevel>.FindCommonBeginning(a: "test1", b: null));
			}

			[TestCase("", "")]
			[TestCase("abc", "def")]
			[TestCase("xzzz", "yzzz")]
			[TestCase("", "b")]
			[TestCase("a", "")]
			public void NoCommonBeginning_Should_ReturnEmptyString(string a, string b)
			{
				// Arrange
				// Act
				var result = Router<StandardLoglevel>.FindCommonBeginning(a: a, b: b);

				// Assert
				result.Should().Be("");
			}

			[TestCase("test", "test", ExpectedResult = "test")]
			[TestCase("test1", "test2", ExpectedResult = "test")]
			[TestCase("tes", "test", ExpectedResult = "tes")]
			[TestCase("test", "tes", ExpectedResult = "tes")]
			public string CommonBeginning_Should_ReturnCommonBeginning(string a, string b)
			{
				// Arrange
				// Act
				// Assert
				return Router<StandardLoglevel>.FindCommonBeginning(a: a, b: b);
			}
		}

		public class CleaningUpLogfiles
		{
			[Test]
			public void FileSystemNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				var router = TestHelpers.CreateRouter();

				// Act & Assert
				Assert.ThrowsAsync<ArgumentNullException>(
					async () => await router.CleanUpOldLogfilesAsync(null, default));
			}

			[Test]
			public void CancellationTokenAlreadyCanceled_ShouldThrow_OperationCanceledException()
			{
				// Arrange
				var router = TestHelpers.CreateRouter();

				// Act & Assert
				Assert.ThrowsAsync<OperationCanceledException>(
					async () => await router.CleanUpOldLogfilesAsync(
						Mock.Of<IFileSystem>(),
						new CancellationToken(true)).ConfigureAwait(false));
			}

			[Test]
			public async Task ExceptionInDeleteFile_Should_Ignore()
			{
				// Arrange
				var config = TestHelpers.CreateConfiguration(keepLogfiles: 0, path: "", fileNameFormat: "{seq-no}");
				var router = TestHelpers.CreateRouter(configuration: config);

				const int filesCount = 10;
				var files = new List<string>();
				for (int i = 1; i <= filesCount; i++)
					files.Add(Path.Combine(config.Path, config.BuildFileName(i)));

				var fileSystem = Mock.Of<IFileSystem>();
				Mock.Get(fileSystem)
					.Setup(m => m.EnumerateFiles(It.IsAny<string>()))
					.Throws<AggregateException>();

				// Act & Assert
				await router.CleanUpOldLogfilesAsync(fileSystem, default).ConfigureAwait(false);
				files.Count.Should().Be(filesCount);
			}

			[Test]
			public async Task ExceptionInEnumerateFiles_Should_Ignore()
			{
				// Arrange
				var config = TestHelpers.CreateConfiguration(keepLogfiles: 0, path: "", fileNameFormat: "{seq-no}");
				var router = TestHelpers.CreateRouter(configuration: config);

				const int filesCount = 10;
				var files = new List<string>();
				for (int i = 1; i <= filesCount; i++)
					files.Add(Path.Combine(config.Path, config.BuildFileName(i)));

				var fileSystem = Mock.Of<IFileSystem>();
				Mock.Get(fileSystem)
					.Setup(m => m.EnumerateFiles(It.IsAny<string>()))
					.Returns(files);
				Mock.Get(fileSystem)
					.Setup(m => m.OpenForReading(It.IsAny<string>()))
					.Returns<string>(
						filePath => new MemoryStream(
							ContentEncoding.Encoding.GetBytes(
								new Header<StandardLoglevel>(
									appName: config.AppName,
									appStartUpTime: DateTime.Now,
									appInstanceSequenceNumber: int.Parse(filePath),
									miscellaneous: new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())).Serialize(config))));

				// Act & Assert
				await router.CleanUpOldLogfilesAsync(fileSystem, default).ConfigureAwait(false);
				files.Count.Should().Be(filesCount);
			}

			[Test]
			public async Task ExceptionInFileSystemOpenFile_Should_Ignore()
			{
				// Arrange
				var config = TestHelpers.CreateConfiguration(keepLogfiles: 0, path: "", fileNameFormat: "{seq-no}");
				var router = TestHelpers.CreateRouter(configuration: config);

				const int filesCount = 10;
				var files = new List<string>();
				for (int i = 1; i <= filesCount; i++)
					files.Add(Path.Combine(config.Path, config.BuildFileName(i)));

				var fileSystem = Mock.Of<IFileSystem>();
				Mock.Get(fileSystem)
					.Setup(m => m.EnumerateFiles(It.IsAny<string>()))
					.Returns(files);
				Mock.Get(fileSystem)
					.Setup(m => m.OpenForReading(It.IsAny<string>()))
					.Throws<AggregateException>();

				// Act & Assert
				await router.CleanUpOldLogfilesAsync(fileSystem, default).ConfigureAwait(false);
				files.Count.Should().Be(filesCount);
			}

			[Test]
			public async Task DoNotDeleteAnyLogfiles_Should_KeepOldLogfiles()
			{
				// Arrange
				var config = TestHelpers.CreateConfiguration(keepLogfiles: null);
				var router = TestHelpers.CreateRouter(configuration: config);

				const int filesCount = 10;
				var files = new List<string>();
				for (int i = 1; i <= filesCount; i++)
					files.Add(Path.Combine(config.Path, config.BuildFileName(i)));

				var fileSystem = Mock.Of<IFileSystem>();
				Mock.Get(fileSystem)
					.Setup(m => m.EnumerateFiles(It.IsAny<string>()))
					.Returns(files);
				Mock.Get(fileSystem)
					.Setup(m => m.DeleteFile(It.IsAny<string>()))
					.Callback<string>(filePath =>
					{
						foreach (var fileName in files.Where(n => filePath.EndsWith(n)).ToList())
							files.Remove(fileName);
					});

				// Act
				await router.CleanUpOldLogfilesAsync(fileSystem, default).ConfigureAwait(false);

				// Assert
				files.Count.Should().Be(filesCount);
			}

			[Test]
			public async Task DeleteAllLogfiles_Should_DeleteAllOldLogfiles()
			{
				// Arrange
				var config = TestHelpers.CreateConfiguration(keepLogfiles: 0, path: "", fileNameFormat: "{seq-no}");
				var router = TestHelpers.CreateRouter(configuration: config);

				const int filesCount = 10;
				var files = new List<string>();
				for (int i = 1; i <= filesCount; i++)
					files.Add(Path.Combine(config.Path, config.BuildFileName(i)));

				var fileSystem = Mock.Of<IFileSystem>();
				Mock.Get(fileSystem)
					.Setup(m => m.EnumerateFiles(It.IsAny<string>()))
					.Returns(files);
				Mock.Get(fileSystem)
					.Setup(m => m.DeleteFile(It.IsAny<string>()))
					.Callback<string>(filePath =>
					{
						foreach (var fileName in files.Where(n => filePath.EndsWith(n)).ToList())
							files.Remove(fileName);
					});
				Mock.Get(fileSystem)
					.Setup(m => m.OpenForReading(It.IsAny<string>()))
					.Returns<string>(
						filePath => new MemoryStream(
							ContentEncoding.Encoding.GetBytes(
								new Header<StandardLoglevel>(
									appName: config.AppName,
									appStartUpTime: DateTime.Now,
									appInstanceSequenceNumber: int.Parse(filePath),
									miscellaneous: new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())).Serialize(config))));

				// Act
				await router.CleanUpOldLogfilesAsync(fileSystem, default).ConfigureAwait(false);

				// Assert
				files.Count.Should().Be(0);
			}

			[Test]
			public async Task DeleteAllButTwoLogfiles_Should_DeleteAllButTwoOldLogfiles()
			{
				// Arrange
				var config = TestHelpers.CreateConfiguration(keepLogfiles: 2, path: "", fileNameFormat: "{seq-no}");
				var router = TestHelpers.CreateRouter(configuration: config);

				const int filesCount = 10;
				var files = new List<string>();
				for (int i = 1; i <= filesCount; i++)
					files.Add(Path.Combine(config.Path, config.BuildFileName(i)));

				var fileSystem = Mock.Of<IFileSystem>();
				Mock.Get(fileSystem)
					.Setup(m => m.EnumerateFiles(It.IsAny<string>()))
					.Returns(files);
				Mock.Get(fileSystem)
					.Setup(m => m.DeleteFile(It.IsAny<string>()))
					.Callback<string>(filePath =>
					{
						foreach (var fileName in files.Where(n => filePath.EndsWith(n)).ToList())
							files.Remove(fileName);
					});
				Mock.Get(fileSystem)
					.Setup(m => m.OpenForReading(It.IsAny<string>()))
					.Returns<string>(
						filePath => new MemoryStream(
							ContentEncoding.Encoding.GetBytes(
								new Header<StandardLoglevel>(
									appName: config.AppName,
									appStartUpTime: DateTime.Now,
									appInstanceSequenceNumber: int.Parse(filePath),
									miscellaneous: new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())).Serialize(config))));

				// Act
				await router.CleanUpOldLogfilesAsync(fileSystem, default).ConfigureAwait(false);

				// Assert
				files.Count.Should().Be(2);
				files.Should().Contain("9");
				files.Should().Contain("10");
			}
		}

		public class Flushing
		{
			[Test]
			public async Task CancellationTokenAlreadyCanceled_ShouldThrow_OperationCanceledException()
			{
				// Arrange
				var router = TestHelpers.CreateRouter();

				// Act & Assert
				Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(),
					async () => await router.FlushAsync(new CancellationToken(true)));
			}

			[Test]
			public async Task ExceptionInWriterFlush_Should_Ignore()
			{
				// Arrange
				var exceptionWriter = Mock.Of<ITextWriter>();
				Mock.Get(exceptionWriter)
					.Setup(m => m.FlushAsync(It.IsAny<CancellationToken>()))
					.Throws<AggregateException>();

				var finalWriterFlushed = false;
				var finalWriter = Mock.Of<ITextWriter>();
				Mock.Get(finalWriter)
					.Setup(m => m.FlushAsync(It.IsAny<CancellationToken>()))
					.Returns<CancellationToken>(_ =>
					{
						finalWriterFlushed = true;
						return Task.CompletedTask;
					});

				var config = TestHelpers.CreateConfiguration(additionalStreamWriters: new[] { exceptionWriter, finalWriter });
				var router = TestHelpers.CreateRouter(configuration: config);

				// Act
				await router.FlushAsync(default).ConfigureAwait(false);

				// Assert
				finalWriterFlushed.Should().BeTrue();
			}
		}
	}
}
