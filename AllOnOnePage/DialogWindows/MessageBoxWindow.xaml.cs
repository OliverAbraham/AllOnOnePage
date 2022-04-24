using System.Windows;

namespace AllOnOnePage.DialogWindows
{
	public partial class MessageBoxWindow : Window
	{
		#region ------------- Types and constants -------------------------------------------------
		#endregion



		#region ------------- Properties ----------------------------------------------------------
		public MessageBoxResult MessageBoxResult { get; internal set; }
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public MessageBoxWindow()
		{
			InitializeComponent();
			checkboxDontShowAgain.Visibility = Visibility.Hidden;
		}

		public MessageBoxWindow(Window parent)
		{
			InitializeComponent();
			Owner = parent;
			checkboxDontShowAgain.Visibility = Visibility.Hidden;
		}

		public MessageBoxWindow(Window parent, bool showCheckbox)
		{
			InitializeComponent();
			Owner = parent;
			checkboxDontShowAgain.Visibility = (showCheckbox) ? Visibility.Visible : Visibility.Hidden;
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public static void Show(Window parent, string message)
		{
			var wnd = new MessageBoxWindow(parent);
			wnd.ContentBox.Text = message;
			wnd.ShowDialog();
		}

		#endregion



		#region ------------- Implementation ------------------------------------------------------
		private void Button_OK_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult = MessageBoxResult.OK;
			Close();
		}

		private void Button_Yes_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult = MessageBoxResult.Yes;
			Close();
		}

		private void Button_No_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult = MessageBoxResult.No;
			Close();
		}
		#endregion
	}
}
