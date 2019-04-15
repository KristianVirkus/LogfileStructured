using FluentAssertions;
using Logfile.Core.Details;
using Logfile.Structured.Misc;
using NUnit.Framework;
using System;
using System.Text;

namespace Logfile.Structured.UnitTests.Details
{
	class BinaryStringifierTest
	{
		string stringify(
			string dataString = "0123456789abcdef0123456789abcdef",
			int offset = 0,
			int length = 32,
			int bytesPerRow = 16,
			bool includeAddresses = true,
			bool includeHex = true,
			bool includeTranscript = true,
			char nonPrintableCharacterSubstitute = '.')
		{
			return BinaryStringifier.Stringify(Encoding.UTF8.GetBytes(dataString), offset,
				length, bytesPerRow, includeAddresses, includeHex, includeTranscript,
				nonPrintableCharacterSubstitute);
		}

		[Test]
		public void DataNull_ShouldThrow_ArgumentNullException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentNullException>(() => this.stringify(dataString: null));
		}

		[Test]
		public void OffsetNegative_ShouldThrow_ArgumentOutOfRangeException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => this.stringify(offset: -1));
		}

		[Test]
		public void OffsetExceedsDataLength_ShouldThrow_ArgumentOutOfRangeException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => this.stringify(offset: 64));
		}

		[Test]
		public void LengthNegative_ShouldThrow_ArgumentOutOfRangeException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => this.stringify(length: -1));
		}

		[Test]
		public void LengthZero_ShouldThrow_ArgumentOutOfRangeException()
		{
			// Arrange
			// Act
			// Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => this.stringify(length: 0));
		}

		[Test]
		public void LengthGreaterThanActualDataLength_Should_Ignore()
		{
			// Arrange
			// Act
			this.stringify(length: 100);
			// Assert
		}

		[Test]
		public void StringifyWith2BytesWidth_Should_Format()
		{
			// Arrange
			// Act
			var s = this.stringify(dataString: "0123", length: 4, bytesPerRow: 2);

			// Assert
			s.Should().Contain("    0  1");
			s.Should().Contain("00: 30 31 01");
			s.Should().Contain("02: 32 33 23");
		}

		[Test]
		public void StringifyWith64BytesWidth_Should_Format()
		{
			// Arrange
			// Act
			var s = this.stringify(dataString: "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", length: 64, bytesPerRow: 64);

			// Assert
			s.Should().Contain("    00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A 1B ");
			s.Should().Contain(" 0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
		}

		[Test]
		public void Stringify_Should_UseReplacementCharacterForControlCharacters()
		{
			// Arrange
			// Act
			var s = this.stringify(dataString: "x\0\ny", length: 4, nonPrintableCharacterSubstitute: '.');

			// Assert
			s.Should().Contain("    0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F");
			s.Should().Contain("00: 78 00 0A 79 " + "".PadRight(12 * 3, ' ') + "x..y");
		}
	}
}
