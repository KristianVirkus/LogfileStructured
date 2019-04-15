using Logfile.Structured.Elements;
using System;
using System.Collections.Generic;

namespace Logfile.Structured
{
	public static class Constants
	{
		/// <summary>
		/// Gets the entity separator. May not appear elsewhere in the log.
		/// </summary>
		public const string EntitySeparator = "\x1e";

		/// <summary>
		/// Gets a string with characters which will automatically be ignored
		/// after entity separators when reading logfiles.
		/// </summary>
		public const string IgnoredAfterEntitySeparators = " \t\n\r";

		/// <summary>
		/// Gets the character to introduce new lines.
		/// </summary>
		public const char NewLine = '\n';

		/// <summary>
		/// Gets the indent for certain outputs.
		/// </summary>
		public const string Indent = "    ";

		/// <summary>
		/// Gets the known structured logfile element types.
		/// </summary>
		public static Dictionary<string, Type> ElementTypes;

		/// <summary>
		/// Static constructor.
		/// </summary>
		/// <typeparam name="TLoglevel">The loglevel.</typeparam>
		public static void BuildElementTypes<TLoglevel>()
				where TLoglevel : Enum
		{
			ElementTypes = new Dictionary<string, Type>
			{
				{ Event<TLoglevel>.Identification, typeof(Event<TLoglevel>) },
			};
		}
	}
}
