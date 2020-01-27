using Logfile.Structured.Elements;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Logfile.Structured.StreamReaders
{
	public class StructuredLogfileReader<TLoglevel>
		where TLoglevel : Enum
	{
		#region Constants

		private const int SingleBufferSize = 4096;
		private const int MaximumBufferSize = 32768;

		#endregion

		#region Fields

		private readonly Stream stream;
		private byte[] buffer = new byte[0];
		private bool wasHeaderRead = false;

		#endregion


		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="StructuredLogfileReader"/> class.
		/// </summary>
		/// <param name="stream">The input stream.</param>
		/// <exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="stream"/> is null.</exception>
		///	<exception cref="ArgumentException">Thrown, if the
		///		<paramref name="stream"/> is not readable.</exception>
		public StructuredLogfileReader(Stream stream)
		{
			this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
			if (!stream.CanRead) throw new ArgumentException("Stream is not readable.");
		}

		#endregion

		#region Methods

		/// <summary>
		/// Reads the next element from the stream.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the execution.</param>
		/// <returns>The read element.</returns>
		/// <exception cref="IOException">Thrown, if failed to read from the stream.</exception>
		/// <exception cref="InvalidOperationException">Thrown, if failed to parse the read data.</exception>
		/// <exception cref="OperationCanceledException">Thrown, if  the
		///		<paramref name="cancellationToken"/> was canceled.</exception>
		public async Task<IElement<TLoglevel>> ReadNextElementAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				// Repeat reading bytes from stream until either a logfile element
				// could be interpreted from the data or the buffer is full.
				bool first = true;
				int readBytes = 0;
				while (true)
				{
					// Only read more data if this is either not the first loop iteration
					// (thus more data required) or there is no more data in the buffer.
					if (!first || this.buffer.Length == 0)
					{
						var buf = new byte[SingleBufferSize];
						readBytes = await stream.ReadAsync(
							buffer: buf,
							offset: 0,
							count: Math.Min(buf.Length, MaximumBufferSize - this.buffer.Length),
							cancellationToken: cancellationToken).ConfigureAwait(false);
						if (readBytes == 0)
						{
							if (this.buffer.Length != 0)
								throw new InvalidOperationException("Incomplete element.");
							else
								return null; // No more elements.
						}

						var newData = new byte[readBytes];
						Array.Copy(buf, 0, newData, 0, readBytes);
						this.buffer = this.buffer.Concat(newData).ToArray();
					}
					first = false;

					if (this.buffer.Length >= MaximumBufferSize)
					{
						// Cannot be tested as this situation could only be tested if a single entity
						// was larger than the buffer size.
						throw new InvalidOperationException("Buffer full.");
					}

					// Try to interpret data.
					int consumedData = 0;
					IElement<TLoglevel> nextElement = null;
					do
					{
						if (!wasHeaderRead)
						{
							// Header expected first.
							var (moreDataRequired, isCompatible) = Header<TLoglevel>.Identify(data: this.buffer);
							if (!moreDataRequired)
							{
								if (!isCompatible)
									throw new InvalidOperationException("Expecting the header first.");
								var result = Header<TLoglevel>.Parse(data: this.buffer);
								if (!result.MoreDataRequired)
								{
									nextElement = result.Element;
									consumedData = result.ConsumedData;
									wasHeaderRead = true;
								}
								else
								{
									// Cannot be tested as this situtation should never occur.
									throw new InvalidOperationException("Internal error: Parser should have been able to interpret the data but reports that more data is required.");
								}
							}
						}
						else
						{
							// Any element expected.
							// TODO Skip for now. This needs to be implemented at some later point.
							return null;
						}

						if (consumedData > 0)
						{
							// Remove processed data from buffer.
							var tempBuffer = new byte[this.buffer.Length - consumedData];
							Array.Copy(this.buffer, consumedData, tempBuffer, 0, tempBuffer.Length);
							this.buffer = tempBuffer;
						}

						// Return next element if parsed successfully.
						if (nextElement != null) return nextElement;
					} while (consumedData > 0);
				}
			}
			catch (Exception ex) when (!(ex is IOException) && !(ex is InvalidOperationException) && !(ex is OperationCanceledException))
			{
				throw new InvalidOperationException("Failed to parse the data.", ex);
			}
		}

		#endregion
	}
}
