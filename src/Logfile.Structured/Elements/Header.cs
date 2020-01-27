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
			sb.Append($"{LogfileIdentity}{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{AppNameRecord}{Constants.AssignmentCharacter}{Constants.QuotationMark}{encode(this.AppName)}{Constants.QuotationMark}{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{AppStartUpTimeRecord}{Constants.AssignmentCharacter}{Constants.QuotationMark}{this.AppStartUpTime.ToIso8601String()}{Constants.QuotationMark}{Constants.RecordSeparator}{Constants.VisualRecordSeparator}{AppInstanceLogfileSequenceNumberRecord}{Constants.AssignmentCharacter}{this.AppInstanceLogfileSequenceNumber}");

			// Add miscellaneous meta information.
			foreach (var kvp in this.Miscellaneous)
			{
				sb.Append(Constants.NewLine);
				sb.Append($"{Constants.RecordSeparator}{Constants.Indent}{Constants.QuotationMark}{encode(kvp.Key)}{Constants.QuotationMark}{Constants.AssignmentCharacter}{Constants.QuotationMark}{encode(kvp.Value)}{Constants.QuotationMark}");
			}

			sb.Append(Constants.EntitySeparator + Constants.NewLine);
			return sb.ToString();
		}

		/// <summary>
		/// Encodes a text for use within a structured logfile.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The encoded text.</returns>
		/// <exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="text"/> is null.</exception>
		static string encode(string text) => ContentEncoding.Encode(text, new[] { (byte)Constants.QuotationMark });

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
		public static (bool MoreDataRequired, int ConsumedData, IElement<TLoglevel> Element) Parse(
			byte[] data, TimeZoneInfo timeZone = null)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));
			timeZone = timeZone ?? TimeZoneInfo.Local;

			var exceptionThrownByPurpose = false;
			try
			{
				var (records, consumedData, entityComplete) = ContentEncoding.SplitRecords(data: data, offset: 0);
				if (!entityComplete) return (MoreDataRequired: true, ConsumedData: 0, Element: null);

				var record = 0;
				if (records?.Count() < 4)
				{
					exceptionThrownByPurpose = true;
					throw new NotSupportedException("Incompatible element data.");
				}

				var charactersToTrim = ContentEncoding.Encoding.GetBytes(Constants.IgnoredAfterRecordSeparators);
				var headerRecord = ContentEncoding.TrimData(data: records.ElementAt(record++), charactersToTrim: charactersToTrim);

				var appNameRecord = ContentEncoding.TrimData(data: records.ElementAt(record++), charactersToTrim: charactersToTrim);
				var appNameKvp = ContentEncoding.ParseKeyValuePair(data: appNameRecord);
				if (decode(ContentEncoding.Encoding.GetString(appNameKvp.Key ?? new byte[0])) != AppNameRecord)
					throw new FormatException("Invalid app name.");
				var appNameText = decode(ContentEncoding.Encoding.GetString(appNameKvp.Value ?? new byte[0]));

				var startUpTimeRecord = ContentEncoding.TrimData(data: records.ElementAt(record++), charactersToTrim: charactersToTrim);
				var startUpTimeKvp = ContentEncoding.ParseKeyValuePair(data: startUpTimeRecord);
				if (decode(ContentEncoding.Encoding.GetString(startUpTimeKvp.Key ?? new byte[0])) != AppStartUpTimeRecord)
					throw new FormatException("Invalid start-up time.");
				var startUpTimeText = decode(ContentEncoding.Encoding.GetString(startUpTimeKvp.Value ?? new byte[0]));
				var startUpTime = DateTimeExtensions.ParseIso8601String(startUpTimeText);
				if (startUpTime.Kind == DateTimeKind.Unspecified)
				{
					startUpTime = TimeZoneInfo.ConvertTimeToUtc(startUpTime, timeZone);
				}
				if (startUpTime.Kind != DateTimeKind.Utc)
					startUpTime = startUpTime.ToUniversalTime();

				var sequenceNoRecord = ContentEncoding.TrimData(data: records.ElementAt(record++), charactersToTrim: charactersToTrim);
				var sequenceNoKvp = ContentEncoding.ParseKeyValuePair(data: sequenceNoRecord);
				if (decode(ContentEncoding.Encoding.GetString(sequenceNoKvp.Key ?? new byte[0])) != AppInstanceLogfileSequenceNumberRecord)
					throw new FormatException("Invalid sequence number.");
				var sequenceNoText = decode(ContentEncoding.Encoding.GetString(sequenceNoKvp.Value ?? new byte[0]));
				var sequenceNo = int.Parse(sequenceNoText);

				// Read optional key-value-pairs.
				var kvps = new List<(string Key, string Value)>();
				for (int i = 4; i < records.Count(); i++)
				{
					var kvpBytes = ContentEncoding.TrimData(records.ElementAt(i), charactersToTrim: charactersToTrim);
					var kvp = ContentEncoding.ParseKeyValuePair(data: kvpBytes);
					kvps.Add((Key: decode(ContentEncoding.Encoding.GetString(kvp.Key)), Value: decode(ContentEncoding.Encoding.GetString(kvp.Value))));
				}

				var headerIdentityBytes = ContentEncoding.Encoding.GetBytes(LogfileIdentity);
				if (!headerRecord.SequenceEqual(headerIdentityBytes))
					throw new NotSupportedException("Incompatible element data.");

				return (MoreDataRequired: false,
						ConsumedData: consumedData,
						Element: new Header<TLoglevel>(
							appName: appNameText,
							appStartUpTime: startUpTime,
							appInstanceSequenceNumber: sequenceNo,
							miscellaneous: kvps.ToDictionary(k => k.Key, v => v.Value)));
			}
			catch (Exception ex) when (!exceptionThrownByPurpose)
			{
				throw new FormatException("Invalid element format.", ex);
			}
		}

		public static (bool MoreDataRequired, bool IsCompatible) Identify(byte[] data)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));

			var headerIdentityOnlyBytes = ContentEncoding.Encoding.GetBytes(LogfileIdentity);
			var headerIdentityRecordBytes = ContentEncoding.Encoding.GetBytes(LogfileIdentity + Constants.RecordSeparator);

			// Request more data if not enough to determine compatibility.
			if (data.Length < headerIdentityRecordBytes.Length)
				return (MoreDataRequired: true, IsCompatible: false);

			var result = ContentEncoding.SplitRecords(data: data, offset: 0);
			return (MoreDataRequired: false,
					IsCompatible: result.Records?.Count() >= 1 && result.Records.First().SequenceEqual(headerIdentityOnlyBytes));
		}
	}
}
