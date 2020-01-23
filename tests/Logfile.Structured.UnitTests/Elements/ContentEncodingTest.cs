using FluentAssertions;
using Logfile.Structured.Elements;
using NUnit.Framework;
using System;
using System.Linq;

namespace Logfile.Structured.UnitTests.Elements
{
	static class ContentEncodingTest
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

		public class ParseKeyValuePair
		{
			static string b2s(byte[] data) => ContentEncoding.Encoding.GetString(data);
			static byte[] s2b(string s) => ContentEncoding.Encoding.GetBytes(s);

			[Test]
			public void DataNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(() => ContentEncoding.ParseKeyValuePair(data: null));
			}

			[Test]
			public void UnquotedKeyOnly_ShouldReturn_KeyAndValueNull()
			{
				// Arrange
				var data = s2b("my-key");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("my-key");
				kvp.Value.Should().BeNull();
			}

			[Test]
			public void QuotedKeyOnly_ShouldReturn_KeyAndValueNull()
			{
				// Arrange
				var data = s2b("`my-key`");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("my-key");
				kvp.Value.Should().BeNull();
			}

			[Test]
			public void UnquotedKeyWithWhiteSpaces_ShouldReturn_KeyAndValueNull()
			{
				// Arrange
				var data = s2b("my key");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("my key");
				kvp.Value.Should().BeNull();
			}

			[Test]
			public void UnquotedKeyWithLeadingAndTrailingWhiteSpaces_ShouldReturn_TrimmedKeyAndValueNull()
			{
				// Arrange
				var data = s2b("\t \nmy key\t \n");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("my key");
				kvp.Value.Should().BeNull();
			}

			[Test]
			public void QuotedKeyWithWhiteSpaces_ShouldReturn_NonTrimmedKeyAndValueNull()
			{
				// Arrange
				var data = s2b("`\t\n my key\t\n `");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("\t\n my key\t\n ");
				kvp.Value.Should().BeNull();
			}

			[Test]
			public void QuotedKeyAndQuotedValue_ShouldReturn_KeyAndValue()
			{
				// Arrange
				var data = s2b("`key`=`value`");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("key");
				b2s(kvp.Value).Should().Be("value");
			}

			[Test]
			public void UnquotedKeyAndUnquotedValue_ShouldReturn_KeyAndValue()
			{
				// Arrange
				var data = s2b("key=value");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("key");
				b2s(kvp.Value).Should().Be("value");
			}

			[Test]
			public void UnquotedKeyAndUnquotedValueBothWithLeadingAndTrailingWhiteSpaces_ShouldReturn_TrimmedKeyAndValueNull()
			{
				// Arrange
				var data = s2b("\t \nmy key\t \n=\t \nmy value\t \n");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("my key");
				b2s(kvp.Value).Should().Be("my value");
			}

			[Test]
			public void QuotedKeyAndQuotedValueWithWhiteSpacesInBetweenTheQuotationMarksAndTheAssignmentCharacter_ShouldReturn_KeyAndValue()
			{
				// Arrange
				var data = s2b("`key`\t\n =\t\n `value`");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("key");
				b2s(kvp.Value).Should().Be("value");
			}

			[Test]
			public void QuotedKeyAndQuotedValueWithWhiteSpacesAroundKeyAndValue_ShouldReturn_KeyAndValue()
			{
				// Arrange
				var data = s2b("\t\n `key`\t\n =\t\n `value`\t\n ");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("key");
				b2s(kvp.Value).Should().Be("value");
			}

			[Test]
			public void UnquotedKeyAndQuotedValue_ShouldReturn_KeyAndValue()
			{
				// Arrange
				var data = s2b("key=`value`");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("key");
				b2s(kvp.Value).Should().Be("value");
			}

			[Test]
			public void QuotedKeyAndUnquotedValue_ShouldReturn_KeyAndValue()
			{
				// Arrange
				var data = s2b("`key`=value");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("key");
				b2s(kvp.Value).Should().Be("value");
			}

			[Test]
			public void EmptyKeyAndUnquotedValue_ShouldReturn_TreatValueAsKeyDueToTrimming()
			{
				// Arrange
				var data = s2b("=value");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("value");
				kvp.Value.Should().BeNull();
			}

			[Test]
			public void KeyEmptyAndQuotedValue_ShouldReturn_TreatValueAsKeyDueToTrimming()
			{
				// Arrange
				var data = s2b("=`value`");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("value");
				kvp.Value.Should().BeNull();
			}

