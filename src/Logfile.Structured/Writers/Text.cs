using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Logfile.Structured.Writers
{
	/// <summary>
	/// Implements an adapter to output structured log data to
	/// arbitrary <see cref="TextWriter"/>s.
	/// </summary>
	public class Text : ITextWriter
	{
		#region Fields

		readonly System.IO.TextWriter textWriter;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Text"/> class.
		/// </summary>
		/// <param name="textWriter">The text writer to write to.</param>
		/// <exception cref="ArgumentNullException">Thrown if
		///		<paramref name="textWriter"/> is null.</exception>
		public Text(System.IO.TextWriter textWriter)
		{
			this.textWriter = textWriter ?? throw new ArgumentNullException(nameof(textWriter));
		}

		#endregion

		#region IWriter implementation

		public void Dispose()
		{
			// Do not close the stream created outside this class.
			this.FlushAsync(default).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public async Task FlushAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				await this.textWriter.FlushAsync();
			}
			catch
			{
				// Ignore any exception as the text writer might have been disposed
				// or it just failed to flush its cache.
			}
		}

		public async Task WriteAsync(string text, CancellationToken cancellationToken)
		{
			try
			{
				await this.textWriter.WriteAsync(text);
			}
			catch
			{
				// Ignore any exception as the text writer might have been disposed
				// or it just failed to process the data.
			}
		}

		#endregion
	}
}
