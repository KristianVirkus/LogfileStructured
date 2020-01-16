﻿using System;
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
	}
}
