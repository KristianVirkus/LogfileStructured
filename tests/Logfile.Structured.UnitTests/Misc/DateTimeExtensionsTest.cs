using FluentAssertions;
using Logfile.Structured.Misc;
using NUnit.Framework;
using System;

namespace Logfile.Structured.UnitTests.Misc
{
	class DateTimeExtensionsTest
	{
		public class Iso8601
		{
			public class WithDateTime
			{
				public class ToIso8601
				{
					[Test]
					public void UtcTime_Should_ConvertToZulu()
					{
						// Arrange
						var dt = new DateTime(2000, 01, 02, 12, 34, 56, 789, DateTimeKind.Utc);

						// Act
						var s = DateTimeExtensions.ToIso8601String(dt);

						// Assert
						s.Should().Be("2000-01-02T12:34:56.7890000Z");
					}

					[Test]
					public void LocalTime_Should_ConvertToOffset()
					{
						// Arrange
						var dt = new DateTime(2000, 01, 02, 12, 34, 56, 789, DateTimeKind.Local);

						// Act
						var s = DateTimeExtensions.ToIso8601String(dt);

						// Assert
						s.Should().StartWith("2000-01-02T12:34:56.7890000");
						TimeZoneInfo.Local.BaseUtcOffset.ToString().Should().StartWith(s.Substring(28));
					}

					[Test]
					public void UnspecifiedTime_Should_ConvertWithNoOffset()
					{
						// Arrange
						var dt = new DateTime(2000, 01, 02, 12, 34, 56, 789, DateTimeKind.Unspecified);

						// Act
						var s = DateTimeExtensions.ToIso8601String(dt);

						// Assert
						s.Should().Be("2000-01-02T12:34:56.7890000");
					}
				}

