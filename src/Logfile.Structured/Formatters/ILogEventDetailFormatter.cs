using System;
using System.Collections.Generic;

namespace Logfile.Structured.Formatters
{
	/// <summary>
	/// Common interface of all log event details formatters.
	/// </summary>
	public interface ILogEventDetailFormatter
	{
		/// <summary>
		/// Gets a list of supported log event details types.
		/// </summary>
		IEnumerable<Type> SupportedLogEventDetailsTypes { get; }

		/// <summary>
		/// Gets the log event detail ID.
		/// </summary>
		string ID { get; }

		/// <summary>
		/// Formats a log event data instance for structured output.
		/// </summary>
		/// <param name="detail">The log event detail.</param>
		/// <returns>Formatted output.</returns>
		/// <exception cref="ArgumentNullException">Thrown if
		///		<paramref name="detail"/> is null.</exception>
		///	<exception cref="NotSupportedException">Thrown if
		///		<paramref name="detail"/> is of an unsupported type.</exception>
		string Format(object detail);
	}
}
