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
		public void ShowDialogEx(Window parent, string title, string message, bool centered = true, bool showCheckbox = false)
		{
			Title = title;
			ContentBox.Text = message;

            if (parent != null && centered)
            {
                Left = parent.Left + (parent.Width  - Width)  / 2;
                Top  = parent.Top  + (parent.Height - Height) / 2;
            }

			checkboxDontShowAgain.Visibility = (showCheckbox) ? Visibility.Visible : Visibility.Hidden;

			int lineCount = (message is not null) ? message.Split('\n').Length : 1;

			if (lineCount > 9)
			{
				var additionalHeight = (lineCount - 9) * 32;
                Height += additionalHeight;
            }
			ShowDialog();
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
