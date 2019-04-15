using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Logfile.Structured.UnitTests.Writers
{
	class TextTest
	{
		[Test]
		public void ConstructorTextWriterNull_ShouldThrow_ArgumentNullException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentNullException>(() => new StreamWriters.Text(null));
		}

		[Test]
		public async Task WriteText_Should_WriteTextToStream()
		{
			// Arrange
			using (var memoryStream = new MemoryStream())
			{
				using (var streamWriter = new StreamWriter(memoryStream))
				{
					using (var text = new StreamWriters.Text(streamWriter))
					{
						// Act
						await text.WriteAsync("test text", default);
						await streamWriter.FlushAsync();

						// Assert
						Encoding.UTF8.GetString(memoryStream.ToArray()).Should().Be("test text");
					}
				}
			}
		}

		[Test]
		public async Task Dispose_Should_NotDisposeTextWriter()
		{
			// Arrange
			using (var memoryStream = new MemoryStream())
			{
				using (var streamWriter = new StreamWriter(memoryStream))
				{
					using (var text = new StreamWriters.Text(streamWriter))
					{
						// Act
						text.Dispose();

						await text.WriteAsync("test text", default);
						await streamWriter.FlushAsync();

						// Assert
						Encoding.UTF8.GetString(memoryStream.ToArray()).Should().Be("test text");
					}
				}
			}
		}
	}
}
