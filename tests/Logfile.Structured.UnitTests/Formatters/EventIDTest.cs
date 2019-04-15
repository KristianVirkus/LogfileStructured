using FluentAssertions;
using Logfile.Structured.Formatters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Logfile.Structured.UnitTests.Formatters
{
	class EventIDTest
	{
		[Test]
		public void ID_Should_BeEventID()
		{
			// Arrange
			// Act
			// Assert
			EventID.Identification.Should().Be("EventID");
		}

		[Test]
		public void FormatNull_ShouldThrow_ArgumentNullException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentNullException>(() => EventID.Default.Format(null));
		}

		[Test]
		public void FormatUnsupportedData_ShouldThrow_NotSupportedException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<NotSupportedException>(() => EventID.Default.Format(123));
		}

		[Test]
		public void FormatEvent_Should_CombineNumbersAndNames()
		{
			// Arrange
			var detail = new Logfile.Core.Details.EventID(new[] { "a", "b" }, new[] { 1, 2 });

			// Act
			var s = EventID.Default.Format(detail);

			// Assert
			s.Should().Be(detail.ToString());
		}
	}
}
