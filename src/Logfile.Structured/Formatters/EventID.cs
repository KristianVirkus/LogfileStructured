using System;
using System.Collections.Generic;

namespace Logfile.Structured.Formatters
{
	/// <summary>
	/// Implements a formatter for event ID log event details.
	/// </summary>
	public class EventID : ILogEventDetailFormatter
	{
		#region Constants

		/// <summary>
		/// Gets the log event detail's identification string.
		/// </summary>
		public const string Identification = "EventID";

		#endregion

		#region Static members

		/// <summary>
		/// Gets the default instance.
		/// </summary>
		public static EventID Default { get; }

		#endregion

		#region Constructors

		static EventID()
		{
			Default = new EventID();
		}

		private EventID()
		{
		}

		#endregion

		#region ILogEventDetailFormatter implementation

		public IEnumerable<Type> SupportedLogEventDetailsTypes => new[] { typeof(Logfile.Core.Details.EventID) };

		public string ID => Identification;

		public string Format(object detail)
		{
			if (detail == null) throw new ArgumentNullException(nameof(detail));
			if (!(detail is Logfile.Core.Details.EventID eventID)) throw new NotSupportedException(nameof(detail));

			return eventID.ToString();
		}

		#endregion
	}
}
