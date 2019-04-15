using System;

namespace Logfile.Structured.Elements
{
	/// <summary>
	/// Common interface of all elements to be serialized into a structured logfile.
	/// </summary>
	/// <typeparam name="TLoglevel">The loglevel type.</typeparam>
	public interface IElement<TLoglevel> where TLoglevel : Enum
	{
		/// <summary>
		/// Serialises the element to a string.
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		/// <returns>The string representation.</returns>
		string Serialize(Configuration<TLoglevel> configuration);
	}
}
