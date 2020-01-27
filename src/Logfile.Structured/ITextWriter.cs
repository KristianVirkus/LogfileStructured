using System;
using System.Threading;
using System.Threading.Tasks;

namespace Logfile.Structured
{
	/// <summary>
	/// Common interface of all stream writers.
	/// </summary>
	public interface ITextWriter : IDisposable
	{
		/// <summary>
		/// Writes out a text.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="cancellationToken">The cancellation token to cancel
		///		the operation.</param>
		/// <exception cref="ArgumentNullException">Thrown if
		///		<paramref name="text"/> is null.</exception>
		///	<exception cref="ObjectDisposedException">Thrown if
		///		the instance has already been disposed.</exception>
		///	<exception cref="OperationCanceledException">Thrown if
		///		the operation was canceled.</exception>
		///	<exception cref="Exception">Thrown if any error occurred
		///		during writing out the data.</exception>
		Task WriteAsync(string text, CancellationToken cancellationToken);

		/// <summary>
		/// Flushes any underlying cache.
		/// </summary>
		/// <param name="cancellationToken">The <c>CancellationToken</c> to abort the process.</param>
		/// <exception cref="OperationCanceledException">Thrown, if the operation
		///		was canceled.</exception>
		///	<exception cref="Exception">Thrown, if flushing the caches failed.</exception>
		Task FlushAsync(CancellationToken cancellationToken);
	}
}
