﻿using Logfile.Core;
using Logfile.Core.Details;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logfile.Structured.SampleApp
{
	class Program
	{
		public static async Task Main()
		{
			Console.WriteLine("This is a structured logfile sample application, hello.");

			Console.WriteLine("Building logfile configuration...");

			// The structured logfile router can either be prepared this way or using
			// the "AddStructuredLogfile" extension method on the logfile framework's
			// configuration builder.
			//var structuredLogfileConfiguration = new ConfigurationBuilder<StandardLoglevel>()
			//	.UseAppName("SampleApp")
			//	.UseFileNameFormat("{app-name}-{start-up-time}-{seq-no}.s.log")
			//	.UsePath(".")
			//	.UseConsole()
			//	.UseDebugConsole()
			//	.Build();
			//var structuredLogfileRouter = new Router<StandardLoglevel>();
			//await structuredLogfileRouter.ReconfigureAsync(structuredLogfileConfiguration, default);

			// The configuration (extension) methods are ordered from specific to general.
			// This is required to make use of the fluent syntax.
			var logfileConfiguration = new LogfileConfigurationBuilder<StandardLoglevel>()
				.AllowLoglevels(StandardLoglevel.Warning, StandardLoglevel.Critical)
				.EnableDeveloperMode()
				.UseLogEventsFromExceptionData()
				//.AddRouter(structuredLogfileRouter) // Alternative to the lines below.
				.AddStructuredLogfile((_builder) =>
				{
					_builder
						.UseAppName("SampleApp")
						.UseFileNameFormat("{app-name}-{start-up-time}-{seq-no}.s.log")
						.UsePath("Logs")
						.KeepLogfiles(5)
						.UseConsole()
						.UseDebugConsole()
						.BeautifyConsoleOutput();
				})
				.Build();

			Console.WriteLine("Initializing the logfile instance...");

			var logfile = new StandardLogfile();
			await logfile.ReconfigureAsync(logfileConfiguration, default);

			Console.WriteLine("Generating log events...");

			logfile.Info.Force.Msg("This is the logfile.").Log();
			logfile.Info.Msg("As this message is not forced and the minimum loglevel had been set above Information, this text will not appear in the logfile.").Log();
			logfile.Warning.Msg("Log event not terminated by '.Log()' will not be logged at all.");
			logfile.Error.Msg("The text contains some % characters which need to be `encoded` before writing.").Log();

			logfile.Error.Developer.Msg("Due to developer mode, this will be printed regardless of the configured loglevels.");

			var data = Encoding.UTF8.GetBytes("TestTestTestTestTestTestTestTestTest");
			logfile.Error.Binary(data).Log();

			var exception1 = new InvalidOperationException("This is an exception as error.");
			logfile.Error.Exception(exception1).Log();

			try
			{
				throw new InvalidOperationException("This is an exception as critical.", new ArgumentException());
			}
			catch (Exception exception2)
			{
				logfile.Critical.Exception(exception2).Log();
			}

			var exceptionWithData = new DivideByZeroException("Never attempt to divide by zero.");
			exceptionWithData.AddLogEvent(StandardLoglevel.Error).Msg("This comes from within an exception object. This is logging without a logger reference.");
			exceptionWithData.AddLogEvent(StringSplitOptions.RemoveEmptyEntries).Msg("Unsupported loglevel will not be logged.");

			logfile.Warning.Event(TestEvent.One, 1, 2, 3).Log();
			logfile.Warning.Event(TestEvent.Two).Log();
			logfile.Warning.Event(TestEvent.Two, "arg").Log();

			// Expected output:
			// This is the logfile.
			// Due to developer mode, this will be printed regardless of the configured loglevels.
			// This is an exception as error.
			// This is an exception as critical.
			// Never attempt to divide by zero.
			// This comes from within an exception object. This is logging without a logger reference.
			// 1.1 TestEvent.One ... `EventID`=`{"en": [ 1, 1 ], "et": [ "TestEvent", "One" ], "a": [ { "n": "P1", "v": "1" }, { "n": "P2", "v": "2" }, { "v": "3" } ] }`
			// 1.2 TestEvent.Two
			// 1.2 TestEvent.Two... `EventID`=`{"en": [ 1, 2 ], "et": [ "TestEvent", "Two" ], "a": [ { "v": "args" } ] }`

			logfile.Warning.Msg(new string('=', 1000)).Log();

			Console.WriteLine("Flushing the write cache to the disk...");
			// TODO It is still necessary to allow the queue get cleared by the
			// routers' asynchronous mechanisms as there is currently no option to
			// to wait for that, too. Flushing will only flush what's currently
			// already written to output streams.
			await Task.Delay(TimeSpan.FromSeconds(1));

			await logfile.FlushAsync(default);

			Console.Write("All log events should be written to disk by now. Please hit RETURN to quit...");
			while (Console.ReadKey(true).Key != ConsoleKey.Enter) ;
		}
	}
}
