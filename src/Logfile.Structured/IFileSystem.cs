using System;
using System.Collections.Generic;
using System.IO;

namespace Logfile.Structured
{
	/// <summary>
	/// Common interface of all file system interfaces.
	/// </summary>
	internal interface IFileSystem
	{
		/// <summary>
		/// Enumerates all files in the <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to search.</param>
		/// <returns>The list of file names without the path.</returns>
		/// <exception cref="Exception">Thrown, if enumerating files fails.</exception>
		IEnumerable<string> EnumerateFiles(string path);

		/// <summary>
		/// Opens a file for reading.
		/// </summary>
		/// <param name="filePath">The file path.</param>
		/// <returns>The data stream.</returns>
		/// <exception cref="Exception">Thrown, if opening the file fails.</exception>
		Stream OpenForReading(string filePath);

		/// <summary>
		/// Deleted a file.
		/// </summary>
		/// <param name="filePath">The file path.</param>
		/// <exception cref="Exception">Thrown, if deleting the file fails.</exception>
		void DeleteFile(string filePath);
	}
}
