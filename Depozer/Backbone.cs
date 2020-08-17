using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;


namespace Depozer {

	/// <summary>
	/// Backbone decides which path the hybrid application takes
	///		If there are args, its a console application
	///		If there are no args, its a GUI application
	/// </summary>
	class Backbone {

		[DllImport("kernel32.dll",
			SetLastError = true,
			ExactSpelling = true)]
		public static extern bool FreeConsole();

		[STAThread]
		public static void Main(string[] args) {

			if (args.Length == 0) {
				// GUI mode
				FreeConsole();

				MainWindow mainWindow = new MainWindow();
				mainWindow.ShowDialog();
				

			} else {
				// Console mode
				// TODO 
				
				
			}
		}
	}
}
