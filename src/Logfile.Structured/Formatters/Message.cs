using System;
using System.Collections.Generic;

namespace Logfile.Structured.Formatters
{
	/// <summary>
	/// Implementation a formatter for message log event details.
	/// </summary>
	public class Message : ILogEventDetailFormatter
	{
		#region Constants

		/// <summary>
		/// Gets the log event detail's identification string.
		/// </summary>
		public const string Identification = "Message";

		#endregion

		#region Static members

		/// <summary>
		/// Gets the default instance.
		/// </summary>
		public static Message Default { get; }

		#endregion

		#region Constructors

		static Message()
		{
			Default = new Message();
		}

		private Message()
		{
		}

		#endregion

		#region ILogEventDetailFormatter implementation

		public IEnumerable<Type> SupportedLogEventDetailsTypes => new[] { typeof(Logfile.Core.Details.Message) };

		public string ID => Identification;

		public string Format(object detail)
		{
			if (detail == null) throw new ArgumentNullException(nameof(detail));
			if (!(detail is Logfile.Core.Details.Message message)) throw new NotSupportedException(nameof(detail));

			return message.Text;
		}

		#endregion
	}
}
