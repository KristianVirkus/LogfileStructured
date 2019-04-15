using FluentAssertions;
using Logfile.Structured.Formatters;
using NUnit.Framework;
using System;

namespace Logfile.Structured.UnitTests.Formatters
{
	class ExceptionDetailTest
	{
		[Test]
		public void ID_Should_BeException()
		{
			// Arrange
			// Act
			// Assert
			ExceptionDetail.Default.ID.Should().Be("Exception");
		}

		[Test]
		public void FormatNull_ShouldThrow_ArgumentNullException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentNullException>(() => ExceptionDetail.Default.Format(null));
		}

		[Test]
		public void FormatUnsupportedData_ShouldThrow_NotSupportedException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<NotSupportedException>(() => ExceptionDetail.Default.Format(123));
		}

		[Test]
		public void FormatSomeException_Should_IncludeExceptionTypeAndMessageAndStackTrace()
		{
			// Arrange
			Exception exception;
			try
			{
				throw new InvalidOperationException("Exception text");
			}
			catch (Exception ex)
			{
				exception = ex;
			}

			var detail = new Logfile.Core.Details.ExceptionDetail(exception);

			// Act
			var s = ExceptionDetail.Default.Format(detail);

			// Assert
			s.Should().Contain(exception.GetType().Name);
			s.Should().Contain(exception.Message);
			s.Should().Contain("at ");
		}

		[Test]
		public void FormatMultiLineMessageWithCarriageReturnsAndNewLines_Should_KeepCarriageReturnsAndNewLines()
		{
			// Arrange
			Exception exception;
			try
			{
				throw new InvalidOperationException("Exception\r\ntext");
			}
			catch (Exception ex)
			{
				exception = ex;
			}

			var detail = new Logfile.Core.Details.ExceptionDetail(exception);

			// Act
			var s = ExceptionDetail.Default.Format(detail);

			// Assert
			s.Should().Contain(exception.Message);
		}

		[Test]
		public void FormatWithNestedExceptions_Should_IncludeAllNestedExceptions()
		{
			// Arrange
			Exception nested2;
			Exception nested1;
			Exception exception;

			try
			{
				throw new ArgumentException("Nested ArgumentException");
			}
			catch (Exception ex)
			{
				nested2 = ex;
			}

			try
			{
				throw new NotSupportedException("Nested NotSupportedException", nested2);
			}
			catch (Exception ex)
			{
				nested1 = ex;
			}

			try
			{
				throw new InvalidOperationException("Exception message", nested1);
			}
			catch (Exception ex)
			{
				exception = ex;
			}

			var detail = new Logfile.Core.Details.ExceptionDetail(exception);

			// Act
			var s = ExceptionDetail.Default.Format(detail);

			// Assert
			s.Should().Contain(exception.Message);
			s.Should().Contain(nested1.Message);
			s.Should().Contain(nested2.Message);
		}
	}
}
