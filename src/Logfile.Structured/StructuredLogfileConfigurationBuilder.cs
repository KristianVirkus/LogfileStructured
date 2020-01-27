using Logfile.Core.Details;
using Logfile.Structured.Formatters;
using System;
using System.Collections.Generic;

namespace Logfile.Structured
{
	/// <summary>
	/// Implements a builder for the structured logfiles configuration.
	/// </summary>
	/// <typeparam name="TLoglevel">The loglevel type.</typeparam>
	public class StructuredLoglevelConfigurationBuilder<TLoglevel>
		where TLoglevel : Enum
	{
		/// <summary>
		/// Gets or sets the application name.
		/// </summary>
		public string AppName { get; set; }

		/// <summary>
		/// Gets or sets whether writing to the console is enabled.
		/// </summary>
		public bool WriteToConsole { get; set; }

		/// <summary>
		/// Gets or sets whether writing to the debug console is enabled.
		/// </summary>
		public bool WriteToDebugConsole { get; set; }

		/// <summary>
		/// Gets or sets whether writing to a file on a disk is enabled.
		/// If not, only the stream writers will be used.
		/// </summary>
		public bool WriteToDisk { get; set; } = true;

		/// <summary>
		/// Gets or sets the path to save the logfiles to.
		/// </summary>
		public string Path { get; set; } = "./logs";

		/// <summary>
		/// Gets or sets the format for logfile names.
		/// </summary>
		public string FileNameFormat { get; set; } = "{app-name}-{start-up-time}-{seq-no}.slf.log";

		/// <summary>
		/// Gets or sets the maximum logfile size in bytes and enables logfile rotation.
		/// </summary>
		public int? MaximumLogfileSize { get; set; } = 25 * 1024 * 1024;

		/// <summary>
		/// Gets or sets the number of logfiles to keep in the path and enables
		/// logfiles cleanup. Can be null to disable logfiles cleanup.
		/// </summary>
		public int? KeepLogfiles { get; set; } = 5;

		/// <summary>
		/// Gets or sets the sensitive data settings. Can be null if not configured.
		/// </summary>
		public ISensitiveSettings SensitiveSettings { get; set; }

		/// <summary>
		/// Gets or sets the log event detail formatters, identified by their log event types.
		/// </summary>
		public Dictionary<Type, ILogEventDetailFormatter> LogEventDetailFormatters { get; set; } = new Dictionary<Type, ILogEventDetailFormatter>();

		/// <summary>
		/// Gets or sets the additional stream writer.
		/// </summary>
		public List<ITextWriter> StreamWriters { get; set; } = new List<ITextWriter>();

		/// <summary>
		/// Gets or sets whether to beautify (debug) console output by stripping
		/// entity and record separators (true.) The output matches the disk file
		/// if false.
		/// </summary>
		public bool IsConsoleOutputBeautified { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="StructuredLoglevelConfigurationBuilder{TLoglevel}"/> class.
		/// </summary>
		public StructuredLoglevelConfigurationBuilder()
		{
			this.UseLogEventDetailFormatter(typeof(Core.Details.Binary), Structured.Formatters.Binary.Default);
			this.UseLogEventDetailFormatter(typeof(Core.Details.EventID), Structured.Formatters.EventID.Default);
			this.UseLogEventDetailFormatter(typeof(Core.Details.ExceptionDetail), Structured.Formatters.ExceptionDetail.Default);
			this.UseLogEventDetailFormatter(typeof(Core.Details.Message), Structured.Formatters.Message.Default);
		}

		/// <summary>
		/// Builds the immutable configuration object.
		/// </summary>
		/// <returns>The configuration object.</returns>
		public StructuredLogfileConfiguration<TLoglevel> Build()
		{
			return new StructuredLogfileConfiguration<TLoglevel>(
				this.AppName,
				this.WriteToConsole,
				this.WriteToDebugConsole,
				this.WriteToDisk,
				this.Path,
				this.FileNameFormat,
				this.MaximumLogfileSize,
				this.KeepLogfiles,
				this.LogEventDetailFormatters ?? new Dictionary<Type, ILogEventDetailFormatter>(),
				this.SensitiveSettings,
				this.StreamWriters,
				this.IsConsoleOutputBeautified);
		}
	}

	public static class ConfigurationBuilderExtensions
	{
		/// <summary>
		/// Sets the name of the logging application.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="appName">The application name.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/> or <paramref name="appName"/> is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> UseAppName<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder, string appName)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			configurationBuilder.AppName = appName ?? throw new ArgumentNullException(nameof(appName));
			return configurationBuilder;
		}

		/// <summary>
		/// Enables logging to the console.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/> is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> UseConsole<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			configurationBuilder.WriteToConsole = true;
			return configurationBuilder;
		}

