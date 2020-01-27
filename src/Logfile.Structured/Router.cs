using EventRouter.Core;
using Logfile.Core;
using Logfile.Structured.Elements;
using Logfile.Structured.StreamReaders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		IFileSystem fileSystem = new FileSystem();

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
									// TODO Issue #32 Abstract file system to increase testability
									if (Directory.Exists(this.configuration.Path))
									{
										// Clean up old logfiles.
										await this.CleanUpOldLogfilesAsync(this.fileSystem, cancellationToken).ConfigureAwait(false);
									}
									else
									{
										// Initialize directory.
										Directory.CreateDirectory(this.configuration.Path);
									}

									// Open new logfile.
									++this.fileSequenceNo;
									var startUpTime = DateTime.Now;
									var filePath = Path.Combine(this.configuration.Path, this.configuration.BuildFileName(this.fileSequenceNo));
									this.fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);

									// Write file header.
									var misc = new Dictionary<string, string>();
									var header = new Header<TLoglevel>(
										configuration.AppName,
										startUpTime,
										this.fileSequenceNo,
										new ReadOnlyDictionary<string, string>(misc));
									var headerData = ContentEncoding.Encoding.GetBytes(header.Serialize(this.configuration));
									await this.fileStream.WriteAsync(headerData, 0, headerData.Length);
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

		internal async Task CleanUpOldLogfilesAsync(IFileSystem fileSystem, CancellationToken cancellationToken)
		{
			if (fileSystem == null) throw new ArgumentNullException(nameof(fileSystem));

			cancellationToken.ThrowIfCancellationRequested();

			var config = this.configuration;
			if (config == null) return;
			if ((config.KeepLogfiles ?? -1) < 0) return; // If to keep all logfiles, abort now.

			try
			{
				// Create map of matching files with their creation dates and sequence numbers.
				var list = new List<(DateTime StartUpTime, int SequenceNo, string FilePath)>();
				// Find files matching the file name format.
				var fileNames = fileSystem.EnumerateFiles(this.configuration.Path).Select(n => Path.GetFileName(n));

				// Filter by fixed file name beginning and ending.
				var exampleFileName = config.BuildFileName(1);
				var fileNamePattern = config.FileNameFormat.Replace("{app-name}", config.AppName); // Replace app-name as it is configurable but then static and does not change with any subsequently generated logfile.
				var commonBeginning = FindCommonBeginning(a: exampleFileName, b: fileNamePattern);
				var commonEnding = new string(FindCommonBeginning(
									a: new string(exampleFileName.Reverse().ToArray()),
									b: new string(fileNamePattern.Reverse().ToArray())).Reverse().ToArray());
				fileNames = fileNames.Where(f => f.StartsWith(commonBeginning) && f.EndsWith(commonEnding));

				foreach (var fileName in fileNames)
				{
					cancellationToken.ThrowIfCancellationRequested();

					// Parse file headers.
					try
					{
						var filePath = Path.Combine(this.configuration.Path, fileName);
						using (var fileStream = fileSystem.OpenForReading(filePath))
						{
							var reader = new StructuredLogfileReader<TLoglevel>(fileStream);
							var header = (Header<TLoglevel>)(await reader.ReadNextElementAsync(cancellationToken).ConfigureAwait(false));
							list.Add((StartUpTime: header.AppStartUpTime, SequenceNo: header.AppInstanceLogfileSequenceNumber, FilePath: filePath));
						}
					}
					catch (Exception ex)
					{
						// TODO Log.
					}
				}

				// Delete all files above threshold + 1 (as a new file is going to be created right now)
				var orderedList = from l in list
								  orderby l.StartUpTime, l.SequenceNo
								  select l.FilePath;
				foreach (var filePathToDelete in orderedList
													.Take(Math.Min(
															orderedList.Count(),
															orderedList.Count() - config.KeepLogfiles.Value)) // add +1 if new logfile should considered part of the "logfiles to keep"
													.ToList())
				{
					cancellationToken.ThrowIfCancellationRequested();

					try
					{
						fileSystem.DeleteFile(filePathToDelete);
					}
					catch (Exception ex)
					{
						// TODO Log.
					}
				}
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

		/// <summary>
		/// Finds the common beginning of two strings.
		/// </summary>
		/// <param name="a">The first string.</param>
		/// <param name="b">The second string.</param>
		/// <returns>The common beginning.</returns>
		/// <exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="a"/> or <paramref name="b"/> is null.</exception>
		public static string FindCommonBeginning(string a, string b)
		{
			if (a == null) throw new ArgumentNullException(nameof(a));
			if (b == null) throw new ArgumentNullException(nameof(b));

			var shorter = (a.Length < b.Length ? a : b).AsEnumerable();
			var longer = (a.Length >= b.Length ? a : b).AsEnumerable();
			return new string(shorter.TakeWhile((c, i) => longer.ElementAt(i) == c).ToArray());
		}

		#endregion

		#region Nested types

		/// <summary>
		/// Implements file handling for files and directories.
		/// Not testable as directly tied to file system operations.
		/// </summary>
		class FileSystem : IFileSystem
		{
			/// <inherit />
			public void DeleteFile(string filePath) => File.Delete(filePath);

			/// <inherit />
			public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

			/// <inherit />
			public Stream OpenForReading(string filePath) => File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		#endregion
	}
}
