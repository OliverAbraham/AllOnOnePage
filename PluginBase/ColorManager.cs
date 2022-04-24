using System;
using System.Windows.Media;

namespace AllOnOnePage.Plugins
{
	public class ColorManager
	{
		#region ------------- Methods -------------------------------------------------------------
		public static SolidColorBrush CreateBrush(string argbValues)
		{
			return new SolidColorBrush(CreateColor(argbValues));
		}

		public static Color CreateColor(string argbValues)
		{
			(byte a, byte r, byte g, byte b) = ConvertRgbStringToColors(argbValues);
			return Color.FromArgb(a,r,g,b);
		}

		public static (byte,byte,byte,byte) ConvertRgbStringToColors(string argbValues)
		{
			if (string.IsNullOrWhiteSpace(argbValues) || argbValues.Length != 9)
				argbValues = "#ffffffff";
				//throw new ArgumentException(nameof(argbValues));
			
			var colorValue = Int32.Parse(argbValues.Substring(1,8), System.Globalization.NumberStyles.HexNumber);
			byte a = (byte)((colorValue >> 24) & 0xFF);
			byte r = (byte)((colorValue >> 16) & 0xFF);
			byte g = (byte)((colorValue >>  8) & 0xFF);
			byte b = (byte)((colorValue      ) & 0xFF);
			return (a,r,g,b);
		}

		public static string ConvertColorsToRgbString(byte a, byte r, byte g, byte b)
		{
			return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
		}

		public static string ConvertColorToRgbString(Color color)
		{
			return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
		}
		#endregion
	}
}