			[Test]
			public void KeyEmptyAndValueEmpty_ShouldReturn_KeyEmptyAndValueEmpty()
			{
				// Arrange
				var data = s2b("=");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("");
				b2s(kvp.Value).Should().Be("");
			}

			[Test]
			public void QuotedKeyEmptyAndQuotedValueEmpty_ShouldReturn_KeyEmptyAndValueEmpty()
			{
				// Arrange
				var data = s2b("``=``");

				// Act
				var kvp = ContentEncoding.ParseKeyValuePair(data: data);

				// Assert
				b2s(kvp.Key).Should().Be("");
				b2s(kvp.Value).Should().Be("");
			}

			[Test]
			public void NonWhiteSpaceCharactersBeforeQuotedKeyOnly_ShouldThrow_FormatException()
			{
				// Arrange
				var data = s2b("abc`key`");

				// Act & Assert
				Assert.Throws<FormatException>(() => ContentEncoding.ParseKeyValuePair(data: data));
			}

			[Test]
			public void NonWhiteSpaceCharactersAfterQuotedKeyOnly_ShouldThrow_FormatException()
			{
				// Arrange
				var data = s2b("`key`abc");

				// Act & Assert
				Assert.Throws<FormatException>(() => ContentEncoding.ParseKeyValuePair(data: data));
			}

			[Test]
			public void NonWhiteSpaceCharactersBeforeQuotedValueOnly_ShouldThrow_FormatException()
			{
				// Arrange
				var data = s2b("`key`=abc`value`");

				// Act & Assert
				Assert.Throws<FormatException>(() => ContentEncoding.ParseKeyValuePair(data: data));
			}

			[Test]
			public void NonWhiteSpaceCharactersAfterQuotedValueOnly_ShouldThrow_FormatException()
			{
				// Arrange
				var data = s2b("`key`=`value`abc");

				// Act & Assert
				Assert.Throws<FormatException>(() => ContentEncoding.ParseKeyValuePair(data: data));
			}

			[Test]
			public void NonWhiteSpaceCharactersAfterQuotedKeyAndAssignment_ShouldThrow_FormatException()
			{
				// Arrange
				var data = s2b("`key`abc=`value`");

				// Act & Assert
				Assert.Throws<FormatException>(() => ContentEncoding.ParseKeyValuePair(data: data));
			}

