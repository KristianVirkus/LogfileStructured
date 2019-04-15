using FluentAssertions;
using Logfile.Structured.Formatters;
using NUnit.Framework;
using System;

namespace Logfile.Structured.UnitTests.Formatters
{
	class MessageTest
	{
		[Test]
		public void ID_Should_BeMessage()
		{
			// Arrange
			// Act
			// Assert
			Message.Default.ID.Should().Be("Message");
		}

		[Test]
		public void FormatNull_ShouldThrow_ArgumentNullException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentNullException>(() => Message.Default.Format(null));
		}

		[Test]
		public void FormatUnsupportedData_ShouldThrow_NotSupportedException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<NotSupportedException>(() => Message.Default.Format(123));
		}

		[Test]
		public void FormatSomeText_Should_IncludeText()
		{
			// Arrange
			var detail = new Logfile.Core.Details.Message("Message text", null);

			// Act
			var s = Message.Default.Format(detail);

			// Assert
			s.Should().Be("Message text");
		}

		[Test]
		public void FormatMultiLineText_Should_RemainMultiLined()
		{
			// Arrange
			var detail = new Logfile.Core.Details.Message("Message\ntext", null);

			// Act
			var s = Message.Default.Format(detail);

			// Assert
			s.Should().Be("Message\ntext");
		}

		[Test]
		public void FormatMultiLineTextWithCarriageReturnsAndNewLines_Should_KeepCarriageReturnsAndNewLines()
		{
			// Arrange
			var detail = new Logfile.Core.Details.Message("Message\r\ntext", null);

			// Act
			var s = Message.Default.Format(detail);

			// Assert
			s.Should().Be("Message\r\ntext");
		}
	}
}
