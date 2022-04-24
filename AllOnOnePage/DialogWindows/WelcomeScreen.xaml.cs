using System.Windows;

namespace AllOnOnePage.DialogWindows
{
	public partial class WelcomeScreen : Window
	{
		public WelcomeScreen(HelpTexts texts)
		{
			InitializeComponent();
			textBlock1.Text = texts[HelpTexts.ID.INTRODUCTION1];
			textBlock2.Text = texts[HelpTexts.ID.INTRODUCTION2];
		}

		private void ButtonSave_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
