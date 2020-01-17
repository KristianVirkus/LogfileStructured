using Logfile.Structured.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * Event structure:
 * ES := entity separator
 * RS := record separator
 * VRS := [\s\-=#\*]*
 * QS := quotation sign '`'
 * AS := assignment sign '='
 * Event := "SLF.1"
 *				<RS> <VRS> "app" <AS> <QS> {app name} <QS>
 *				<RS> <VRS> "start-up" <AS> <QS> {start-up time} <QS>
 *				<RS> <VRS> "seq-no" <AS> <QS> {sequence number} <QS>
 *				(<RS> '\n' \s* <QS> [\e]* <QS> '=' <QS> [\e]* <QS>)*
 *				<ES>
 */

namespace Logfile.Structured.Elements
{
	/// <summary>
	/// Represents a structure element for a header.
	/// </summary>
	/// <typeparam name="TLoglevel">The loglevel type.</typeparam>
	public class Header<TLoglevel> : IElement<TLoglevel> where TLoglevel : Enum
	{
		/// <summary>
		/// Gets the logfile identity string.
		/// </summary>
		public const string LogfileIdentity = "SLF.1";

		/// <summary>
		/// Gets the record ID for the application name.
		/// </summary>
		public const string AppNameRecord = "app";

		/// <summary>
		/// Gets the record ID for the application start-up time.
		/// </summary>
		public const string AppStartUpTimeRecord = "start-up";

		/// <summary>
		/// Gets the record ID for the application instance's logfile sequence number.
		/// </summary>
		public const string AppInstanceLogfileSequenceNumberRecord = "seq-no";

		/// <summary>
		/// Gets the application name.
		/// </summary>
		public string AppName { get; }

		/// <summary>
		/// Gets the application start-up time.
		/// </summary>
		public DateTime AppStartUpTime { get; }

		/// <summary>
		/// Gets the application instance's logfile sequence number.
		/// </summary>
		public int AppInstanceLogfileSequenceNumber { get; }

		/// <summary>
		/// Gets miscellaneous key-value pairs.
		/// </summary>
		public IReadOnlyDictionary<string, string> Miscellaneous { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Header"/> class.
		/// </summary>
		/// <param name="appName">The application name.</param>
		/// <param name="appStartUpTime">The application start-up time.</param>
		/// <param name="appInstanceSequenceNumber">The logfile sequence number within
		///		an application instance.</param>
		/// <param name="miscellaneous">Miscellaneous meta information.</param>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="appName"/> or <paramref name="miscellaneous"/>
		///		is null.</exception>
		public Header(string appName, DateTime appStartUpTime,
			int appInstanceSequenceNumber, IReadOnlyDictionary<string, string> miscellaneous)
		{
			this.AppName = appName ?? throw new ArgumentNullException(nameof(appName));
			this.AppStartUpTime = appStartUpTime;
			this.AppInstanceLogfileSequenceNumber = appInstanceSequenceNumber;
			this.Miscellaneous = miscellaneous ?? throw new ArgumentNullException(nameof(miscellaneous));
		}

		public string Serialize(StructuredLogfileConfiguration<TLoglevel> configuration)
		{
			var sb = new StringBuilder();

			// Add logfile identity and common meta information.
			sb.Append($"{LogfileIdentity}{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{AppNameRecord}{Constants.AssignmentSign}{Constants.QuotationSign}{encode(this.AppName)}{Constants.QuotationSign}{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{AppStartUpTimeRecord}{Constants.AssignmentSign}{Constants.QuotationSign}{this.AppStartUpTime.ToIso8601String()}{Constants.QuotationSign}{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{AppInstanceLogfileSequenceNumberRecord}{Constants.AssignmentSign}{this.AppInstanceLogfileSequenceNumber}");

			// Add miscellaneous meta information.
			foreach (var kvp in this.Miscellaneous)
			{
				sb.Append(Constants.NewLine);
				sb.Append($"{Constants.RecordSeparator}{Constants.Indent}{Constants.QuotationSign}{encode(kvp.Key)}{Constants.QuotationSign}{Constants.AssignmentSign}{Constants.QuotationSign}{encode(kvp.Value)}{Constants.QuotationSign}");
			}

			sb.Append(Constants.EntitySeparator);
			return sb.ToString();
		}

		/// <summary>
		/// Encodes a text for use within a structured logfile.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The encoded text.</returns>
		/// <exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="text"/> is null.</exception>
		static string encode(string text) => ContentEncoding.Encode(text, new[] { (byte)Constants.QuotationSign });

		/// <summary>
		/// Decodes a text from use within a structured logfile.
		/// </summary>
		/// <param name="text">The encoded text.</param>
		/// <returns>The text.</returns>
		/// <exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="text"/> is null.</exception>
		static string decode(string text) => ContentEncoding.Decode(text);

		/// <summary>
		/// Parses a header element from <paramref name="data"/>.
		/// </summary>
		/// <param name="data">The data to parse. May contain additional data at the ending.</param>
		/// <param name="encoding">The encoding to treat the data with, null to use UTF-8.</param>
		/// <param name="timeZone">The time zone to treat untyped dates/times as. If null,
		///		the local time zone will be used.</param>
		/// <returns>Information on whether more data is required for successful parsing,
		///		the amount of consumed <paramref name="data"/> bytes and the element
		///		which was parsed from <paramref name="data"/>.</returns>
		///	<exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="data"/> is null.</exception>
		///	<exception cref="NotSupportedException">Thrown, if
		///		<paramref name="data"/> contains unexpected data.</exception>
		///	<exception cref="FormatException">Thrown, if
		///		the <paramref name="data"/> cannot be parsed successfully as
		///		header element.</exception>
		public static (bool MoreDataRequired, int ConsumedData, IElement<TLoglevel> Element) Parse(byte[] data, Encoding encoding, TimeZoneInfo timeZone)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));
			if (encoding == null) encoding = Encoding.UTF8;
			if (timeZone == null) timeZone = TimeZoneInfo.Local;

			var exceptionThrownByPurpose = false;
			try
			{
				var (records, consumedData, entityComplete) = SplitRecords(data: data, offset: 0, encoding: encoding);
				if (!entityComplete) return (MoreDataRequired: true, ConsumedData: 0, Element: null);

				var record = 0;
				if (records?.Count() < 4)
				{
					exceptionThrownByPurpose = true;
					throw new NotSupportedException("Incompatible element data.");
				}

				var charactersToTrim = encoding.GetBytes(Constants.IgnoredAfterRecordSeparators);
				var headerRecord = unbeautify(data: records.ElementAt(record++), charactersToTrim: charactersToTrim);
				var appNameRecord = unbeautify(data: records.ElementAt(record++), charactersToTrim: charactersToTrim);
				var appNameKvp = ContentEncoding.ParseKeyValuePair(data: appNameRecord);
				if (decode(encoding.GetString(appNameKvp.Key ?? new byte[0])) != AppNameRecord)
					throw new FormatException("Invalid app name.");
				var appNameText = decode(encoding.GetString(appNameKvp.Value ?? new byte[0]));
				var startUpTimeRecord = unbeautify(data: records.ElementAt(record++), charactersToTrim: charactersToTrim);
				var startUpTimeKvp = ContentEncoding.ParseKeyValuePair(data: startUpTimeRecord);
				if (decode(encoding.GetString(startUpTimeKvp.Key ?? new byte[0])) != AppStartUpTimeRecord)
					throw new FormatException("Invalid start-up time.");
				var startUpTimeText = decode(encoding.GetString(startUpTimeKvp.Value ?? new byte[0]));
				var startUpTime = DateTimeExtensions.ParseIso8601String(startUpTimeText);
				if (startUpTime.Kind == DateTimeKind.Unspecified)
					DateTime.SpecifyKind(startUpTime, DateTimeKind.Local);
				var sequenceNoRecord = unbeautify(data: records.ElementAt(record++), charactersToTrim: charactersToTrim);
				var sequenceNoText = decode(encoding.GetString(ContentEncoding.ParseKeyValuePair(data: sequenceNoRecord).Value ?? new byte[0]));
				var sequenceNo = int.Parse(sequenceNoText);

				// Read optional key-value-pairs.
				var kvps = new List<(string Key, string Value)>();
				for (int i = 4; i < records.Count(); i++)
				{
					var kvpBytes = unbeautify(records.ElementAt(i), charactersToTrim: charactersToTrim);
					var kvp = ContentEncoding.ParseKeyValuePair(data: kvpBytes);
					kvps.Add((Key: decode(encoding.GetString(kvp.Key)), Value: decode(encoding.GetString(kvp.Value))));
				}

				var headerIdentityBytes = encoding.GetBytes(LogfileIdentity);
				if (!headerRecord.SequenceEqual(headerIdentityBytes))
					throw new NotSupportedException("Incompatible element data.");

				return (MoreDataRequired: false,
						ConsumedData: consumedData,
						Element: new Header<TLoglevel>(
							appName: appNameText,
							appStartUpTime: DateTimeExtensions.ParseIso8601String(startUpTimeText),
							appInstanceSequenceNumber: int.Parse(sequenceNoText),
							miscellaneous: kvps.ToDictionary(k => k.Key, v => v.Value)));
			}
			catch (Exception ex) when (!exceptionThrownByPurpose)
			{
				throw new FormatException("Invalid element format.", ex);
			}
		}

		/// <summary>
		/// Splits a whole bunch of data into separate records.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="offset">The offset to read from the data.</param>
		/// <param name="encoding">The encoding to assume.</param>
		/// <returns>The records found, the number of data bytes consumed from
		///		<paramref name="offset"/> and whether the entity has been
		///		read completely (false) or lacks more data (false.)</returns>
		static (IEnumerable<byte[]> Records, int ConsumedData, bool EntityComplete) SplitRecords(byte[] data, int offset, Encoding encoding)
		{
			var initialOffset = offset;
			var records = new List<byte[]>();

			var entitySeparatorBytes = encoding.GetBytes(Constants.EntitySeparator);
			var recordSeparatorBytes = encoding.GetBytes(Constants.RecordSeparator);
			var minSeparatorLength = Math.Min(entitySeparatorBytes.Length, recordSeparatorBytes.Length);
			var lastSeparatorOffset = -1;

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

		/// <summary>
		/// Removes unnecessary characters from <paramref name="data"/> which
		/// are irrelevant for parsing.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="charactersToTrim">The characters to trim at the front and the back.</param>
		/// <returns>The unbeautified data.</returns>
		static byte[] unbeautify(byte[] data, byte[] charactersToTrim)
		{
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

		public static (bool MoreDataRequired, bool IsCompatible) Identify(byte[] data, Encoding encoding)
		{
			var headerIdentityBytes = encoding.GetBytes(LogfileIdentity + Constants.RecordSeparator);

			// Request more data if not enough to determine compatibility.
			if (data.Length < headerIdentityBytes.Length)
				return (MoreDataRequired: true, IsCompatible: false);

			var (records, _, _) = SplitRecords(data: data, offset: 0, encoding: encoding);
			return (MoreDataRequired: false,
					IsCompatible: records?.Count() >= 1 && records.First().SequenceEqual(headerIdentityBytes));
		}
	}
}
