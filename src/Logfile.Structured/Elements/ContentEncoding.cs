using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logfile.Structured.Elements
{
	/// <summary>
	/// Helps encoding and decoding file contents.
	/// </summary>
	public static class ContentEncoding
	{
		public static readonly Encoding Encoding = Encoding.UTF8;

		/// <summary>
		/// Encodes a string to not contain any control characters or
		/// quotation marks ("). The encoding resembles the URI encoding
		/// scheme and is compatible to it when it comes to decoding but
		/// this encoding implementation does not encode all characters
		/// which would get encoded by URI encode to keep human readability.
		/// </summary>
		/// <param name="s">The string to encode, not null.</param>
		/// <param name="additionalCharactersToEscape">The list of additional
		///		characters to escape.</param>
		/// <returns>The encoded <paramref name="s"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown if
		///		<paramref name="s"/> is null.</exception>
		public static string Encode(string s, params byte[] additionalCharactersToEscape)
		{
			if (s == null) throw new ArgumentNullException(nameof(s));

			var escapeChars = new List<byte>(new[] { (byte)'%' }.Union(additionalCharactersToEscape ?? new byte[0]));
			var excludedChars = new[] { (byte)'\t', (byte)'\r', (byte)'\n' };
			for (byte b = 0; b < 0x1f; b++)
			{
				if (!excludedChars.Contains(b)) escapeChars.Add(b);
			}

			foreach (var b in escapeChars.Distinct())
			{
				s = s.Replace($"{(char)b}", $"%{b.ToString($"X2")}");
			}

			return s;
		}

		/// <summary>
		/// Decodes an escaped string. The encoding resembles the URI encoding
		/// scheme and is compatible to it when it comes to decoding
		/// </summary>
		/// <param name="s">The encoded string.</param>
		/// <returns>The decoded string.</returns>
		/// <exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="s"/> is null.</exception>
		///	<exception cref="FormatException">Thrown, if
		///		<paramref name="s"/> contains an incomplete or invalid escape code.</exception>
		public static string Decode(string s)
		{
			if (s == null) throw new ArgumentNullException(nameof(s));

			const char Escape = '%';
			var sb = new StringBuilder();
			int i = 0;
			while (i < s.Length)
			{
				var ch = s[i];
				if (ch != Escape)
				{
					sb.Append(ch);
					++i;
				}
				else
				{
					if (s.Length < i + 2 + 1) throw new FormatException("Invalid string format.");
					var escapeCode = s.Substring(i + 1, 2);
					var escapedChar = (char)byte.Parse(escapeCode, System.Globalization.NumberStyles.HexNumber);
					sb.Append(escapedChar);
					i += 3;
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Splits a string with possible line-breaks into multiple strings, each
		/// representing a single line. Line feeds (LF) and carriage returns (CR)
		/// are considered line breaks. All combinations of CR+LF are first replaced
		/// by a single LFs.
		/// </summary>
		/// <param name="s">The string.</param>
		/// <returns>All lines from <paramref name="s"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown if
		///		<paramref name="s"/> is null.</exception>
		public static IEnumerable<string> GetLines(string s)
		{
			if (s == null) throw new ArgumentNullException(nameof(s));

			s = s.Replace("\r\n", "\n").Replace("\r", "\n");
			return s.Split('\n');
		}

		/// <summary>
		/// Parses a record as key-value-pair.
		/// </summary>
		/// <param name="data">The record data, may still be beautified.</param>
		/// <returns>The parsed key and value.</returns>
		/// <exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="data"/> is null.</exception>
		/// <exception cref="FormatException">Thrown, if
		///		<paramref name="data"/> does not contain valid key-value-pair data.</exception>
		public static (byte[] Key, byte[] Value) ParseKeyValuePair(byte[] data)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));

			// Trim data first.
			var charactersToTrim = (ContentEncoding.Encoding ?? Encoding.UTF8).GetBytes(Constants.IgnoredAfterRecordSeparators);
			data = TrimData(data: data, charactersToTrim: charactersToTrim);

			// Find quotation signs.
			var QuotationMarkIndexes = findIndexes(data: data, b: (byte)Constants.QuotationMark);
			if (QuotationMarkIndexes.Length != 0 && QuotationMarkIndexes.Length != 2 && QuotationMarkIndexes.Length != 4)
				throw new FormatException("Invalid quotation pattern for key-value-pair.");

			// Find assignment sign depending on whether the key is in quotation signs or not.
			int AssignmentCharacterIndex = -1;
			if (data[0] == Constants.QuotationMark)
				AssignmentCharacterIndex = Array.FindIndex(data, QuotationMarkIndexes[1] + 1, b => b == Constants.AssignmentCharacter);
			else
				AssignmentCharacterIndex = Array.FindIndex(data, b => b == Constants.AssignmentCharacter);

			var hasValue = AssignmentCharacterIndex != -1;
			var keyQuoted = data[0] == Constants.QuotationMark;
			var valueQuoted = hasValue && data[data.Length - 1] == Constants.QuotationMark
								&& ((QuotationMarkIndexes.Length == 2 && !keyQuoted)
									|| QuotationMarkIndexes.Length == 4);

			if (keyQuoted && !valueQuoted && !hasValue
				&& (data[0] != Constants.QuotationMark || data[data.Length - 1] != Constants.QuotationMark))
			{
				throw new FormatException("Invalid quotation pattern for key-value-pair.");
			}

			if ((!keyQuoted && !valueQuoted && QuotationMarkIndexes.Length != 0)
				|| (keyQuoted ^ valueQuoted && QuotationMarkIndexes.Length != 2)
				|| (keyQuoted && valueQuoted && QuotationMarkIndexes.Length != 4))
			{
				throw new FormatException("Invalid quotation pattern for key-value-pair.");
			}

			// Test that between `key` and = is nothing but white spaces.
			if (keyQuoted && hasValue)
			{
				var space = new byte[AssignmentCharacterIndex - QuotationMarkIndexes[1] - 1];
				Array.Copy(data, QuotationMarkIndexes[1] + 1, space, 0, space.Length);
				for (int i = 0; i < space.Length; i++)
					if (!charactersToTrim.Contains(space[i]))
						throw new FormatException("Non-white-space characters are disallowed between quotation and assignment characters.");
			}

			// Test that between = and `value` is nothing but white spaces.
			if (valueQuoted && hasValue)
			{
				var space = new byte[QuotationMarkIndexes[keyQuoted ? 2 : 0] - AssignmentCharacterIndex - 1];
				Array.Copy(data, AssignmentCharacterIndex + 1, space, 0, space.Length);
				for (int i = 0; i < space.Length; i++)
				{
					if (!charactersToTrim.Contains(space[i]))
						throw new FormatException("Non-white-space characters are disallowed between quotation and assignment characters.");
				}
			}

			// Get key.
			byte[] key;
			var keyFromIndex = keyQuoted ? 1 : 0;
			var keyToIndex = keyQuoted
								? QuotationMarkIndexes[1] - 1
								: (AssignmentCharacterIndex == -1
									? data.Length - 1
									: AssignmentCharacterIndex - 1);

			byte[] value = null;
			var valueFromIndex = -1;
			var valueToIndex = -1;
			if (hasValue)
			{
				if (valueQuoted)
				{
					if (keyQuoted)
					{
						// Value is within quotation signs: `abc`=`def`
						valueFromIndex = QuotationMarkIndexes[2] + 1;
						valueToIndex = QuotationMarkIndexes[3] - 1;
					}
					else
					{
						// Value is within quotation signs: abc=`def`
						valueFromIndex = QuotationMarkIndexes[0] + 1;
						valueToIndex = QuotationMarkIndexes[1] - 1;
					}
				}
				else
				{
					// Value is not within quotation signs: abc=def | `abc`=def
					valueFromIndex = AssignmentCharacterIndex + 1;
					valueToIndex = data.Length - 1;
				}
			}

			// Cut off whitespaces after unquoted keys.
			if (!keyQuoted)
			{
				// Decrease index to until no more whitespaces.
				while (keyToIndex > 0 && charactersToTrim.Contains(data[keyToIndex]))
					keyToIndex--;
			}

			// Cut off whitespaces before unquoted values.
			if (!valueQuoted && hasValue)
			{
				// Increase index from until no more whitespaces.
				while (valueFromIndex < data.Length && charactersToTrim.Contains(data[valueFromIndex]))
					valueFromIndex++;
			}

			if (keyFromIndex > keyToIndex)
			{
				key = new byte[0];
			}
			else
			{
				key = new byte[keyToIndex - keyFromIndex + 1];
				Array.Copy(data, keyFromIndex, key, 0, key.Length);
			}

			if (valueFromIndex > valueToIndex)
			{
				value = new byte[0];
			}
			else if (hasValue)
			{
				value = new byte[valueToIndex - valueFromIndex + 1];
				Array.Copy(data, valueFromIndex, value, 0, value.Length);
			}

			return (Key: key, Value: value);
		}

		/// <summary>
		/// Finds all indexes in <paramref name="data"/> at which
		/// the value <paramref name="b"/> occurs.
		/// </summary>
		/// <param name="data">The data to search.</param>
		/// <param name="b">The byte to find.</param>
		/// <returns>Array of all indexes, the <paramref name="b"/> is at.</returns>
		/// <exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="data"/> is null.</exception>
		private static int[] findIndexes(byte[] data, byte b)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));

			var indexes = new List<int>();
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i] == b) indexes.Add(i);
			}

			return indexes.ToArray();
		}

		/// <summary>
		/// Removes unnecessary characters from <paramref name="data"/> which
		/// are irrelevant for parsing.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="charactersToTrim">The characters to trim at the front and the back.</param>
		/// <returns>The unbeautified data.</returns>
		public static byte[] TrimData(byte[] data, byte[] charactersToTrim)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));
			if (charactersToTrim == null) throw new ArgumentNullException(nameof(charactersToTrim));

			int firstByte = -1;
			int lastByte = data.Length;

			for (int i = 0; i < data.Length; i++)
			{
				++firstByte;
				var b = data[i];
				var trim = false;
				foreach (var v in charactersToTrim)
				{
					if (b == v)
					{
						trim = true;
						break;
					}
				}

				if (!trim) break;
			}

			for (int i = data.Length - 1; i >= 0; i--)
			{
				--lastByte;
				var b = data[i];
				var trim = false;
				foreach (var v in charactersToTrim)
				{
					if (b == v)
					{
						trim = true;
						break;
					}
				}

				if (!trim) break;
			}


			if (firstByte > lastByte) return new byte[0];

			var temp = new byte[lastByte - firstByte + 1];
			Array.Copy(data, firstByte, temp, 0, temp.Length);
			return temp;
		}

		/// <summary>
		/// Splits a whole bunch of data into separate records.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="offset">The offset to read from the data.</param>
		/// <returns>The records found, the number of data bytes consumed from
		///		<paramref name="offset"/> and whether the entity has been
		///		read completely (false) or lacks more data (false.)</returns>
		public static (IEnumerable<byte[]> Records, int ConsumedData, bool EntityComplete) SplitRecords(byte[] data, int offset = 0)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));
			if (offset < 0 || offset >= data.Length)
				throw new ArgumentOutOfRangeException("Offset out of bounds.", nameof(offset));

			var initialOffset = offset;
			var records = new List<byte[]>();

			var entitySeparatorBytes = ContentEncoding.Encoding.GetBytes(Constants.EntitySeparator);
			var recordSeparatorBytes = ContentEncoding.Encoding.GetBytes(Constants.RecordSeparator);
			var minSeparatorLength = Math.Min(entitySeparatorBytes.Length, recordSeparatorBytes.Length);
			var lastSeparatorOffset = offset - 1;

			byte[] temp;
			while (offset <= data.Length - minSeparatorLength)
			{
				if (offset <= data.Length - entitySeparatorBytes.Length)
				{
					// Can be entity separator.
					temp = new byte[entitySeparatorBytes.Length];
					Array.Copy(data, offset, temp, 0, entitySeparatorBytes.Length);
					if (temp.SequenceEqual(entitySeparatorBytes))
					{
						temp = new byte[offset - lastSeparatorOffset - 1];
						Array.Copy(data, lastSeparatorOffset + 1, temp, 0, temp.Length);
						records.Add(temp);
						offset += entitySeparatorBytes.Length - 1; // One byte would be added at the end of the loop.
						lastSeparatorOffset = offset; // End of separator bytes required as index.

						// Any entity separator will terminate splitting.
						return (Records: records, ConsumedData: offset - initialOffset + 1, EntityComplete: true);
					}
				}

				if (offset <= data.Length - recordSeparatorBytes.Length)
				{
					// Can be record separator.
					temp = new byte[recordSeparatorBytes.Length];
					Array.Copy(data, offset, temp, 0, recordSeparatorBytes.Length);
					if (temp.SequenceEqual(recordSeparatorBytes))
					{
						temp = new byte[offset - lastSeparatorOffset - 1];
						Array.Copy(data, lastSeparatorOffset + 1, temp, 0, temp.Length);
						records.Add(temp);
						offset += recordSeparatorBytes.Length - 1; // One byte will be added at the end of the loop.
						lastSeparatorOffset = offset; // End of separator bytes required as index.
					}
				}

				++offset;
			}

			return (Records: records, ConsumedData: offset - initialOffset, EntityComplete: false);
		}
	}
}
