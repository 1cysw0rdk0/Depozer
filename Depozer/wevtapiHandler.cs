using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;




namespace Depozer {
	class WevtapiHandler {


		// Define dictionary mapping channel enum to their string equivalent
		static Dictionary<EventLogChannel,string> eventlogstrings = new Dictionary<EventLogChannel, string> {
			{EventLogChannel.System, "System" },
			{EventLogChannel.Security, "Security" },
			{EventLogChannel.Application, "Application" },
			{EventLogChannel.PowerShell, "Windows PowerShell" },
			{EventLogChannel.Azure, "Windows Azure" },
			{EventLogChannel.InternetExp, "Internet Explorer" }
		};
		
		// Import and configure Windows Event Handler API (wevtapi.dll)
		[DllImport(@"wevtapi.dll",
			CallingConvention = CallingConvention.Winapi,
			CharSet = CharSet.Auto,
			SetLastError = true)]

		// Execute wevtapi.EvtExportLog
		private static extern bool EvtExportLog(
			IntPtr sessionHandle,
			string path,
			string query, // USE QUERY TO FILTER EVENTS SELECTED FOR EXPORT OR RETRIEVAL
			string targetPath,
			[MarshalAs(UnmanagedType.I4)] EventExportLogFlags flags);


		// Pretty looking wrapper for EvtExportLog
		public static bool ExportChannel(IntPtr sessionHandle, EventLogChannel channel, string exportPath, string query="*"){
			string channel_s = eventlogstrings[channel];
			return EvtExportLog(sessionHandle, channel_s, query, exportPath, EventExportLogFlags.ChannelPath);
		}

		public static EventLog[] EnumerateChannels() => EventLog.GetEventLogs();

	}

	[Flags]
	public enum EventExportLogFlags {
		ChannelPath = 1,
		LogFilePath = 2,
		TolerateQueryErrors = 0x1000
	};

	public enum EventLogChannel {
		System,
		Security,
		Application,
		PowerShell,
		Azure,
		InternetExp
	}



	
}
