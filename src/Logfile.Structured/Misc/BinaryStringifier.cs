using Logfile.Structured.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Logfile.Structured.Misc
{
	/// <summary>
	/// Implements a formatter for binary data.
	/// </summary>
	public static class BinaryStringifier
	{
		/// <summary>
		/// Creates a string representation of the binary <paramref name="data"/>.
		/// </summary>
		/// <param name="data">The binary data.</param>
		/// <param name="offset">The byte offset within the <paramref name="data"/>
		///		to start at.</param>
		/// <param name="length">The number of bytes from the <paramref name="offset"/>
		///		to consider.</param>
		/// <param name="bytesPerRow">The number of bytes to print per row.</param>
		/// <param name="IncludeAddresses">Whether to include addresses in hexadecimal
		///		notation in the output.</param>
		/// <param name="IncludeHex">Whether to include the data itself in a hexadecimal
		///		notation.</param>
		/// <param name="IncludeTranscript">Whether to include the <paramref name="data"/>
		///		in a textual notation. For any control character and non-printable character
		///		a <paramref name="NonPrintableCharacterSubstitute"/> will be written.</param>
		/// <param name="NonPrintableCharacterSubstitute">The character to write for
		///		any control character and non-printable character in the
		///		<paramref name="data"/>.</param>
		/// <returns>The string representation.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="data"/>
		///		is null.</exception>
		///	<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/>
		///		is negative or greater or equal to the <paramref name="data"/> length
		///		or if <paramref name="length"/> is either negative or zero.</exception>
		public static string Stringify(IReadOnlyList<byte> data, int offset, int length,
			int bytesPerRow, bool IncludeAddresses, bool IncludeHex, bool IncludeTranscript,
			char NonPrintableCharacterSubstitute)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));
			if ((offset < 0) || (offset >= data.Count)) throw new ArgumentOutOfRangeException(nameof(offset));
			if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

			var rows = new List<string>();
			var row = new StringBuilder();
			var hexRow = new StringBuilder();
			var transcriptRow = new StringBuilder();

			// Fix length and thus allow arbitrary lengths.
			length = Math.Min(length, data.Count - offset);

			var addressNibbles = Convert.ToInt32(Math.Ceiling(2.0 * (offset + length) / 8.0));
			var addressLength = (addressNibbles % 2 == 0 ? addressNibbles : addressNibbles + 1);
			var addressFormatString = string.Format("X{0}", addressLength);

			/*
			 * 16^x = bytesPerRow
			 * x = log16(bytesPerRow)
			 */

			var horizontalAddressNibbles = (int)Math.Ceiling(Math.Log(bytesPerRow, 16.0));
			var dataColumnWidth = Math.Max(2, horizontalAddressNibbles) + 1;

			// Insert padding if offset is not fully dividable by number of bytes per row.
			if (((offset % bytesPerRow) is int paddingLength) && (paddingLength > 0))
			{
				hexRow.Append("".PadRight(paddingLength, ' '));
				transcriptRow.Append("".PadRight(paddingLength, ' '));
			}

			int address = 0;
			int addressRowStart = offset - (offset % bytesPerRow);
			var bytesInCurrentRow = offset % bytesPerRow;

			void addRow()
			{
				if (IncludeAddresses)
				{
					row.Append(addressRowStart.ToString(addressFormatString));
					row.Append(": ");
				}

				if (IncludeHex)
				{
					row.Append(hexRow);
				}

				if (IncludeTranscript)
				{
					if (row.Length > 0) row.Append(" ");
					row.Append(transcriptRow);
				}

				rows.Add(row.ToString());

				row.Clear();
				hexRow.Clear();
				transcriptRow.Clear();
				bytesInCurrentRow = 0;
				addressRowStart = address;
			}

			// Insert lines of formatted data.
			for (address = offset; address < offset + length; address++)
			{
				if ((address % bytesPerRow == 0) && (bytesInCurrentRow > 0))
				{
					addRow();
				}

				var ch = (char)data[address];
				++bytesInCurrentRow;

				if (IncludeHex)
				{
					if (hexRow.Length > 0) hexRow.Append("".PadRight(dataColumnWidth - 2));
					hexRow.Append(((byte)ch).ToString("X2"));
				}

				if (IncludeTranscript)
					transcriptRow.Append(Char.IsControl(ch) ? NonPrintableCharacterSubstitute : ch);
			}

			if ((bytesInCurrentRow % bytesPerRow) != 0)
			{
				if (IncludeHex)
					hexRow.Append("".PadRight((bytesPerRow - bytesInCurrentRow) * dataColumnWidth, ' '));
				if (IncludeTranscript)
					transcriptRow.Append("".PadRight((bytesPerRow - bytesInCurrentRow), ' '));
			}

			if (bytesInCurrentRow > 0)
			{
				addRow();
			}

			// Insert horizontal address.
			if (IncludeAddresses)
			{
				var horizontalAddressRow = new StringBuilder();
				horizontalAddressRow.Append("".PadRight(addressLength + 2, ' '));

				var horizontalAddressFormatString = string.Format("X{0}", horizontalAddressNibbles);
				for (int horizontalAddress = 0; horizontalAddress < bytesPerRow; horizontalAddress++)
				{
					if (horizontalAddress > 0) horizontalAddressRow.Append("".PadRight(dataColumnWidth - horizontalAddressNibbles, ' '));
					horizontalAddressRow.Append(horizontalAddress.ToString(horizontalAddressFormatString));
				}

				rows.Insert(0, $"{horizontalAddressRow.ToString()}");
			}

			return string.Join(Environment.NewLine, rows);
		}
	}
}
