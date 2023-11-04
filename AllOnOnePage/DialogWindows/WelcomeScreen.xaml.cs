using System.Windows;

namespace AllOnOnePage.DialogWindows
{
	public partial class WelcomeScreen : Window
	{
		#region ------------- Types and constants -------------------------------------------------
		#endregion



		#region ------------- Properties ----------------------------------------------------------
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public WelcomeScreen(HelpTexts texts)
		{
			InitializeComponent();
			textBlock1.Text = texts[HelpTexts.ID.INTRODUCTION1];
			textBlock2.Text = texts[HelpTexts.ID.INTRODUCTION2];
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
        public void CenterWindow(Window parent)
        {
            Left = parent.Left + (parent.Width  - parent.Width) / 2;
            Top  = parent.Top  + (parent.Height - parent.Height) / 2;
        }
		#endregion



		#region ------------- Implementation ------------------------------------------------------
		private void ButtonSave_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
		#endregion
	}
}