		/// <summary>
		/// Enables logging to the debug console.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/> is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> UseDebugConsole<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			configurationBuilder.WriteToDebugConsole = true;
			return configurationBuilder;
		}

		/// <summary>
		/// Disables logging to a file on the disk.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/> is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> DoNotUseFileOnDisk<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			configurationBuilder.WriteToDisk = false;
			return configurationBuilder;
		}

		/// <summary>
		/// Sets the path to save logfiles to.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="path">The absolute path or relative to the application's working directory.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/> or <paramref name="path"/> is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> UsePath<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder, string path)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			configurationBuilder.Path = path ?? throw new ArgumentNullException(nameof(path));
			return configurationBuilder;
		}

		/// <summary>
		/// Sets the format for logfile names.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="fileNameFormat">The format for logfile names. Use <c>{app-name}</c>
		///		as placeholder for the application name,
		///		<c>{start-up-time}</c> as placeholder for the application start-up time,
		///		<c>{creation-time}</c> as placeholder for the logfile creation time,
		///		and <c>{seq-no}</c> as placeholder for the application instance
		///		sequence number.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/> or <paramref name="fileNameFormat"/>
		///		is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> UseFileNameFormat<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder, string fileNameFormat)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			configurationBuilder.FileNameFormat = fileNameFormat ?? throw new ArgumentNullException(nameof(fileNameFormat));
			return configurationBuilder;
		}

		/// <summary>
		/// Sets the maximum size of a single logfile in bytes when logfile rotation is
		/// enabled before creating the next logfile with an increased sequence number
		/// but the same instance ID. Also enables logfile rotation.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="size">The maximum logfile size in bytes.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="configurationBuilder"/>
		///		is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="size"/>
		///		is less than or equal to zero.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> RestrictLogfileSize<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder, int size)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			if (size <= 0) throw new ArgumentNullException(nameof(size));
			configurationBuilder.MaximumLogfileSize = size;
			return configurationBuilder;
		}

		/// <summary>
		/// Sets the maximum number of logfiles in the configured path to keep when rotating
		/// logfiles.
		/// </summary>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="count">The maximum number of logfiles in addition to the current one
		///		to keep in the configured path. Use null to disable logfiles clean-up.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="configurationBuilder"/>
		///		is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/>
		///		is less than or equal to zero.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> KeepLogfiles<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder, int count)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			if (count <= -1) throw new ArgumentOutOfRangeException(nameof(count));
			configurationBuilder.KeepLogfiles = count;
			return configurationBuilder;
		}

		/// <summary>
		/// Sets the <paramref name="logEventDetailFormatter"/> for a specific
		/// <paramref name="logEventDetailType"/>.
		/// </summary>
		/// <typeparam name="TLoglevel">The loglevel type.</typeparam>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="logEventDetailType">The log event detail type.</param>
		/// <param name="logEventDetailFormatter">The log event detail formatter instance.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/>, <paramref name="logEventDetailType"/>,
		///		or <paramref name="logEventDetailFormatter"/> is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> UseLogEventDetailFormatter<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder, Type logEventDetailType,
			ILogEventDetailFormatter logEventDetailFormatter)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			if (logEventDetailType == null) throw new ArgumentNullException(nameof(logEventDetailType));
#pragma warning disable IDE0016 // Use 'throw' expression
			if (logEventDetailFormatter == null) throw new ArgumentNullException(nameof(logEventDetailFormatter));
#pragma warning restore IDE0016 // Use 'throw' expression

			if (configurationBuilder.LogEventDetailFormatters == null)
				configurationBuilder.LogEventDetailFormatters = new Dictionary<Type, ILogEventDetailFormatter>();
			configurationBuilder.LogEventDetailFormatters[logEventDetailType] = logEventDetailFormatter;

			return configurationBuilder;
		}

		/// <summary>
		/// Sets the settings for sensitive data.
		/// </summary>
		/// <typeparam name="TLoglevel">The loglevel type.</typeparam>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="sensitiveSettings">The settings for sensitive data.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/> or <paramref name="sensitiveSettings"/>
		///		is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> UseSensitiveSettings<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder, ISensitiveSettings sensitiveSettings)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			configurationBuilder.SensitiveSettings = sensitiveSettings ?? throw new ArgumentNullException(nameof(sensitiveSettings));
			return configurationBuilder;
		}

		/// <summary>
		/// Uses the <paramref name="additionalStreamWriter"/>.
		/// </summary>
		/// <typeparam name="TLoglevel">The loglevel.</typeparam>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <param name="additionalStreamWriter">The additional stream writer.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/> or <paramref name="additionalStreamWriter"/>
		///		is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> UseWriter<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder,
			ITextWriter additionalStreamWriter)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			if (additionalStreamWriter == null) throw new ArgumentNullException(nameof(additionalStreamWriter));

			if (configurationBuilder.StreamWriters == null)
				configurationBuilder.StreamWriters = new List<ITextWriter>();
			configurationBuilder.StreamWriters.Add(additionalStreamWriter);
			return configurationBuilder;
		}

		/// <summary>
		/// Instructs the router to beautify the output for the (debug) console.
		/// </summary>
		/// <typeparam name="TLoglevel">The loglevel type.</typeparam>
		/// <param name="configurationBuilder">The configuration builder.</param>
		/// <returns>The same configuration builder instance to allow a fluent syntax.</returns>
		/// <exception cref="ArgumentNullException">Thrown if either
		///		<paramref name="configurationBuilder"/> or <paramref name="sensitiveSettings"/>
		///		is null.</exception>
		public static StructuredLoglevelConfigurationBuilder<TLoglevel> BeautifyConsoleOutput<TLoglevel>(
			this StructuredLoglevelConfigurationBuilder<TLoglevel> configurationBuilder)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null) throw new ArgumentNullException(nameof(configurationBuilder));
			configurationBuilder.IsConsoleOutputBeautified = true;
			return configurationBuilder;
		}
	}
}
