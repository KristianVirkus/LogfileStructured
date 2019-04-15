using System;
using System.Threading;
using System.Threading.Tasks;

namespace Logfile.Structured
{
	/// <summary>
	/// Common interface of all stream writers.
	/// </summary>
	public interface IStreamWriter : IDisposable
	{
		/// <summary>
		/// Writes out a text.
		/// </summary>
		/// <param name="text">The UTF-8-encoded text.</param>
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
	}
}
