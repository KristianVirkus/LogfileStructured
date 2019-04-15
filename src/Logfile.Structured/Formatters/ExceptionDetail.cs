using Logfile.Structured.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Logfile.Structured.Formatters
{
	/// <summary>
	/// Implements a formatter for exception log event details.
	/// </summary>
	public class ExceptionDetail : ILogEventDetailFormatter
	{
		#region Constants

		/// <summary>
		/// Gets the log event detail's identification string.
		/// </summary>
		public const string Identification = "Exception";

		#endregion

		#region Static members

		/// <summary>
		/// Gets the default instance.
		/// </summary>
		public static ExceptionDetail Default { get; }

		#endregion

		#region Constructors

		static ExceptionDetail()
		{
			Default = new ExceptionDetail();
		}

		private ExceptionDetail()
		{
		}

		#endregion

		#region ILogEventDetailFormatter implementation

		public IEnumerable<Type> SupportedLogEventDetailsTypes => new[] { typeof(Logfile.Core.Details.ExceptionDetail) };

		public string ID => Identification;

		public string Format(object detail)
		{
			if (detail == null) throw new ArgumentNullException(nameof(detail));
			if (!(detail is Logfile.Core.Details.ExceptionDetail exception)) throw new NotSupportedException(nameof(detail));

			var text = new StringBuilder();
			var ex = exception.ExceptionObject;
			while (ex != null)
			{
				if (text.Length > 0)
					text.Append(Constants.NewLine);

				text.Append(ex.ToString());

				ex = ex.InnerException;
			}

			return text.ToString();
		}

		#endregion
	}
}
