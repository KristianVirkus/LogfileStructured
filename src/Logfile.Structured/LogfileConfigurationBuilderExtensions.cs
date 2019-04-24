using Logfile.Core;
using System;

namespace Logfile.Structured
{
	/// <summary>
	/// Implements extension methods to ease adding a structured logfile as
	/// a router to a logfile framework configuration.
	/// </summary>
	public static class LogfileConfigurationBuilderExtensions
	{
		/// <summary>
		/// Adds a new structured logfile router to the framework's logfile configuration.
		/// Allows via <paramref name="configureCallback"/> to customize the
		/// structured logfile router's configuration.
		/// </summary>
		/// <typeparam name="TLoglevel">The loglevel type.</typeparam>
		/// <param name="configurationBuilder">The framework's logfile configuration.</param>
		/// <param name="configureCallback">The callback to invoke for customizing
		///		the structured logfile router's configuration. The single argument
		///		is an already initialized configuration builder. This argument may
		///		be null if no specific configuration is required.</param>
		/// <returns>The same <paramref name="configurationBuilder"/> instance to
		///		allow a fluent configuration syntax.</returns>
		///	<exception cref="ArgumentNullException">Thrown if
		///		<paramref name="configurationBuilder"/> is null.</exception>
		///	<exception cref="Exception">Thrown if any error occurred while configuring
		///		the structured logfile router's configuration or while applying it.</exception>
		public static LogfileConfigurationBuilder<TLoglevel> AddStructuredLogfile<TLoglevel>(
			this LogfileConfigurationBuilder<TLoglevel> configurationBuilder,
			Action<StructuredLoglevelConfigurationBuilder<TLoglevel>> configureCallback)
			where TLoglevel : Enum
		{
			if (configurationBuilder == null)
			{
				throw new ArgumentNullException(nameof(configurationBuilder));
			}

			var structuredConfigurationBuilder = new StructuredLoglevelConfigurationBuilder<TLoglevel>();
			configureCallback?.Invoke(structuredConfigurationBuilder);

			// Add to Logfile.Core configuration.
			var router = new Router<TLoglevel>();
			router.ReconfigureAsync(structuredConfigurationBuilder.Build(), default).GetAwaiter().GetResult();
			configurationBuilder.AddRouter(router);

			return configurationBuilder;
		}
	}
}
