using Logfile.Structured.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		/// Gets the separator for multiple records within an event.
		/// </summary>
		public const string RecordSeparator = "\x1f";

		/// <summary>
		/// Gets the visual separator for multiple records within an event.
		/// </summary>
		public const string VisualRecordSeparator = " == ";

		/// <summary>
		/// Gets a string with characters to be ignored after record separators
		/// when reading an event.
		/// </summary>
		public const string IgnoredAfterRecordSeparators = "-=#*";

		/// <summary>
		/// Gets the character for quoting values.
		/// </summary>
		public const char QuotationSign = '`';

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
		public Header(string appName, DateTime appStartUpTime, Guid appInstanceID,
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
			sb.Append($"{Constants.EntitySeparator}{LogfileIdentity}{RecordSeparator}{VisualRecordSeparator}{AppNameRecord}={QuotationSign}{this.encode(this.AppName)}{QuotationSign}{RecordSeparator}{VisualRecordSeparator}{AppStartUpTimeRecord}={QuotationSign}{this.AppStartUpTime.ToIso8601String()}{QuotationSign}{RecordSeparator}{VisualRecordSeparator}{AppInstanceLogfileSequenceNumberRecord}={this.AppInstanceLogfileSequenceNumber}");
			sb.Append(Constants.NewLine);

			// Add miscellaneous meta information.
			foreach (var kvp in this.Miscellaneous)
			{
				sb.Append($"{RecordSeparator}{Constants.Indent}{QuotationSign}{this.encode(kvp.Key)}{QuotationSign}={QuotationSign}{this.encode(kvp.Value)}{QuotationSign}");
				sb.Append(Constants.NewLine);
			}

			return sb.ToString();
		}

		string encode(string text) => ContentEncoding.Encode(text, new[] { (byte)QuotationSign });
	}
}
