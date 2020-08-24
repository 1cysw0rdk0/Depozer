using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;
using System.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using Xceed.Wpf.Toolkit;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

namespace Depozer {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private List<SelectableChannelItem> Channels;
		private List<SelectableUserItem> Users;
		private List<bool> preAll; // Save state for all checkbox, and reset on uncheck
		private static Mutex mutex = new Mutex();
		delegate void SetValueCallback(int value);
		delegate void SetValueCallback2(bool value);

		public MainWindow() {
			InitializeComponent();

			// Call to save the powershell logs to file
			//WevtapiHandler.ExportChannel(IntPtr.Zero, EventLogChannel.PowerShell, "C:\\Users\\camdo\\Desktop\\test.evtx");
			GetSystemConfiguration();

		}

		private void GetSystemConfiguration() {
			Backbone.LogEvent("INFO", "---- Gathering System Information ----");

			// Gather all event log channels
			EventLog[] channels_raw = WevtapiHandler.EnumerateChannels();
			Channels = new List<SelectableChannelItem>();

			Backbone.LogEvent("INFO", "---- Attempting to Enumerate Log Channels ----");

			// Configure SelectableChannelItem and add to list
			foreach (EventLog channel in channels_raw ) {
				SelectableChannelItem NewChannel = new SelectableChannelItem {
					Selected = false,
					Channel = channel,
					ChannelName = channel.LogDisplayName
				};
				Backbone.LogEvent("INFO", "Found Log Channel: " +  channel.LogDisplayName);
				Channels.Add(NewChannel);
			}

			if (Channels.Count == 0) { Backbone.LogEvent("WARNING", "No Open Log Channels Found"); }

			// Set the display datagrid
			Backbone.LogEvent("INFO", "Setting Channel Display");
			ChannelsDisplay.ItemsSource = Channels;



			Backbone.LogEvent("INFO", "---- Attempting to Enumerate User Accounts ----");

			// Gather all usernames
			SelectQuery query = new SelectQuery("Win32_UserAccount");
			ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

			// Init Users List
			Users = new List<SelectableUserItem>();

			// Configure and add users to list
			foreach (ManagementObject user in searcher.Get()) {
				SelectableUserItem userItem = new SelectableUserItem {
					Selected = false,
					Username = user["Name"].ToString()
				};

				Backbone.LogEvent("INFO", "User Account Found: " + user["Name"]);
				Users.Add(userItem);
			}


			if (Users.Count == 0) { Backbone.LogEvent("ERROR", "Failed to Enumerate User Accounts"); }

			Backbone.LogEvent("INFO", "Setting User Display");
			UserDisplay.ItemsSource = Users;
			Backbone.LogEvent("INFO", "---- Finished Gathering System Information ----");
		}

		private class SelectableChannelItem {
			bool selected;
			EventLog channel;
			string channelName;

			public bool Selected {
				get { return (selected); }
				set { selected = value;  }
			}

			public string ChannelName {
				get { return channelName; }
				set { channelName = value;  }
			}

			public EventLog Channel {
				get { return channel; }
				set { channel = value; }
			}
		}

		private class SelectableUserItem {
			bool selected;
			string username;

			public bool Selected {
				get { return selected; }
				set { selected = value; }
			}

			public string Username {
				get { return username; }
				set { username = value; }
			}
		}

		private void ChannelsAll_Click(object sender, RoutedEventArgs e) {
			foreach (SelectableChannelItem channel in Channels) {
				channel.Selected = true;
				ChannelsDisplay.Items.Refresh();
			}
			Backbone.LogEvent("INFO", "Selecting all open log channels");
		}

		private void ChannelsNone_Click(object sender, RoutedEventArgs e) {
			foreach (SelectableChannelItem channel in Channels) {
				channel.Selected = false;
				ChannelsDisplay.Items.Refresh();
			}
			Backbone.LogEvent("INFO", "Deselecting all open log channels");
		}

		private void UsersAll_Click(object sender, RoutedEventArgs e) {
			foreach (SelectableUserItem user in Users) {
				user.Selected = true;
				UserDisplay.Items.Refresh();
			}
			Backbone.LogEvent("INFO", "Selecting all user accounts");
		}

		private void UsersNone_Click(object sender, RoutedEventArgs e) {
			foreach (SelectableUserItem user in Users) {
				user.Selected = false;
				UserDisplay.Items.Refresh();
			}
			Backbone.LogEvent("INFO", "Deselecting all user accounts");
		}

		private void AllCB_Checked(object sender, RoutedEventArgs e) {

			CheckBox[] checkBoxes = {ErrorCB, InformationCB, FailureAuditCB, SuccessAuditCB, WarningCB};
			preAll = new List<bool>();

			Backbone.LogEvent("INFO", "Storing current selections, selecting all severities");

			foreach (CheckBox checkbox in checkBoxes) {
				preAll.Add(checkbox.IsChecked.Value);
				checkbox.IsChecked = true;
				checkbox.IsEnabled = false;
			}

		}

