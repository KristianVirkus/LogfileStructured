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

			// Find quotation signs.
			var quotationSignIndexes = findIndexes(data: data, b: (byte)Constants.QuotationSign);
			if (quotationSignIndexes.Length != 0 && quotationSignIndexes.Length != 2 && quotationSignIndexes.Length != 4)
				throw new FormatException("Invalid quotation pattern for key-value-pair.");

			// Find assignment sign depending on whether the key is in quotation signs or not.
			int assignmentSignIndex = -1;
			if (data[0] == Constants.QuotationSign)
				assignmentSignIndex = Array.FindIndex(data, quotationSignIndexes[1] + 1, b => b == Constants.AssignmentSign);
			else
				assignmentSignIndex = Array.FindIndex(data, b => b == Constants.AssignmentSign);

			var keyQuoted = data[0] == Constants.QuotationSign;
			var valueQuoted = (quotationSignIndexes.Length == 2 && !keyQuoted)
								|| quotationSignIndexes.Length == 4;
			var hasValue = assignmentSignIndex != -1;

			if (keyQuoted && !valueQuoted && !hasValue
				&& (data[0] != Constants.QuotationSign || data[data.Length - 1] != Constants.QuotationSign))
			{
				throw new FormatException("Invalid quotation pattern for key-value-pair.");
			}

			if ((!keyQuoted && !valueQuoted && quotationSignIndexes.Length != 0)
				|| (keyQuoted ^ valueQuoted && quotationSignIndexes.Length != 2))
			{
				throw new FormatException("Invalid quotation pattern for key-value-pair.");
			}

			// Test that between `key` and = is nothing but white spaces.
			if (keyQuoted && hasValue)
			{
				var space = new byte[assignmentSignIndex - quotationSignIndexes[1] - 1];
				Array.Copy(data, quotationSignIndexes[1] + 1, space, 0, space.Length);
				for (int i = 0; i < space.Length; i++)
					if (!Char.IsWhiteSpace((char)space[i]))
						throw new FormatException("Non-white-space characters are disallowed between quotation and assignment characters.");
			}

			// Test that between = and `value` is nothing but white spaces.
			if (valueQuoted && hasValue)
			{
				var space = new byte[quotationSignIndexes[keyQuoted ? 2 : 0] - assignmentSignIndex - 1];
				Array.Copy(data, assignmentSignIndex + 1, space, 0, space.Length);
				for (int i = 0; i < space.Length; i++)
					if (!Char.IsWhiteSpace((char)space[i]))
						throw new FormatException("Non-white-space characters are disallowed between quotation and assignment characters.");
			}

			// Get key.
			byte[] key;
			var keyFromIndex = keyQuoted ? 1 : 0;
			var keyToIndex = keyQuoted
								? quotationSignIndexes[1] - 1
								: (assignmentSignIndex == -1
									? data.Length - 1
									: assignmentSignIndex - 1);

			byte[] value = null;
			var valueFromIndex = -1;
			var valueToIndex = -1;
			if (hasValue)
			{
				if (!keyQuoted && valueQuoted)
				{
					// Value is within quotation signs: abc=`def`
					valueFromIndex = quotationSignIndexes[0] + 1;
					valueToIndex = quotationSignIndexes[1] - 1;
				}
				else if (keyQuoted && valueQuoted)
				{
					// Value is within quotation signs: `abc`=`def`
					valueFromIndex = quotationSignIndexes[2] + 1;
					valueToIndex = quotationSignIndexes[3] - 1;
				}
				else if (!valueQuoted)
				{
					// Value is not within quotation signs: abc=def | `abc`=def
					valueFromIndex = assignmentSignIndex + 1;
					valueToIndex = data.Length - 1;
				}
				else
				{
					throw new FormatException("Invalid quotation pattern for key-value-pair.");
				}
			}

			// Cut off whitespaces before and after unquoted keys.
			if (!keyQuoted)
			{
				// Increase index from and decrease index to until no more whitespaces.
				while (keyFromIndex < data.Length && char.IsWhiteSpace((char)data[keyFromIndex]))
					keyFromIndex++;

				while (keyToIndex > 0 && char.IsWhiteSpace((char)data[keyToIndex]))
					keyToIndex--;
			}

			// Cut off whitespaces before and after unquoted values.
			if (!valueQuoted && hasValue)
			{
				// Increase index from and decrease index to until no more whitespaces.
				while (valueFromIndex < data.Length && char.IsWhiteSpace((char)data[valueFromIndex]))
					valueFromIndex++;

				while (valueToIndex > 0 && char.IsWhiteSpace((char)data[valueToIndex]))
					valueToIndex--;
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
	}
}
