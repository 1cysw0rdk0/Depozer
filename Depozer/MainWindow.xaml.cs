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
using Microsoft.WindowsAPICodePack.Dialogs;


namespace Depozer {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private List<SelectableChannelItem> Channels;
		private List<SelectableUserItem> Users;
		private List<bool> preAll; // Save state for all checkbox, and reset on uncheck

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

			Backbone.LogEvent("INFO", "---- Attempting to Collect Log Channels ----");

			// Compile a list of all selected channels
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
					channels.Add(user.Username);
					Backbone.LogEvent("INFO", "Added User: " + user.Username);
				}
			}



			Backbone.LogEvent("INFO", "---- Attempting to Collect Severities ----");

			// Compile a list of all selected severities
			List<string> severities = new List<string>();
			CheckBox[] boxes = { ErrorCB, WarningCB, InformationCB, FailureAuditCB, SuccessAuditCB };

			foreach (CheckBox item in  boxes) {
				if (item.IsChecked.Value) {
					severities.Add(item.Content.ToString());
					Backbone.LogEvent("INFO", "Added Severity: " + item.Content.ToString());
				}
			}

			// Prevent User Stupidity
			if (severities.Count == 0) {
				Backbone.LogEvent("WARNING", "No Severities Selected, Selecting All Severities.");
				foreach (CheckBox item in boxes) {
					severities.Add(item.Content.ToString());
					Backbone.LogEvent("INFO", "Added User: " + item.Content.ToString());
				}
			}

			Backbone.LogEvent("ERROR", "NOT YET IMPLEMENTED");


		}
	}
}