		private void AllCB_Unchecked(object sender, RoutedEventArgs e) {

			Backbone.LogEvent("INFO", "Restoring previous configuration");

			CheckBox[] checkBoxes = { ErrorCB, InformationCB, FailureAuditCB, SuccessAuditCB, WarningCB };

			for (int i = 0; i < checkBoxes.Length; i++) {
				checkBoxes[i].IsEnabled = true;
				checkBoxes[i].IsChecked = preAll[i];
			}

		}

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {


			switch (IntensitySlider.Value) {

				case 0:
					IntensityDescriptor.Content = "Low - Windows Event API Call";
					break;

				case 1:
					IntensityDescriptor.Content = "Med - Locate .evtx Files (WIP)";
					break;

				case 2:
					IntensityDescriptor.Content = "High - Look for deleted .evtx Files (WIP)";
					break;

			}
		}

		private void BrowseDir_Click(object sender, RoutedEventArgs e) {

			Backbone.LogEvent("INFO", "---- Opening OpenFileDialog ----");

			var dialogBox = new CommonOpenFileDialog {
				Title = "Select Output Directory for Dump",
				IsFolderPicker = true,
				AddToMostRecentlyUsedList = false,
				AllowNonFileSystemItems = false,
				EnsurePathExists = true,
				EnsureFileExists = true,
				Multiselect = false,
				InitialDirectory = System.IO.Directory.GetCurrentDirectory()
			};

			if (dialogBox.ShowDialog() == CommonFileDialogResult.Ok) {
				Backbone.LogEvent("INFO", "Directory accepted, setting display");
				OutputDirectory.Text = dialogBox.FileName;
			} else {
				Backbone.LogEvent("WARNING", "Dialog failed or cancelled, directory not set");
			}
		}

		private void PumpAndDump_Click(object sender, RoutedEventArgs e) {

			PumpAndDump.IsEnabled = false;


			//
			// Collect Information on Selected Channels
			//
			Backbone.LogEvent("INFO", "---- Attempting to Collect Log Channels ----");
			List<string> channels = new List<string>();

			foreach (SelectableChannelItem channel in Channels) {
				if (channel.Selected) {
					channels.Add(channel.ChannelName);
					Backbone.LogEvent("INFO", "Added Channel: " + channel.ChannelName);
				}
			}

			// Prevent User Stupidity
			if (channels.Count == 0) {
				Backbone.LogEvent("WARNING", "No Channels Selected, Selecting All Channels.");
				foreach (SelectableChannelItem channel in Channels) {
					channels.Add(channel.ChannelName);
					Backbone.LogEvent("INFO", "Added Channel: " + channel.ChannelName);
				}
			}


			//
			// Collect Information on Selected Users
			//
			Backbone.LogEvent("INFO", "---- Attempting to Collect Users ----");

			// Compile a list of all selected users
			List<string> users = new List<string>();

			foreach (SelectableUserItem user in Users) {
				if (user.Selected) {
					users.Add(user.Username);
					Backbone.LogEvent("INFO", "Added User: " + user.Username);
				}
			}

			// Prevent User Stupidity
			if (users.Count == 0) {
				Backbone.LogEvent("WARNING", "No Users Selected, Selecting All Users.");
				foreach (SelectableUserItem user in Users) {
					users.Add(user.Username);
					Backbone.LogEvent("INFO", "Added User: " + user.Username);
				}
			}

			//
			// Collect Information on Selected Severities
			//
			Backbone.LogEvent("INFO", "---- Attempting to Collect Severities ----");

			// Compile a list of all selected severities
			List<string> severities = new List<string>();
			CheckBox[] boxes = { ErrorCB, WarningCB, InformationCB, FailureAuditCB, SuccessAuditCB };

			foreach (CheckBox item in  boxes) {
				if (!item.IsChecked.Value) {
					severities.Add(item.Content.ToString());
					Backbone.LogEvent("INFO", "Blocked Severity: " + item.Content.ToString());
				} else {
					Backbone.LogEvent("INFO", "Added Severity: " + item.Content.ToString());
				}
			}

			// Prevent User Stupidity
			if (severities.Count == 5) {
				Backbone.LogEvent("WARNING", "All Severities Blocked, Unblocking All Severities.");
					severities.Clear();
					Backbone.LogEvent("INFO", "Unblocked all severity levels");
				
			}



			//
			// Validate Parent Directory
			//
			string path = OutputDirectory.Text + "\\" + DateTime.Now.ToString("dd-MM-yyy") + "\\" +  DateTime.Now.ToString("HH.mm.ss");

			if(!Directory.Exists(path)) {
				mutex.WaitOne();
				Backbone.LogEvent("WARNING", "Parent directory " + path + " does not exist, attempting to create.");

				try {
					Directory.CreateDirectory(path);
				} catch (UnauthorizedAccessException) {
					Backbone.LogEvent("ERROR", "You do not have the required permission to create this directory.");
					mutex.ReleaseMutex();
					return;
				} catch (PathTooLongException) {
					Backbone.LogEvent("ERROR", "The path entered exceeds the system-defined limit.");
					mutex.ReleaseMutex();
					return;
				} catch (DirectoryNotFoundException) {
					Backbone.LogEvent("ERROR", "Invalid directory path. Perhaps the drive is unmapped?");
					mutex.ReleaseMutex();
					return;
				} catch (Exception) {
					Backbone.LogEvent("ERROR", "Unspecified IO Error when creating " + path);
					mutex.ReleaseMutex();
					return;
				}

				Backbone.LogEvent("INFO", "Parent directory " + path + " created successfully.");
				mutex.ReleaseMutex();
			}


			List<string> queryList = WevtapiHandler.GenerateQueryList(channels, users, severities, StartDayPicker, StartTimePicker, EndDayPicker, EndTimePicker);
			progress.Value = 0;
			progress.Maximum = queryList.Count;
			progress.Minimum = 0;

			queryList.Add(path);


			BackgroundWorker worker = new BackgroundWorker();
			worker.WorkerReportsProgress = true;

			worker.DoWork += new DoWorkEventHandler(SubmitQueries);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(QueriesComplete);

			worker.RunWorkerAsync(argument: queryList);

			mutex.WaitOne();
			Backbone.LogEvent("ERROR", "END OF IMPLEMENTATION");
			mutex.ReleaseMutex();

		}


