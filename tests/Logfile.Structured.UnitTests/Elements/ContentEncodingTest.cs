using FluentAssertions;
using Logfile.Structured.Elements;
using NUnit.Framework;
using System;
using System.Linq;

namespace Logfile.Structured.UnitTests.Elements
{
	class ContentEncodingTest
	{
		public class Encode
		{
			[Test]
			public void StringNull_ShouldThrow_ArgumentNullException()
			{
				Assert.Throws<ArgumentNullException>(() => ContentEncoding.Encode(null));
			}

			[Test]
			public void SimpleString_Should_KeepSimpleString()
			{
				ContentEncoding.Encode("test").Should().Be("test");
			}

			[Test]
			public void Utf8String_Should_KeepUtf8Characters()
			{
				ContentEncoding.Encode("\u1111\u2222").Should().Be("\u1111\u2222");
			}

			[Test]
			public void PerCentSign_Should_BeEncoded()
			{
				ContentEncoding.Encode("100% completed").Should().Be("100%25 completed");
			}

			[Test]
			public void QuotationMarkSign_Should_NotBeEncoded()
			{
				ContentEncoding.Encode("Some \"quotation\"").Should().Be("Some \"quotation\"");
			}

			[Test]
			public void AdditionalBacktickSign_Should_BeEncoded()
			{
				ContentEncoding.Encode("Some `backticks`", (byte)'`').Should().Be("Some %60backticks%60");
			}

			[Test]
			public void MultipleLines_Should_KeepMultipleLines()
			{
				ContentEncoding.Encode(@"multi-line
text
value").Should().Be(@"multi-line
text
value");
			}

			[Test]
			public void SpaceAndTabsCharacters_Should_KeepSpaceAndTabCharacters()
			{
				ContentEncoding.Encode("tab\tand space").Should().Be("tab\tand space");
			}

			[Test]
			public void AlreadyEncodedString_Should_EncodeAgain()
			{
				ContentEncoding.Encode("100%25 completed").Should().Be("100%2525 completed");
			}
		}

		public class Decode
		{
			[Test]
			public void StringNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(() => ContentEncoding.Decode(s: null));
			}

			[Test]
			public void StringEmpty_ShouldReturn_EmptyString()
			{
				// Arrange
				// Act
				// Assert
				ContentEncoding.Decode(s: "").Should().Be("");
			}

			[Test]
			public void StringWithoutEscapeSequences_ShouldReturn_String()
			{
				// Arrange
				var s = "This is a \"test\".";

				// Act
				var decoded = ContentEncoding.Decode(s: s);

				// Assert
				decoded.Should().Be(s);
			}

			[Test]
			public void StringWithPerCentEscaped_Should_DecodeToPerCent()
			{
				// Arrange
				var s = "Done 100%.";

				// Act
				var decoded = ContentEncoding.Decode(s: ContentEncoding.Encode(s: s));

				// Assert
				decoded.Should().Be(s);
			}

			[Test]
			public void StringWithAdditionallyEncodedCharacters_Should_DecodeCorrectly()
			{
				// Arrange
				var s = "This is an `encoding` example with some @dditional %%char%% to be encoded and some n#t.";

				// Act
				var decoded = ContentEncoding.Decode(s: ContentEncoding.Encode(s: s, (byte)'`', (byte)'@'));

				// Assert
				decoded.Should().Be(s);
			}

			[Test]
			public void IncompleteEscapeSequenceAtStringEnding_ShouldThrow_FormatException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<FormatException>(() => ContentEncoding.Decode(s: "Test %1"));
			}

			[TestCase("Test %% Test")]
			[TestCase("Test %xx Test")]
			[TestCase("Test %")]
			[TestCase("Test %1")]
			public void InvalidEscapeSequenceAtStringEnding_ShouldThrow_FormatException(string s)
			{
				// Arrange
				// Act & Assert
				Assert.Throws<FormatException>(() => ContentEncoding.Decode(s: s));
			}
		}

		public class GetLines
		{
			[Test]
			public void Null_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act
				// Assert
				Assert.Throws<ArgumentNullException>(() => ContentEncoding.GetLines(null));
			}

			[Test]
			public void Empty_ShouldReturn_SingleEmptyLine()
			{
				// Arrange
				// Act
				var lines = ContentEncoding.GetLines("");

				// Assert
				lines.Single().Should().BeEmpty();
			}

			[Test]
			public void SingleLine_ShouldReturn_SingleLine()
			{
				// Arrange
				// Act
				var lines = ContentEncoding.GetLines("Single line text");

				// Assert
				lines.Single().Should().Be("Single line text");
			}

			[Test]
			public void TwoEmptyLines_ShouldReturn_TwoEmptyLines()
			{
				// Arrange
				// Act
				var lines = ContentEncoding.GetLines("\n");

				// Assert
				lines.Count().Should().Be(2);
				lines.First().Should().BeEmpty();
				lines.Last().Should().BeEmpty();
			}

			[Test]
			public void EmptyLineBeforeAndAfterText_Should_KeepEmptyLines()
			{
				// Arrange
				// Act
				var lines = ContentEncoding.GetLines("\nSecond line\n");

				// Assert
				lines.Count().Should().Be(3);
				lines.ElementAt(0).Should().BeEmpty();
				lines.ElementAt(1).Should().Be("Second line");
				lines.ElementAt(2).Should().BeEmpty();
			}

			[Test]
			public void ThreeLines_ShouldReturn_ThreeLines()
			{
				// Arrange
				// Act
				var lines = ContentEncoding.GetLines("First\nSecond\nThird");

				// Assert
				lines.Count().Should().Be(3);
				lines.ElementAt(0).Should().Be("First");
				lines.ElementAt(1).Should().Be("Second");
				lines.ElementAt(2).Should().Be("Third");
			}

			[Test]
			public void MixedLineBreaks_Should_ReduceLineBreaks()
			{
				// Arrange
				// Act
				var lines = ContentEncoding.GetLines("\ra\nb\r\n\r\nc\n\r\n\rd\r\re\n\nf");

				// Assert
				lines.Count().Should().Be(12);
				lines.ElementAt(0).Should().BeEmpty();
				lines.ElementAt(1).Should().Be("a");
				lines.ElementAt(2).Should().Be("b");
				lines.ElementAt(3).Should().BeEmpty();
				lines.ElementAt(4).Should().Be("c");
				lines.ElementAt(5).Should().BeEmpty();
				lines.ElementAt(6).Should().BeEmpty();
				lines.ElementAt(7).Should().Be("d");
				lines.ElementAt(8).Should().BeEmpty();
				lines.ElementAt(9).Should().Be("e");
				lines.ElementAt(10).Should().BeEmpty();
				lines.ElementAt(11).Should().Be("f");
			}
		}
	}
}