				public class ParseIso8601
				{
					[Test]
					public void ZuluTime_Should_ConvertToUtc()
					{
						// Arrange
						// Act
						var dt = DateTimeExtensions.ParseIso8601String("2000-01-02T12:34:56.7890000Z");

						// Assert
						dt.Kind.Should().Be(DateTimeKind.Utc);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void OffsetTime_Should_ConvertToLocal()
					{
						// Arrange
						// Act
						var dt = DateTimeExtensions.ParseIso8601String("2000-01-02T12:34:56.7890000+01:00");

						// Assert
						dt.Kind.Should().Be(DateTimeKind.Local);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void UnspecifiedTime_Should_ConvertToUnspecified()
					{
						// Arrange
						// Act
						var dt = DateTimeExtensions.ParseIso8601String("2000-01-02T12:34:56.7890000");

						// Assert
						dt.Kind.Should().Be(DateTimeKind.Unspecified);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void NullString_ShouldThrow_ArgumentNullException()
					{
						// Arrange
						// Act
						// Assert
						Assert.Throws<ArgumentNullException>(() => DateTimeExtensions.ParseIso8601String(null));
					}

					[Test]
					public void MalformedString_ShouldThrow_FormatException()
					{
						// Arrange
						// Act
						// Assert
						Assert.Throws<FormatException>(() => DateTimeExtensions.ParseIso8601String("malformed"));
					}
				}

				public class TryParseIso8601
				{
					[Test]
					public void ZuluTime_Should_ReturnTrueAndConvertToUtc()
					{
						// Arrange
						// Act
						var result = DateTimeExtensions.TryParseIso8601String("2000-01-02T12:34:56.7890000Z", out DateTime dt);

						// Assert
						result.Should().BeTrue();
						dt.Kind.Should().Be(DateTimeKind.Utc);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void OffsetTime_Should_ReturnTrueAndConvertToLocal()
					{
						// Arrange
						// Act
						var result = DateTimeExtensions.TryParseIso8601String("2000-01-02T12:34:56.7890000+01:00", out DateTime dt);

						// Assert
						result.Should().BeTrue();
						dt.Kind.Should().Be(DateTimeKind.Local);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void UnspecifiedTime_Should_ReturnTrueAndConvertToUnspecified()
					{
						// Arrange
						// Act
						var result = DateTimeExtensions.TryParseIso8601String("2000-01-02T12:34:56.7890000", out DateTime dt);

						// Assert
						result.Should().BeTrue();
						dt.Kind.Should().Be(DateTimeKind.Unspecified);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void NullString_ShouldReturn_False()
					{
						// Arrange
						// Act
						// Assert
						DateTimeExtensions.TryParseIso8601String(null, out DateTime _).Should().BeFalse();
					}

					[Test]
					public void MalformedString_ShouldReturn_False()
					{
						// Arrange
						// Act
						// Assert
						DateTimeExtensions.TryParseIso8601String("malformed", out DateTime _).Should().BeFalse();
					}
				}
			}

			public class WithDateTimeOffset
			{
				public class ToIso8601
				{
					[Test]
					public void ZeroOffsetTime_Should_ConvertToZeroOffset()
					{
						// Arrange
						var dt = new DateTimeOffset(2000, 01, 02, 12, 34, 56, 789, TimeSpan.Zero);

						// Act
						var s = DateTimeExtensions.ToIso8601String(dt);

						// Assert
						s.Should().Be("2000-01-02T12:34:56.7890000+00:00");
					}

					[Test]
					public void TimeWithOffset_Should_ConvertToOffset()
					{
						// Arrange
						var dt = new DateTimeOffset(2000, 01, 02, 12, 34, 56, 789, TimeSpan.FromHours(1));

						// Act
						var s = DateTimeExtensions.ToIso8601String(dt);

						// Assert
						s.Should().Be("2000-01-02T12:34:56.7890000+01:00");
					}
				}

				public class ParseIso8601
				{
					[Test]
					public void ZeroOffsetTime_Should_ConvertToZeroOffset()
					{
						// Arrange
						// Act
						var dt = DateTimeExtensions.ParseIso8601StringToDateTimeOffset("2000-01-02T12:34:56.7890000Z");

						// Assert
						dt.Offset.Should().Be(TimeSpan.Zero);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void TimeWithOffset_Should_ConvertToOffset()
					{
						// Arrange
						// Act
						var dt = DateTimeExtensions.ParseIso8601StringToDateTimeOffset("2000-01-02T12:34:56.7890000+01:00");

						// Assert
						dt.Offset.Should().Be(TimeSpan.FromHours(1));
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void UnspecifiedTime_Should_ConvertToLocalOffset()
					{
						// Arrange
						// Act
						var dt = DateTimeExtensions.ParseIso8601StringToDateTimeOffset("2000-01-02T12:34:56.7890000");

						// Assert
						dt.Offset.Should().Be(TimeZoneInfo.Local.BaseUtcOffset);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void NullString_ShouldThrow_ArgumentNullException()
					{
						// Arrange
						// Act
						// Assert
						Assert.Throws<ArgumentNullException>(() => DateTimeExtensions.ParseIso8601StringToDateTimeOffset(null));
					}

					[Test]
					public void MalformedString_ShouldThrow_FormatException()
					{
						// Arrange
						// Act
						// Assert
						Assert.Throws<FormatException>(() => DateTimeExtensions.ParseIso8601StringToDateTimeOffset("malformed"));
					}
				}

				public class TryParseIso8601
				{
					[Test]
					public void ZuluTime_Should_ReturnTrueAndConvertToZeroOffset()
					{
						// Arrange
						// Act
						var result = DateTimeExtensions.TryParseIso8601String("2000-01-02T12:34:56.7890000Z", out DateTimeOffset dt);

						// Assert
						result.Should().BeTrue();
						dt.Offset.Should().Be(TimeSpan.Zero);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void TimeWithOffset_Should_ReturnTrueAndConvertToOffset()
					{
						// Arrange
						// Act
						var result = DateTimeExtensions.TryParseIso8601String("2000-01-02T12:34:56.7890000+01:00", out DateTimeOffset dt);

						// Assert
						result.Should().BeTrue();
						dt.Offset.Should().Be(TimeSpan.FromHours(1));
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void UnspecifiedTime_Should_ReturnTrueAndConvertToLocalOffset()
					{
						// Arrange
						// Act
						var result = DateTimeExtensions.TryParseIso8601String("2000-01-02T12:34:56.7890000", out DateTimeOffset dt);

						// Assert
						result.Should().BeTrue();
						dt.Offset.Should().Be(TimeZoneInfo.Local.BaseUtcOffset);
						dt.Year.Should().Be(2000);
						dt.Month.Should().Be(1);
						dt.Day.Should().Be(2);
						dt.Hour.Should().Be(12);
						dt.Minute.Should().Be(34);
						dt.Second.Should().Be(56);
						dt.Millisecond.Should().Be(789);
					}

					[Test]
					public void NullString_ShouldReturn_False()
					{
						// Arrange
						// Act
						// Assert
						DateTimeExtensions.TryParseIso8601String(null, out DateTimeOffset _).Should().BeFalse();
					}

					[Test]
					public void MalformedString_ShouldReturn_False()
					{
						// Arrange
						// Act
						// Assert
						DateTimeExtensions.TryParseIso8601String("malformed", out DateTimeOffset _).Should().BeFalse();
					}
				}
			}
		}

		public class UnixTime
		{
			[Test]
			public void PreEpochTime_ShouldReturn_NegativeNumber()
			{
				// Arrange
				// Act
				var unixTime = DateTimeExtensions.ToUnixTime(DateTime.SpecifyKind(new DateTime(1969, 12, 31), DateTimeKind.Utc));

				// Assert
				unixTime.Should().Be(-86400);
			}

			[Test]
			public void Epoch_ShouldReturn_Zero()
			{
				// Arrange
				// Act
				var unixTime = DateTimeExtensions.ToUnixTime(DateTime.SpecifyKind(new DateTime(1970, 01, 01), DateTimeKind.Utc));

				// Assert
				unixTime.Should().Be(0);
			}

			[Test]
			public void OneDayAfterEpoch_ShouldReturn_86400()
			{
				// Arrange
				// Act
				var unixTime = DateTimeExtensions.ToUnixTime(DateTime.SpecifyKind(new DateTime(1970, 01, 02), DateTimeKind.Utc));

				// Assert
				unixTime.Should().Be(86400);
			}

			[Test]
			public void Minus86400_ShouldReturn_OneDayAfterEpoch()
			{
				// Arrange
				// Act
				var dt = DateTimeExtensions.FromUnixTime(-86400);

				// Assert
				dt.Year.Should().Be(1969);
				dt.Month.Should().Be(12);
				dt.Day.Should().Be(31);
			}

			[Test]
			public void Zero_ShouldReturn_Epoch()
			{
				// Arrange
				// Act
				var dt = DateTimeExtensions.FromUnixTime(0);

				// Assert
				dt.Year.Should().Be(1970);
				dt.Month.Should().Be(01);
				dt.Day.Should().Be(01);
			}

			[Test]
			public void Plus86400_ShouldReturn_OneDayAfterEpoch()
			{
				// Arrange
				// Act
				var dt = DateTimeExtensions.FromUnixTime(86400);

				// Assert
				dt.Year.Should().Be(1970);
				dt.Month.Should().Be(01);
				dt.Day.Should().Be(02);
			}
		}
	}
}
