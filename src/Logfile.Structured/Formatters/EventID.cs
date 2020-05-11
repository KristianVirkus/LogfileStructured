using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

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

		private const string JsonEventIDNumeric = "en";
		private const string JsonEventIDTextual = "et";
		private const string JsonEventIDArguments = "a";
		private const string JsonEventIDArgumentName = "n";
		private const string JsonEventIDArgumentValue = "v";

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

			return AsJson(eventID).ToString(Newtonsoft.Json.Formatting.None);
		}

		#endregion

		#region --- Methods ---

		/// <summary>
		/// Generates a JSON representation of the event ID.
		/// </summary>
		/// <param name="eventID">The event ID to process.</param>
		/// <returns>The JSON representation.</returns>
		/// <exception cref="ArgumentNullException">Thrown if
		///		<paramref name="eventID"/> is null.</exception>
		public static JToken AsJson(Logfile.Core.Details.EventID eventID)
		{
			if (eventID == null) throw new ArgumentNullException(nameof(eventID));

			var json = new JObject(new JProperty(JsonEventIDNumeric, eventID.NumberChain),
									new JProperty(JsonEventIDTextual, eventID.TextChain));

			var eventArgs = new JArray();
			if (eventID.StringArguments?.Any() == true)
			{
				for (int i = 0; i < eventID.StringArguments.Count(); i++)
				{
					var name = eventID.ParameterNames.Count() >= i + 1 ? eventID.ParameterNames.ElementAt(i) : null;
					var value = eventID.StringArguments.ElementAt(i);
					var obj = new JObject();
					if (!string.IsNullOrWhiteSpace(name))
						obj.Add(new JProperty(JsonEventIDArgumentName, name));
					obj.Add(new JProperty(JsonEventIDArgumentValue, value));
					eventArgs.Add(obj);
				}

				json.Add(new JProperty(JsonEventIDArguments, eventArgs));
			}

			return json;
		}

		#endregion
	}
}
