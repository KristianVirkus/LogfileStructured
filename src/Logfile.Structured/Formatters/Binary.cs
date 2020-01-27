using Logfile.Structured.Elements;
using Logfile.Structured.Misc;
using System;
using System.Collections.Generic;

namespace Logfile.Structured.Formatters
{
	/// <summary>
	/// Implements a formatter for binary log event details.
	/// </summary>
	public class Binary : ILogEventDetailFormatter
	{
		#region Constants

		/// <summary>
		/// Gets the log event detail's identification string.
		/// </summary>
		public const string Identification = "Binary";

		/// <summary>
		/// Gets some introductory text.
		/// </summary>
		public const string Introduction = "Hex dump:";

		/// <summary>
		/// Gets the number of bytes to output per row.
		/// </summary>
		public const int BytesPerRow = 16;

		/// <summary>
		/// Gets whether to include addresses for the output data.
		/// </summary>
		public const bool IncludeAddresses = true;

		/// <summary>
		/// Gets whether to include a hexadecimal representation of the data in the output.
		/// </summary>
		public const bool IncludeHex = true;

		/// <summary>
		/// Gets whether to include a human-readable transcript of the data in the output.
		/// </summary>
		public const bool IncludeTranscript = true;

		/// <summary>
		/// Gets the character to use as substitue for control characters in the output.
		/// </summary>
		public const char NonPrintableCharacterSubstitute = '.';

		#endregion

		#region Static members

		/// <summary>
		/// Gets the default instance.
		/// </summary>
		public static Binary Default { get; }

		#endregion

		#region Constructors

		static Binary()
		{
			Default = new Binary();
		}

		private Binary()
		{
		}

		#endregion

		#region ILogEventDetailFormatter implementation

		public IEnumerable<Type> SupportedLogEventDetailsTypes => new[] { typeof(Logfile.Core.Details.Binary) };

		public string ID => Identification;

		public string Format(object detail)
		{
			if (detail == null) throw new ArgumentNullException(nameof(detail));
			if (!(detail is Logfile.Core.Details.Binary binary)) throw new NotSupportedException(nameof(detail));

			var data = BinaryStringifier.Stringify(
				binary.Data,
				0,
				binary.Data.Count,
				BytesPerRow,
				IncludeAddresses,
				IncludeHex,
				IncludeTranscript,
				NonPrintableCharacterSubstitute);
			return $"{Introduction}{Constants.NewLine}{data.Replace(Event.QuotationMark, NonPrintableCharacterSubstitute)}";
		}

		#endregion
	}
}
