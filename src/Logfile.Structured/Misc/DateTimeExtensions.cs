namespace Logfile.Structured.Misc
{
	using System;
	using System.Globalization;

	/// <summary>
	/// Implements helper extension methods for date/time handling.
	/// </summary>
	public static class DateTimeExtensions
	{
		static readonly DateTime Epoch;

		/// <summary>
		/// Initializes the <see cref="DateTimeExtensions"/> class.
		/// </summary>
		static DateTimeExtensions()
		{
			Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		}

		/// <summary>
		/// Converts a <see cref="DateTime"/> instance to a textual
		/// ISO 8601 date and time representation.
		/// </summary>
		/// <param name="dt">The <see cref="DateTime"/> instance.</param>
		/// <returns>The textual ISO 8601 date and time representation. UTC
		///		<paramref name="dt"/> will output timezone information "Z"
		///		while unspecified <paramref name="dt"/> will output no timezone
		///		information.</returns>
		public static string ToIso8601String(this DateTime dt) => dt.ToString("o");

		/// <summary>
		/// Parses a textual ISO 8601 date and time representation
		/// to a <see cref="DateTime"/> instance.
		/// </summary>
		/// <param name="s">The textual ISO 8601 date and time representation.</param>
		/// <returns>The <see cref="DateTime"/> instance. Zulu "Z" timezone
		///		information will map to a UTC <see cref="DateTime"/> while
		///		a present timezone information will map to a local timezone
		///		information. Missing timezone information will map to an
		///		unspecified <see cref="DateTime"/>.</returns>
		public static DateTime ParseIso8601String(string s) => DateTime.Parse(s, null, DateTimeStyles.RoundtripKind);

		/// <summary>
		/// Parses a textual ISO 8601 date and time representation
		/// to a <see cref="DateTime"/> instance.
		/// </summary>
		/// <param name="s">The textual ISO 8601 date and time representation.</param>
		/// <param name="dt">Outputs the <see cref="DateTime"/> instance. Zulu "Z" timezone
		///		information will map to a UTC <see cref="DateTime"/> while
		///		a present timezone information will map to a local timezone
		///		information. Missing timezone information will map to an
		///		unspecified <see cref="DateTime"/>.</param>
		/// <returns>true if <paramref name="s"/> was successfully parsed,
		///		false otherwise.<returns>
		public static bool TryParseIso8601String(string s, out DateTime dt) => DateTime.TryParse(s, null, DateTimeStyles.RoundtripKind, out dt);

		/// <summary>
		/// Converts a <see cref="DateTimeOffset"/> instance to a textual
		/// ISO 8601 date and time representation.
		/// </summary>
		/// <param name="dt">The <see cref="DateTimeOffset"/> instance.</param>
		/// <returns>The textual ISO 8601 date and time representation.</returns>
		public static string ToIso8601String(this DateTimeOffset dt) => dt.ToString("o");

		/// <summary>
		/// Parses a textual ISO 8601 date and time representation
		/// to a <see cref="DateTimeOffset"/> instance.
		/// </summary>
		/// <param name="s">The textual ISO 8601 date and time representation.</param>
		/// <returns>The <see cref="DateTimeOffset"/> instance. Missing timezone
		///		information will map to the local time offset from UTC.</returns>
		public static DateTimeOffset ParseIso8601StringToDateTimeOffset(string s) => DateTimeOffset.Parse(s, null, DateTimeStyles.RoundtripKind);

		/// <summary>
		/// Parses a textual ISO 8601 date and time representation
		/// to a <see cref="DateTimeOffset"/> instance.
		/// </summary>
		/// <param name="s">The textual ISO 8601 date and time representation.</param>
		/// <param name="dt">Outputs the <see cref="DateTimeOffset"/> instance. Missing timezone
		///		information will map to the local time offset from UTC.</param>
		/// <returns>true if <paramref name="s"/> was successfully parsed,
		///		false otherwise.<returns>
		public static bool TryParseIso8601String(string s, out DateTimeOffset dt) => DateTimeOffset.TryParse(s, null, DateTimeStyles.RoundtripKind, out dt);

		/// <summary>
		/// Creates a <see cref="DateTime"/> UTC instance from a UNIX time in seconds.
		/// </summary>
		/// <param name="unixTime">The UNIX time in seconds.</param>
		/// <returns>The <see cref="DateTime"/> UTC instance.</returns>
		/// <remarks>Taken from: https://stackoverflow.com/questions/2883576/how-do-you-convert-epoch-time-in-c</remarks>
		public static DateTime FromUnixTime(long unixTime)
		{
			return Epoch.AddSeconds(unixTime);
		}

		/// <summary>
		/// Converts the <paramref name="dateTime"/> to a UNIX time in seconds.
		/// </summary>
		/// <param name="dateTime">The date and time.</param>
		/// <returns>The <paramref name="dateTime"/> in UNIX time in seconds.</returns>
		/// <remarks>Taken from: https://stackoverflow.com/questions/2883576/how-do-you-convert-epoch-time-in-c</remarks>
		public static long ToUnixTime(this DateTime dateTime)
		{
			return Convert.ToInt64((dateTime.ToUniversalTime() - Epoch).TotalSeconds);
		}
	}
}
