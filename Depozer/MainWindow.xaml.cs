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


namespace Depozer {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		List<SelectableChannelItem> Channels;

		public MainWindow() {
			InitializeComponent();

			// Call to save the powershell logs to file
			//WevtapiHandler.ExportChannel(IntPtr.Zero, EventLogChannel.PowerShell, "C:\\Users\\camdo\\Desktop\\test.evtx");

			GetSystemConfiguration();


		}

		private void GetSystemConfiguration() {
			EventLog[] channels_raw = WevtapiHandler.EnumerateChannels();
			Channels = new List<SelectableChannelItem>();

			foreach (EventLog channel in channels_raw ) {
				SelectableChannelItem NewChannel = new SelectableChannelItem();
				NewChannel.Selected = false;
				NewChannel.Channel = channel;
				NewChannel.ChannelName = channel.LogDisplayName;

				Channels.Add(NewChannel);
			}


			ChannelsDisplay.ItemsSource = Channels;


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
	}
}
