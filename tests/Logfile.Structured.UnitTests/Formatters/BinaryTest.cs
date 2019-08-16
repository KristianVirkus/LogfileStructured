using FluentAssertions;
using Logfile.Structured.Formatters;
using NUnit.Framework;
using System;
using System.Text;

namespace Logfile.Structured.UnitTests.Formatters
{
	class BinaryTest
	{
		[Test]
		public void ID_Should_BeBinary()
		{
			// Arrange
			// Act
			// Assert
			Binary.Identification.Should().Be("Binary");
		}

		[Test]
		public void FormatNull_ShouldThrow_ArgumentNullException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentNullException>(() => Binary.Default.Format(null));
		}

		[Test]
		public void FormatUnsupportedData_ShouldThrow_NotSupportedException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<NotSupportedException>(() => Binary.Default.Format(123));
		}

		[Test]
		public void FormatEvent_Should_CombineNumbersAndNames()
		{
			// Arrange
			var detail = new Logfile.Core.Details.Binary(Encoding.UTF8.GetBytes("Test"));

			// Act
			var s = Binary.Default.Format(detail);

			// Assert
			s.Should().Contain("Test");
		}

		[Test]
		public void FormatEventWithWithBacktickInTranscript_Should_TreatBacktickAsControlCharacter()
		{
			// Arrange
			var detail = new Logfile.Core.Details.Binary(Encoding.UTF8.GetBytes("test`d"));

			// Act.
			var s = Binary.Default.Format(detail);

			// Assert
			s.Should().Contain("test.d");
		}
	}
}
