using Logfile.Core.Details;
using Logfile.Structured.Formatters;
using System;
using System.Collections.Generic;

namespace Logfile.Structured
{
	public class StructuredLogfileConfiguration<TLoglevel>
		where TLoglevel : Enum
	{
		internal const string DefaultAppName = "None";
		internal const string DefaultPath = ".";
		internal const string DefaultFileNameFormat = "{app-name}-{start-up-time}-{seq-no}.slf.log";
		internal const int DefaultMaximumLogfileSize = 10 * 1024 * 1024;
		internal const int DefaultKeepLogfiles = 0;

		/// <summary>
		/// Gets the application name.
		/// </summary>
		public string AppName { get; }

		/// <summary>
		/// Gets whether writing to the console is enabled.
		/// </summary>
		public bool WriteToConsole { get; }

		/// <summary>
		/// Gets whether writing to the debug console is enabled.
		/// </summary>
		public bool WriteToDebugConsole { get; }

		/// <summary>
		/// Gets whether writing to a file on a disk is enabled.
		/// If not, only the stream writers will be used.
		/// </summary>
		public bool WriteToDisk { get; }

		/// <summary>
		/// Gets the path to save the logfiles to.
		/// </summary>
		public string Path { get; }

		/// <summary>
		/// Gets the format for logfile names.
		/// </summary>
		public string FileNameFormat { get; }

		/// <summary>
		/// Gets the maximum logfile size in bytes, or null if not restricted.
		/// </summary>
		public int? MaximumLogfileSize { get; }

		/// <summary>
		/// Gets the number of logfiles to keep in the path when logfile
		/// cleanup is enabled, or null to disable logfiles cleanup.
		/// </summary>
		public int? KeepLogfiles { get; }

		/// <summary>
		/// Gets the log event detail formatters identified by their log event detail type.
		/// </summary>
		public IReadOnlyDictionary<Type, ILogEventDetailFormatter> LogEventDetailFormatters { get; }

		/// <summary>
		/// Gets the sensitive data settings. Can be null if not configured.
		/// </summary>
		public ISensitiveSettings SensitiveSettings { get; }

		/// <summary>
		/// Gets the additional stream writers.
		/// </summary>
		public IEnumerable<IStreamWriter> StreamWriters { get; }

		/// <summary>
		/// Gets or sets whether to beautify (debug) console output by stripping
		/// entity and record separators (true.) The output matches the disk file
		/// if false.
		/// </summary>
		public bool IsConsoleOutputBeautified { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LogfileConfiguration"/> class.
		/// </summary>
		/// <param name="appName">The name of the application.</param>
		/// <param name="writeToConsole">Whether writing to the console is enabled.</param>
		/// <param name="writeToDebugConsole">Whether writing to the debug console is enabled.</param>
		/// <param name="writeToDisk">Whether writing to a file on a
		///		disk is enabled. If not, only the
		///		<paramref name="additionalStreamWriters"/> will be used.</param>
		///	<param name="path">The path to save logfiles to.</param>
		///	<param name="fileNameFormat">The format for logfile names. Use <c>{app-name}</c>
		///		as placeholder for the <paramref name="appName"/>,
		///		<c>{start-up-time}</c> as placeholder for the application start-up time,
		///		<c>{creation-time}</c> as placeholder for the logfile creation time,
		///		and <c>{seq-no}</c> as placeholder for the application instance
		///		sequence number.</param>
		///	<param name="maximumLogfileSize">The maximum logfile size in bytes. If
		///		this size has been exceeded, a new logfile will be created
		///		(logfile rotation.) Null to disable logfile rotation.</param>
		///	<param name="keepLogfiles">Number of logfiles to keep in the
		///		<paramref name="path"/>. Null to disable logfiles cleanup.</param>
		///	<param name="logEventDetailFormatters">The log event detail formatters, identified
		///		by their log event detail type.</param>
		///	<param name="sensitiveSettings">The sensitive data settings, can be null
		///		if not configured.</param>
		///	<param name="additionalStreamWriters">The additional stream writers.</param>
		/// <param name="isConsoleOutputBeautified">true to beautify the (debug) console output,
		///		false to match (debug) console output the disk file output.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if either
		///		<paramref name="maximumLogfileSize"/> or <paramref name="keepLogfiles"/>
		///		is less than or equal to zero.</exception>
		///	<exception cref="ArgumentNullException">Thrown if
		///		<paramref name="logEventDetailFormatters"/> or
		///		<paramref name="additionalStreamWriters"/> is null.</exception>
		public StructuredLogfileConfiguration(string appName, bool writeToConsole, bool writeToDebugConsole,
			bool writeToDisk, string path, string fileNameFormat, int? maximumLogfileSize, int? keepLogfiles,
			IReadOnlyDictionary<Type, ILogEventDetailFormatter> logEventDetailFormatters,
			ISensitiveSettings sensitiveSettings, IEnumerable<IStreamWriter> additionalStreamWriters,
			bool isConsoleOutputBeautified)
		{
			this.AppName = appName ?? (System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly())?.GetName().Name ?? DefaultAppName;
			this.WriteToConsole = writeToConsole;
			this.WriteToDebugConsole = writeToDebugConsole;
			this.WriteToDisk = writeToDisk;
			this.Path = path ?? DefaultPath;
			this.FileNameFormat = fileNameFormat ?? DefaultFileNameFormat;

			if (maximumLogfileSize <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLogfileSize));
			this.MaximumLogfileSize = maximumLogfileSize ?? DefaultMaximumLogfileSize;

			if (keepLogfiles < 0) throw new ArgumentOutOfRangeException(nameof(keepLogfiles));
			this.KeepLogfiles = keepLogfiles ?? DefaultKeepLogfiles;

			this.LogEventDetailFormatters = logEventDetailFormatters ?? throw new ArgumentNullException(nameof(logEventDetailFormatters));
			this.SensitiveSettings = sensitiveSettings;
			this.StreamWriters = additionalStreamWriters ?? throw new ArgumentNullException(nameof(additionalStreamWriters));
			this.IsConsoleOutputBeautified = isConsoleOutputBeautified;
		}
	}
}