			[TestCase("`key`=`value``")]
			[TestCase("``key`=`value`")]
			[TestCase("`key``=`value`")]
			[TestCase("`key`=``value`")]
			[TestCase("`ke``y`=`value`")]
			[TestCase("`key`=`val``ue`")]
			public void InvalidNumberOfQuotationMarks_ShouldThrow_FormatException(string s)
			{
				// Arrange
				var data = s2b(s);

				// Act & Assert
				Assert.Throws<FormatException>(() => ContentEncoding.ParseKeyValuePair(data: data));
			}
		}

		public class SplittingRecords
		{
			[Test]
			public void DataNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(
					() => ContentEncoding.SplitRecords(data: null, offset: 0));
			}

			[Test]
			public void OffsetNegative_ShouldThrow_ArgumentOutOfRangeException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentOutOfRangeException>(
					() => ContentEncoding.SplitRecords(data: new byte[0], offset: -1));
			}

			[Test]
			public void OffsetBeyondEndOfData_ShouldThrow_ArgumentOutOfRangeException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentOutOfRangeException>(
					() => ContentEncoding.SplitRecords(data: new byte[0], offset: 0));
				Assert.Throws<ArgumentOutOfRangeException>(
					() => ContentEncoding.SplitRecords(data: new byte[1], offset: 1));
			}

			[Test]
			public void SplitSingleRecordWithoutSeparators_Should_ReportNoRecordsAllBytesConsumedAndIncomplete()
			{
				// Arrange
				var data = ContentEncoding.Encoding.GetBytes("incomplete");

				// Act
				var result = ContentEncoding.SplitRecords(
					data: data,
					offset: 0);

				// Assert
				result.Records.Any().Should().BeFalse();
				result.ConsumedData.Should().Be("incomplete".Length);
				result.EntityComplete.Should().BeFalse();
			}

			[Test]
			public void SplitSingleRecordWithOnlyEntitySeparator_Should_ReportSingleRecordAndAllDataConsumedAndComplete()
			{
				// Arrange
				var data = ContentEncoding.Encoding.GetBytes("test" + Constants.EntitySeparator);

				// Act
				var result = ContentEncoding.SplitRecords(
					data: data,
					offset: 0);

				// Assert
				ContentEncoding.Encoding.GetString(result.Records.Single()).Should().Be("test");
				result.ConsumedData.Should().Be(("test" + Constants.EntitySeparator).Length);
				result.EntityComplete.Should().BeTrue();
			}

			[Test]
			public void SingleRecordWithRecordSeparator_Should_ReportSingleRecordAllDataConsumedAndIncomplete()
			{
				// Arrange
				var data = ContentEncoding.Encoding.GetBytes("test" + Constants.RecordSeparator);

				// Act
				var result = ContentEncoding.SplitRecords(
					data: data,
					offset: 0);

				// Assert
				ContentEncoding.Encoding.GetString(result.Records.Single()).Should().Be("test");
				result.ConsumedData.Should().Be(("test" + Constants.RecordSeparator).Length);
				result.EntityComplete.Should().BeFalse();
			}

			[Test]
			public void RandomOffsetToCompleteEntity_Should_ReportSingleRecordAndRealDataConsumedAndComplete()
			{
				// Arrange
				var data = ContentEncoding.Encoding.GetBytes("abc" + Constants.EntitySeparator + "test" + Constants.EntitySeparator);

				// Act
				var result = ContentEncoding.SplitRecords(
					data: data,
					offset: ("abc" + Constants.EntitySeparator).Length);

				// Assert
				ContentEncoding.Encoding.GetString(result.Records.Single()).Should().Be("test");
				result.ConsumedData.Should().Be("test".Length + Constants.EntitySeparator.Length);
				result.EntityComplete.Should().BeTrue();
			}

			[Test]
			public void SplitMultipleEntities_Should_ReportFirstEntityRecordsOnlyAndRealDataConsumedAndComplete()
			{
				// Arrange
				var data = ContentEncoding.Encoding.GetBytes("test" + Constants.RecordSeparator + "test2" + Constants.EntitySeparator + "abc" + Constants.EntitySeparator);

				// Act
				var result = ContentEncoding.SplitRecords(
					data: data,
					offset: 0);

				// Assert
				ContentEncoding.Encoding.GetString(result.Records.First()).Should().Be("test");
				ContentEncoding.Encoding.GetString(result.Records.Last()).Should().Be("test2");
				result.ConsumedData.Should().Be(("test" + Constants.RecordSeparator + "test2" + Constants.EntitySeparator).Length);
				result.EntityComplete.Should().BeTrue();
			}
		}

		public class TrimmingData
		{
			[Test]
			public void DataNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(
					() => ContentEncoding.TrimData(data: null, charactersToTrim: new[] { (byte)' ' }));
			}

			[Test]
			public void CharactersToTrimNull_ShouldThrow_ArgumentNullException()
			{
				// Arrange
				// Act & Assert
				Assert.Throws<ArgumentNullException>(
					() => ContentEncoding.TrimData(data: ContentEncoding.Encoding.GetBytes("test"), charactersToTrim: null));
			}

			[Test]
			public void SingleCharacterToTrim_Should_RemoveChosenCharactersAtBeginningAndEnd()
			{
				// Arrange
				var charactersToTrim = new[] { (byte)'\n' };
				var data = ContentEncoding.Encoding.GetBytes("\n test abc \n");

				// Act
				var trimmedData = ContentEncoding.TrimData(data: data, charactersToTrim: charactersToTrim);

				// Assert
				trimmedData.SequenceEqual(ContentEncoding.Encoding.GetBytes(" test abc ")).Should().BeTrue();
			}

			[Test]
			public void MultipleCharactersToTrim_Should_RemoveChosenCharactersAtBeginningAndEnd()
			{
				// Arrange
				var charactersToTrim = new[] { (byte)'\n', (byte)' ' };
				var data = ContentEncoding.Encoding.GetBytes("\n test abc \n");

				// Act
				var trimmedData = ContentEncoding.TrimData(data: data, charactersToTrim: charactersToTrim);

				// Assert
				trimmedData.SequenceEqual(ContentEncoding.Encoding.GetBytes("test abc")).Should().BeTrue();
			}
		}
	}
}
