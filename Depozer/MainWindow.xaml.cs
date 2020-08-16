using System;
using System.Collections.Generic;
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
		public MainWindow() {
			InitializeComponent();

			// Call to save the powershell logs to file
			//wevtapiHandler.ExportChannel(IntPtr.Zero, EventLogChannel.PowerShell, "C:\\Users\\camdo\\Desktop\\test.evtx");



		}
	}
}
