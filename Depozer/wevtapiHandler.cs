using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit;
using System.Management;

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




		public static string GenerateSuppressSeverity(string Path, string Severity) {

			string level = "";

			switch (Severity) {
				case "Error":
					level = "2";
					break;

				case "Critical":
					level = "1";
					break;

				case "Warning":
					level = "3";
					break;

				case "Information":
					level = "0 or Level=4";
					break;

				case "Failure Audit":
					return "  <Suppress Path=\"" + Path + "\">*[EventData[Data[@Name=\"LogonType\"]=\"7\"] or EventData[Data[@Name=\"LogonType\"]=\"2\"]]</Suppress>\n";

				case "Success Audit":
					return "  <Suppress Path=\"" + Path + "\">*[EventData[Data[@Name=\"LogonType\"]=\"2\"]]</Suppress>\n";


			}

			string suppress = "  <Suppress Path=\"" + Path + "\">*[System[(Level=" + level + ")]]</Suppress>\n";
			return suppress;
		}


		public static string GenerateSuppressTimeRange(string Path, DatePicker startDayPicker, TimePicker startTimePicker, DatePicker endDayPicker, TimePicker endTimePicker) {

			string endDateTime = endDayPicker.SelectedDate.Value.ToString("yyyy-MM-dd") + "T" + endTimePicker.Value.Value.ToString("HH:mm:ss") + ".000z";
			string startDateTime = startDayPicker.SelectedDate.Value.ToString("yyyy-MM-dd") + "T" + startTimePicker.Value.Value.ToString("HH:mm:ss") + ".000z";

			string endSuppress = "  <Suppress Path=\"" + Path + "\">*[System[TimeCreated[@SystemTime&gt;='" + endDateTime + "']]]</Suppress>";
			string startSuppress = "  <Suppress Path=\"" + Path + "\">*[System[TimeCreated[@SystemTime&lt;='" + startDateTime + "']]]</Suppress>";

			return startSuppress + "\n" + endSuppress + "\n";

		}


		public static string GenerateSearchUser(string Path, string user, string SID) {
			string query = "  <Select Path=\"" + Path + "\">*[System[Security[@UserID='" + SID + "']]]</Select>\n";
			query += "  <Select Path=\"" + Path + "\">*[EventData[Data=\"" + user + "\"]]</Select>\n";
			return query;
		}


		public static List<string> GenerateQueryList(List<string> channels, List<string> users, List<string> severities, DatePicker startDayPicker, TimePicker startTimePicker, DatePicker endDayPicker, TimePicker endTimePicker) {

			Backbone.LogEvent("INFO", " ---- Generating Query List ----");

			List<string> queryList = new List<string>();
			int queryID = 0;

			foreach (string channel in channels) {
				queryList.Add(GenerateQuery(channel, users, severities, startDayPicker, startTimePicker, endDayPicker, endTimePicker, queryID));
				queryID++;
			}


			return null;
		}


		private static string GenerateQuery(string channel, List<string> users, List<string> severities, DatePicker startDayPicker, TimePicker startTimePicker, DatePicker endDayPicker, TimePicker endTimePicker, int queryID) {

			string query = "<Query Id=\"" + queryID.ToString() + "\" Path=\"" + channel + "\">\n";

			foreach (string user in users) {
				// Add both types of user select lines
				string SID = "";
				// First we need to identify the user's SID
				ManagementObjectSearcher mos = new ManagementObjectSearcher("select * from Win32_Account where Name='" + user + "'");
				foreach (ManagementObject mo in mos.Get()) {
					SID = mo["SID"].ToString();
					break;
				}

				query += GenerateSearchUser(channel, user, SID);

			}

			foreach (string severity in severities) {
				query += GenerateSuppressSeverity(channel, severity);
			}

			query += GenerateSuppressTimeRange(channel, startDayPicker, startTimePicker, endDayPicker, endTimePicker);
			query += "</Query>\n";
			Backbone.LogEvent("INFO", "Generated Query " + queryID.ToString() + ":\n" + query + "\n");

			return query;
		}


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
