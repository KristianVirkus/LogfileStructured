using Logfile.Structured.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

		private readonly int HeaderIdentityLength;

		private readonly Encoding encoding;
		private readonly Stream stream;
		private byte[] buffer;
		private bool wasHeaderRead = false;

		#endregion


		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="StructuredLogfileReader"/> class.
		/// </summary>
		/// <param name="stream">The input stream.</param>
		/// <param name="encoding">The encoding to read the stream data with. UTF-8 if null.</param>
		/// <exception cref="ArgumentNullException">Thrown, if
		///		<paramref name="stream"/> is null.</exception>
		///	<exception cref="ArgumentException">Thrown, if the
		///		<paramref name="stream"/> is not readable.</exception>
		public StructuredLogfileReader(Stream stream, Encoding encoding = null)
		{
			this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
			if (!stream.CanRead) throw new ArgumentException("Stream is not readable.");

			this.encoding = encoding ?? Encoding.UTF8;
			this.HeaderIdentityLength = this.encoding.GetBytes(Header<TLoglevel>.LogfileIdentity).Length;
		}

		#endregion

		#region Methods

		public async Task<IElement<TLoglevel>> ReadNextElementAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Repeat reading bytes from stream until either a logfile element
			// could be interpreted from the data or the buffer is full.
			int readBytes = 0;
			while (readBytes > 0)
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

				// Try to interpret data.
				int processedData = 0;
				if (!wasHeaderRead)
				{
					// Header expected first.
					var (moreDataRequired, isCompatible) = Header<TLoglevel>.Identify(data: this.buffer, encoding: encoding);
					if (!moreDataRequired)
					{
						if (!isCompatible)
							throw new InvalidOperationException("Expected header.");
						wasHeaderRead = true;
					}
				}
				else
				{
					// Any element expected.
					// TODO Skip for now. This needs to be implemented at some later point.
					return null;
				}

				if (processedData > 0)
				{
					// Remove processed data from buffer.
					var tempBuffer = new byte[this.buffer.Length - processedData];
					Array.Copy(this.buffer, processedData, tempBuffer, 0, tempBuffer.Length);
					this.buffer = tempBuffer;
				}

				if (this.buffer.Length == MaximumBufferSize)
					throw new InvalidOperationException("Buffer full.");
			}

			return null;
		}

		#endregion
	}
}
