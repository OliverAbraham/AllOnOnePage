using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Abraham.WPFAnimations
{
	public class WpfAnimations
    {
        #region ------------- Methods -------------------------------------------------------------
        #region ------------- Labels --------------------------------------------------------------
		public static void FadeOutLabel(Control control)
		{ 
			FadeOut(control, Label.OpacityProperty);
		}

		public static void FadeInLabel(Control control)
		{ 
			FadeIn(control, Label.OpacityProperty);
		}

		public static void FadeInImmediatelyLabel(Control control)
		{ 
			control.Opacity = 1.0;
			control.Visibility = System.Windows.Visibility.Visible;
		}

		/// <summary>
		/// this method is necessary to let the control hit test visible = clickable
		/// </summary>
		public static void FadeOutButLeaveVisible(Control control, DependencyProperty propertyToAnimate)
		{
			FadeOut(control, propertyToAnimate, setVisibilityHidden:false);
		}

		public static void FadeOut(Control control, DependencyProperty propertyToAnimate, bool setVisibilityHidden = true)
		{
			control.Opacity = 1.0;
			control.Visibility = System.Windows.Visibility.Visible;

			Create_and_start_animation(control, 1.0, 0.0,
				propertyToAnimate,
				delegate 
				{ 
					if (setVisibilityHidden)
						control.Visibility = System.Windows.Visibility.Hidden; 
				});
		}

		public static void FadeIn(Control control, DependencyProperty propertyToAnimate)
		{
			control.Opacity = 0.0;
			control.Visibility = System.Windows.Visibility.Visible;

			Create_and_start_animation(control, 0.0, 1.0,
				Label.OpacityProperty,
				delegate { control.Visibility = System.Windows.Visibility.Visible; });
		}
        #endregion

        #region ------------- Images --------------------------------------------------------------

		public static void FadeOut(Image control, DependencyProperty propertyToAnimate, bool setVisibilityHidden = true)
		{
			control.Opacity = 1.0;
			control.Visibility = System.Windows.Visibility.Visible;

			Create_and_start_animation(control, 1.0, 0.0,
				propertyToAnimate,
				delegate 
				{ 
					if (setVisibilityHidden)
						control.Visibility = System.Windows.Visibility.Hidden; 
				});
		}

		/// <summary>
		/// this method is necessary to let the control hit test visible = clickable
		/// </summary>
		public static void FadeOutButLeaveVisible(Image control, DependencyProperty propertyToAnimate)
		{
			FadeOut(control, propertyToAnimate, setVisibilityHidden:false);
		}

		public static void FadeIn(Image control, DependencyProperty propertyToAnimate)
		{
			control.Opacity = 0.0;
			control.Visibility = System.Windows.Visibility.Visible;

			Create_and_start_animation(control, 0.0, 1.0,
				Label.OpacityProperty,
				delegate { control.Visibility = System.Windows.Visibility.Visible; });
		}
        #endregion

        #region ------------- TextBlocks ----------------------------------------------------------
		public static void FadeOutTextBlock(TextBlock control)
		{ 
			FadeOut(control, TextBlock.OpacityProperty);
		}

		public static void FadeInTextBlock(TextBlock control)
		{ 
			FadeIn(control, TextBlock.OpacityProperty);
		}

		public static void FadeInImmediatelyTextBlock(TextBlock control)
		{ 
			FadeIn(control, TextBlock.OpacityProperty);
		}

		public static void FadeOut(TextBlock control, DependencyProperty propertyToAnimate)
		{
			control.Opacity = 1.0;
			control.Visibility = System.Windows.Visibility.Visible;

			Create_and_start_animation(control, 1.0, 0.0,
				propertyToAnimate,
				delegate { control.Visibility = System.Windows.Visibility.Hidden; });
		}

		public static void FadeIn(TextBlock control, DependencyProperty propertyToAnimate)
		{
			control.Opacity = 0.0;
			control.Visibility = System.Windows.Visibility.Visible;

			Create_and_start_animation(control, 0.0, 1.0,
				Label.OpacityProperty,
				delegate { control.Visibility = System.Windows.Visibility.Visible; });
		}
        #endregion
        #region ------------- Grids ---------------------------------------------------------------

		public static void FadeOutGrid(Grid control, bool setVisibilityHidden = true)
		{
			control.Opacity = 1.0;
			control.Visibility = System.Windows.Visibility.Visible;

			Create_and_start_animation(control, 1.0, 0.0,
				Grid.OpacityProperty,
				delegate 
				{ 
					if (setVisibilityHidden)
						control.Visibility = System.Windows.Visibility.Hidden; 
				});
		}

		public static void FadeInImmediatelyGrid(Grid control)
		{
			control.Opacity = 0.0;
			control.Visibility = System.Windows.Visibility.Visible;

			Create_and_start_animation(control, 0.0, 1.0,
				Grid.OpacityProperty,
				delegate { control.Visibility = System.Windows.Visibility.Visible; });
		}
        #endregion
        #endregion



        #region ------------- Implementation ------------------------------------------------------
		private static void Create_and_start_animation(DependencyObject control, double from, double to, 
														DependencyProperty propertyToAnimate,
														EventHandler completedAction)
		{
			var a = new DoubleAnimation
			{
				From = from,
				To = to,
				FillBehavior = FillBehavior.HoldEnd,
				BeginTime = TimeSpan.FromSeconds(0),
				Duration = new Duration(TimeSpan.FromSeconds(1.0))
			};
			var storyboard = new Storyboard();

			storyboard.Children.Add(a);
			Storyboard.SetTarget(a, control);
			Storyboard.SetTargetProperty(a, new PropertyPath(propertyToAnimate));
			storyboard.Completed += completedAction;
			storyboard.Begin();
		}
		#endregion
	}
}
