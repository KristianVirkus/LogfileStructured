﻿using EventRouter.Core;
using Logfile.Core;
using Logfile.Structured.Elements;
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

		/// <summary>
		/// Flushes the file write cache and thus actually writes it to the disk.
		/// </summary>
		/// <param name="cancellationToken">The <c>CancellationToken</c> to abort the process.</param>
		public async Task FlushAsync(CancellationToken cancellationToken)
		{
			await this.sync.WaitAsync(cancellationToken);
			try
			{
				if (this.fileStream != null)
					await this.fileStream.FlushAsync(cancellationToken);
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
									// Clean up old logfiles.
									this.cleanUpOldLogfiles();

									// Initialize new file.
									++this.fileSequenceNo;
									if (!Directory.Exists(this.configuration.Path))
										Directory.CreateDirectory(this.configuration.Path);
									var filePath = Path.Combine(this.configuration.Path, this.configuration.BuildFileName(this.fileSequenceNo));
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

						// Set beautified text to text if no beautification is wanted, otherwise
						// keep it null until required for the first time.
						string beautifiedText = (configuration.IsConsoleOutputBeautified ? null : text);

						// Write to console if enabled.
						if (this.configuration.WriteToConsole)
						{
							try
							{
								beautifiedText = beautifiedText ?? this.beautifyText(text);
								Console.Write(beautifiedText);
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
								beautifiedText = beautifiedText ?? this.beautifyText(text);
								Debug.Write(beautifiedText);
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

		private void cleanUpOldLogfiles()
		{
			var config = this.configuration;
			if (config == null) return;

			try
			{
				// Create map of matching files with their creation dates and sequence numbers.
				var list = new List<(DateTime CreationDate, int SequenceNo, string FileName)>();
				// Find files matching the file name format.
				var fileNames = Directory.EnumerateFiles(this.configuration.Path);
				foreach (var fileName in fileNames)
				{
					// TODO Parse file header.
					try
					{
						using (var fileStream = File.Open(Path.Combine(this.configuration.Path, fileName), FileMode.Open, FileAccess.Read, FileShare.Read))
						{
							// TODO
						}
					}
					catch (Exception ex)
					{
						// TODO Log.
					}
				}

					// TODO Delete all files above threshold + 1 (as a new file is going to be created right now)
			}
			catch (Exception ex)
			{
				// TODO Log.
			}
		}

		/// <summary>
		/// Strips structural characters (from the structured logfile format) from a string.
		/// </summary>
		/// <param name="s">The string.</param>
		/// <returns>The processed text without structural characters.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="s"/> is null.</exception>
		string beautifyText(string s)
		{
			if (s == null) throw new ArgumentNullException(nameof(s));

			return s.Replace(Constants.EntitySeparator, "").Replace(Event.RecordSeparator, "");
		}

		#endregion
	}
}
