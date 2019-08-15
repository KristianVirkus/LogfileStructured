using EventRouter.Core;
using Logfile.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Logfile.Structured
{
	/// <summary>
	/// Implements the router for structured logfiles.
	/// </summary>
	/// <typeparam name="TLoglevel">The loglevel type.</typeparam>
	public class Router<TLoglevel> : IRouter<LogEvent<TLoglevel>>
		where TLoglevel : Enum
	{
		readonly SemaphoreSlim sync = new SemaphoreSlim(1);
		StructuredLogfileConfiguration<TLoglevel> configuration;
		FileStream fileStream = null;
		int fileSequenceNo = 0;
		long bytesWrittenToFile = 0;

		public async Task ReconfigureAsync(StructuredLogfileConfiguration<TLoglevel> configuration, CancellationToken cancellationToken)
		{
			// Just call base configuration change, set new logfile configuration and
			// tolerate some time of inconsistency. Reconfiguring on the fly is just not
			// a usual use case.
			await this.sync.WaitAsync(cancellationToken);
			try
			{
				this.configuration = configuration;
			}
			finally
			{
				this.sync.Release();
			}
		}

		#region IRouter implementation

		public Task StartAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public async Task ForwardAsync(IEnumerable<LogEvent<TLoglevel>> routables, CancellationToken cancellationToken)
		{
			if (routables == null) throw new ArgumentNullException(nameof(routables));

			cancellationToken.ThrowIfCancellationRequested();

			await this.sync.WaitAsync(cancellationToken);
			try
			{
				try
				{
					foreach (var routable in routables)
					{
						cancellationToken.ThrowIfCancellationRequested();

						var text = new Elements.Event<TLoglevel>(routable).Serialize(this.configuration);
						var data = Encoding.UTF8.GetBytes(text);

						// Write to file if enabled.
						if (this.configuration.WriteToDisk)
						{
							try
							{
								if (this.fileStream == null)
								{
									// Initialize new file.
									++this.fileSequenceNo;
									if (!Directory.Exists(this.configuration.Path))
										Directory.CreateDirectory(this.configuration.Path);
									var filePath = Path.Combine(this.configuration.Path, getFileName());
									this.fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
								}

								await this.fileStream.WriteAsync(data, 0, data.Length);
								this.bytesWrittenToFile += data.Length;

								if (this.bytesWrittenToFile >= this.configuration.MaximumLogfileSize)
								{
									// Enough data written per file. Flush to disk and prepare
									// everything to have a new file created when the next
									// events are to be written.
									try
									{
										await this.fileStream.FlushAsync();
									}
									finally
									{
										try
										{
											this.fileStream.Dispose();
										}
										finally
										{
											this.fileStream = null;
											this.bytesWrittenToFile = 0;
										}
									}
								}
							}
							catch
							{
								// TODO Log.
							}
						}

						// Write to console if enabled.
						if (this.configuration.WriteToConsole)
						{
							try
							{
								Console.Write(text);
							}
							catch
							{
								// TODO Log.
							}
						}

						// Write to debug console if enabled.
						if (this.configuration.WriteToConsole)
						{
							try
							{
								Debug.Write(text);
							}
							catch
							{
								// TODO Log.
							}
						}

						foreach (var streamWriter in this.configuration.StreamWriters)
						{
							cancellationToken.ThrowIfCancellationRequested();
							try
							{
								await streamWriter.WriteAsync(text, cancellationToken);
							}
							catch
							{
								// TODO Log.
							}
						}
					}
				}
				catch
				{
					// TODO Log.
				}
			}
			finally
			{
				this.sync.Release();
			}
		}

		/// <summary>
		/// Replaces common placeholders in the set-up filename format with
		/// actual values from the context.
		/// </summary>
		/// <returns>The final filename.</returns>
		string getFileName() => this.configuration.FileNameFormat
			.Replace("{app-name}", this.configuration.AppName)
			.Replace("{start-up-time}", System.Diagnostics.Process.GetCurrentProcess().StartTime.ToString("yyyyMMdd-HHmmssfff"))
			.Replace("{creation-time}", DateTime.Now.ToString("yyyyMMdd-HHmmssfff"))
			.Replace("{seq-no}", this.fileSequenceNo.ToString());

		#endregion
	}
}
