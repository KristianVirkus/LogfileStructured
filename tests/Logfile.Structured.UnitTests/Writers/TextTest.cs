﻿using FluentAssertions;
using Logfile.Structured.Elements;
using NUnit.Framework;
using System;
using System.IO;
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
			Assert.Throws<ArgumentNullException>(() => new Logfile.Structured.Writers.Text(null));
		}

		[Test]
		public async Task WriteText_Should_WriteTextToStream()
		{
			// Arrange
			using (var memoryStream = new MemoryStream())
			{
				using (var streamWriter = new StreamWriter(memoryStream))
				{
					using (var text = new Logfile.Structured.Writers.Text(streamWriter))
					{
						// Act
						await text.WriteAsync("test text", default);
						await streamWriter.FlushAsync();

						// Assert
						ContentEncoding.Encoding.GetString(memoryStream.ToArray()).Should().Be("test text");
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
					using (var text = new Logfile.Structured.Writers.Text(streamWriter))
					{
						// Act
						text.Dispose();

						await text.WriteAsync("test text", default);
						await streamWriter.FlushAsync();

						// Assert
						ContentEncoding.Encoding.GetString(memoryStream.ToArray()).Should().Be("test text");
					}
				}
			}
		}
	}
}
