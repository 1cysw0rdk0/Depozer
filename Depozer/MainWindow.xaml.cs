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

			// Gather all event log channels
			EventLog[] channels_raw = WevtapiHandler.EnumerateChannels();
			Channels = new List<SelectableChannelItem>();

			// Configure SelectableChannelItem and add to list
			foreach (EventLog channel in channels_raw ) {
				SelectableChannelItem NewChannel = new SelectableChannelItem();
				NewChannel.Selected = false;
				NewChannel.Channel = channel;
				NewChannel.ChannelName = channel.LogDisplayName;

				Channels.Add(NewChannel);
			}

			// Set the display datagrid
			ChannelsDisplay.ItemsSource = Channels;

			// Gather all usernames
			SelectQuery query = new SelectQuery("Win32_UserAccount");
			ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

			// Init Users List
			Users = new List<SelectableUserItem>();

			// Configure and add users to list
			foreach (ManagementObject user in searcher.Get()) {
				SelectableUserItem userItem = new SelectableUserItem();
				userItem.Selected = false;
				userItem.Username = user["Name"].ToString();

				Users.Add(userItem);
			}

			UserDisplay.ItemsSource = Users;

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
		}

		private void ChannelsNone_Click(object sender, RoutedEventArgs e) {
			foreach (SelectableChannelItem channel in Channels) {
				channel.Selected = false;
				ChannelsDisplay.Items.Refresh();
			}
		}

		private void UsersAll_Click(object sender, RoutedEventArgs e) {
			foreach (SelectableUserItem user in Users) {
				user.Selected = true;
				UserDisplay.Items.Refresh();
			}
		}

		private void UsersNone_Click(object sender, RoutedEventArgs e) {
			foreach (SelectableUserItem user in Users) {
				user.Selected = false;
				UserDisplay.Items.Refresh();
			}
		}

		private void AllCB_Checked(object sender, RoutedEventArgs e) {

			CheckBox[] checkBoxes = {ErrorCB, InformationCB, FailureAuditCB, SuccessAuditCB, WarningCB};
			preAll = new List<bool>();

			foreach (CheckBox checkbox in checkBoxes) {
				preAll.Add(checkbox.IsChecked.Value);
				checkbox.IsChecked = true;
				checkbox.IsEnabled = false;
			}

		}

		private void AllCB_Unchecked(object sender, RoutedEventArgs e) {

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
			var dialogBox = new CommonOpenFileDialog();
			dialogBox.Title = "Select Output Directory for Dump";
			dialogBox.IsFolderPicker = true;
			dialogBox.AddToMostRecentlyUsedList = false;
			dialogBox.AllowNonFileSystemItems = false;
			dialogBox.EnsurePathExists = true;
			dialogBox.EnsureFileExists = true;
			dialogBox.Multiselect = false;
			dialogBox.InitialDirectory = System.IO.Directory.GetCurrentDirectory();

			if (dialogBox.ShowDialog() == CommonFileDialogResult.Ok) {
				OutputDirectory.Text = dialogBox.FileName;
			}
		}
	}
}
