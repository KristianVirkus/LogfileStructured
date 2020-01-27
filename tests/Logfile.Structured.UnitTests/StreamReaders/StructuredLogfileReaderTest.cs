using FluentAssertions;
using Logfile.Core;
using Logfile.Structured.Elements;
using Logfile.Structured.StreamReaders;
using Logfile.Structured.UnitTests.Elements;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Logfile.Structured.UnitTests.StreamReaders
{
	static class StructuredLogfileReaderTest
	{
		static StructuredLogfileReader<StandardLoglevel> createReader(
			Stream stream = null,
			string text = null)
		{
			if (stream == null && text != null)
				stream = text != null ? new MemoryStream(ContentEncoding.Encoding.GetBytes(text)) : null;
			return new StructuredLogfileReader<StandardLoglevel>(stream: stream);
		}

		public class Constructors
		{
			[Test]
			public void StreamNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(() => createReader(stream: null, text: null));
			}

			[Test]
			public void UnreadableStream_ShouldThrow_ArgumentException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentException>(() => createReader(stream: new TestUnreadableStream()));
			}

			[Test]
			public void Constructor_Should_Succeed()
			{
				// Arrange
				// Act
				// Assert
				createReader(text: "Test");
			}

			class TestUnreadableStream : Stream
			{
				public override bool CanRead => false;

				public override bool CanSeek => throw new NotImplementedException();

				public override bool CanWrite => throw new NotImplementedException();

				public override long Length => throw new NotImplementedException();

				public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

				public override void Flush()
				{
					throw new NotImplementedException();
				}

				public override int Read(byte[] buffer, int offset, int count)
				{
					throw new NotImplementedException();
				}

				public override long Seek(long offset, SeekOrigin origin)
				{
					throw new NotImplementedException();
				}

				public override void SetLength(long value)
				{
					throw new NotImplementedException();
				}

				public override void Write(byte[] buffer, int offset, int count)
				{
					throw new NotImplementedException();
				}
			}
		}

		public class Reading
		{
			[Test]
			public void CancellationTokenAlreadyCanceled_ShouldThrow_OperationCanceledException()
			{
				// Arrange
				var sut = createReader(text: "Test");

				// Act & Assert
				Assert.ThrowsAsync<OperationCanceledException>(
					async () => await sut.ReadNextElementAsync(
						cancellationToken: new System.Threading.CancellationToken(true)).ConfigureAwait(false));
			}

			[Test]
			public async Task ReadFromEmptyStream_ShouldReturn_Null()
			{
				// Arrange
				var sut = createReader(text: "");

				// Act
				var result = await sut.ReadNextElementAsync(cancellationToken: default).ConfigureAwait(false);

				// Assert
				result.Should().BeNull();
			}

			[Test]
			public void ReadNonHeaderElementFirst_ShouldThrow_InvalidOperationException()
			{
				// Arrange
				var sut = createReader(text: "EVENT" + Constants.RecordSeparator + "flag" + Constants.EntitySeparator);

				// Act & Assert
				Assert.ThrowsAsync<InvalidOperationException>(
					async () => await sut.ReadNextElementAsync(cancellationToken: default).ConfigureAwait(false));
			}

			[Test]
			public void ReadAbbreviatedHeaderElement_ShouldThrow_InvalidOperationException()
			{
				// Arrange
				var sut = createReader(text: Header<StandardLoglevel>.LogfileIdentity.Substring(3));

				// Act & Assert
				Assert.ThrowsAsync<InvalidOperationException>(
					async () => await sut.ReadNextElementAsync(cancellationToken: default).ConfigureAwait(false));
			}

			[Test]
			public async Task ReadCorrectHeaderElementOnly_Should_Succeed()
			{
				// Arrange
				var config = HeaderTest.CreateConfiguration();
				var misc = new Dictionary<string, string>{
					{ "key1", "value1" },
					{ "key2", "value2" },
					{ "key3", "value3" },
				};
				var header = new Header<StandardLoglevel>(
					appName: config.AppName,
					appStartUpTime: DateTime.Now,
					appInstanceSequenceNumber: 1,
					miscellaneous: misc);
				var sut = createReader(text: header.Serialize(config));

				// Act
				var result = await sut.ReadNextElementAsync(cancellationToken: default).ConfigureAwait(false);

				// Assert
				var parsedHeader = (Header<StandardLoglevel>)result;
				parsedHeader.AppName.Should().Be(header.AppName);
				parsedHeader.AppStartUpTime.Should().Be(header.AppStartUpTime.ToUniversalTime());
				parsedHeader.AppInstanceLogfileSequenceNumber.Should().Be(header.AppInstanceLogfileSequenceNumber);
				parsedHeader.Miscellaneous.Count.Should().Be(misc.Count);
				foreach (var kvp in misc)
				{
					parsedHeader.Miscellaneous[kvp.Key].Should().Be(kvp.Value);
				}

				// Act
				result = await sut.ReadNextElementAsync(cancellationToken: default).ConfigureAwait(false);

				// Assert
				result.Should().BeNull();
			}

			[Test]
			public async Task ReadCorrectHeaderElementWithMoreFollowing_Should_Succeed()
			{
				// Arrange
				var config = HeaderTest.CreateConfiguration();
				var header = new Header<StandardLoglevel>(
					appName: config.AppName,
					appStartUpTime: DateTime.Now,
					appInstanceSequenceNumber: 1,
					miscellaneous: new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
				var sut = createReader(text: header.Serialize(config) + "INVALID" + Constants.EntitySeparator + "INVALID");

				// Act
				var result = await sut.ReadNextElementAsync(cancellationToken: default).ConfigureAwait(false);

				// Assert
				var parsedHeader = (Header<StandardLoglevel>)result;
				parsedHeader.AppName.Should().Be(header.AppName);
				parsedHeader.AppStartUpTime.Should().Be(header.AppStartUpTime.ToUniversalTime());
				parsedHeader.AppInstanceLogfileSequenceNumber.Should().Be(header.AppInstanceLogfileSequenceNumber);

				// Act
				// TODO Further elements are to be implemeted later.
				result = await sut.ReadNextElementAsync(cancellationToken: default).ConfigureAwait(false);

				// Assert
				result.Should().BeNull();
			}

			[Test]
			public void ArbitraryExceptionWhileReadingFromStream_ShouldThrow_InvalidOperationException()
			{
				// Arrange
				var testException = new Exception();
				var sut = createReader(stream: new TestThrowingStream(ex: testException));

				// Act & Assert
				Assert.ThrowsAsync<InvalidOperationException>(
					async () => await sut.ReadNextElementAsync(cancellationToken: default));
			}

			[Test]
			public void IOExceptionExceptionWhileReadingFromStream_ShouldThrow_IOException()
			{
				// Arrange
				var testException = new IOException();
				var sut = createReader(stream: new TestThrowingStream(ex: testException));

				// Act & Assert
				Assert.ThrowsAsync<IOException>(
					async () => await sut.ReadNextElementAsync(cancellationToken: default));
			}

			[Test]
			public void OperationCanceledExceptionWhileReadingFromStream_ShouldThrow_OperationCanceledException()
			{
				// Arrange
				var testException = new OperationCanceledException();
				var sut = createReader(stream: new TestThrowingStream(ex: testException));

				// Act & Assert
				Assert.ThrowsAsync<OperationCanceledException>(
					async () => await sut.ReadNextElementAsync(cancellationToken: default));
			}

			class TestThrowingStream : Stream
			{
				Exception Exception { get; }
				public TestThrowingStream(Exception ex)
				{
					this.Exception = ex;
				}

				public override bool CanRead => true;

				public override bool CanSeek => throw new NotImplementedException();

				public override bool CanWrite => throw new NotImplementedException();

				public override long Length => throw new NotImplementedException();

				public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

				public override void Flush()
				{
					throw new NotImplementedException();
				}

				public override int Read(byte[] buffer, int offset, int count)
				{
					throw this.Exception;
				}

				public override long Seek(long offset, SeekOrigin origin)
				{
					throw new NotImplementedException();
				}

				public override void SetLength(long value)
				{
					throw new NotImplementedException();
				}

				public override void Write(byte[] buffer, int offset, int count)
				{
					throw new NotImplementedException();
				}
			}
		}
	}
}
