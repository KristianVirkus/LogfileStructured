using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Logfile.Structured.StreamWriters
{
	/// <summary>
	/// Implements an adapter to output structured log data to
	/// arbitrary <see cref="TextWriter"/>s.
	/// </summary>
	public class Text : IStreamWriter
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

		#region IStreamWriter implementation

		public void Dispose()
		{
			// Do not close the stream created outside this class.
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