		private void QueriesComplete(object sender, RunWorkerCompletedEventArgs e) {
			SetValueCallback2 enable = new SetValueCallback2(SetProcessBarValue);
			progress.Dispatcher.Invoke(enable, true);
		}


		private void SubmitQueries(object sender, DoWorkEventArgs e) {


			// Parse Arguement Object
			List<string> queryList = (List<string>) e.Argument;
			string outPath = queryList.Last();
			queryList.RemoveAt(queryList.Count - 1);

			// Set Callbacks to update UI
			SetValueCallback2 d = new SetValueCallback2(SetProcessBarValue);
			SetValueCallback f = new SetValueCallback(SetProgressBarvalue);

			// Disable Export Button
			progress.Dispatcher.BeginInvoke(d,false);

			List<BackgroundWorker> workers = new List<BackgroundWorker>();

			// For every query, 
			//	configure new argument object
			//	create a new thread
			//	start the thread
			foreach (string query in queryList) {

				string combo_query = outPath + "\n" + query;

				BackgroundWorker worker = new BackgroundWorker();
				worker.WorkerReportsProgress = true;

				worker.DoWork += new DoWorkEventHandler(SubmitQuery);
				worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(QuerySumbitted);

				workers.Add(worker);

				worker.RunWorkerAsync(argument: combo_query);
			}


			// Wait for all threads to finish before finishing the worker thread
			bool isComplete = false;
			do {
				foreach (BackgroundWorker worker in workers) {
					if (worker.IsBusy) {
						isComplete = false;
						break;
					} else {
						isComplete = true;
					}
				}
			} while (!isComplete);
		}


		private void SubmitQuery(object sender, DoWorkEventArgs e) {

			// Parse input argument object
			string query = (string)e.Argument;
			string path = query.Split('\n')[0];
			query = string.Join("\n", query.Split('\n').Skip(1));
			
			// Simulate Thread Actions
			//Random random = new Random();
			//Thread.Sleep(random.Next(1000,10000));

			// Grab the channel path from the query
			string channel = query.Split('"')[3];
			string id = query.Split('"')[1];

			mutex.WaitOne();
			Backbone.LogEvent("INFO", "Attempting to write to " + path + "\\" + channel + "\\Query_" + id);
			

			// Ensure directory path exists
			if (!Directory.Exists(path + "\\" + channel)) {
				Backbone.LogEvent("WARNING", "Log directory " + channel + " does not exist, attempting to create.");

				try {
					Directory.CreateDirectory(path + "\\" + channel);
				} catch (UnauthorizedAccessException) {
					Backbone.LogEvent("ERROR", "You do not have the required permission to create this directory.");
					return;
				} catch (PathTooLongException) {
					Backbone.LogEvent("ERROR", "The path entered exceeds the system-defined limit.");
					return;
				} catch (DirectoryNotFoundException) {
					Backbone.LogEvent("ERROR", "Invalid directory path. Perhaps the drive is unmapped?");
					return;
				} catch (Exception) {
					Backbone.LogEvent("ERROR", "Unspecified IO Error when creating " + channel);
					return;
				}

				Backbone.LogEvent("INFO", "Log directory " + channel + " created successfully.");
			}

			mutex.ReleaseMutex();

			WevtapiHandler.ExportChannel(IntPtr.Zero, "Application", path + "\\" + channel + "\\Query_" + id + ".evtx");




		}

		private void QuerySumbitted(object sender, RunWorkerCompletedEventArgs e) {
			SetValueCallback update = new SetValueCallback(SetProgressBarvalue);
			progress.Dispatcher.Invoke(update, 1);
		}

		private void SetProcessBarValue(bool value) => PumpAndDump.IsEnabled = value;
		private void SetProgressBarvalue(int value) => progress.Value = progress.Value + value;


	}
}
