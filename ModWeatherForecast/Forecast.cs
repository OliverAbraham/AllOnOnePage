using System;

namespace Abraham.Weather
{
	public class Forecast
	{
		public enum WeatherIcon
		{
			Unknown,
			Sun,
			SmallCloud,
			MediumCloud,
			SunCloudRain,
			Cloud,
			CloudWithRain,
			CloudWithSnow,
			CloudWithLightning,
			ThunderCloudAndRain,
			Moon,
			Snow,
			Fog
		}

		public DateTime		Hour				{ get; set; }
		public double		Temp				{ get; set; }
		public WeatherIcon	Icon				{ get; set; }
		public string		IconDescription		{ get; set; }
		public string		WeatherDescription	{ get; set; }

		public override string ToString()
		{
			return $"{Hour} {Temp}° {Icon} {IconDescription} {WeatherDescription}";
		}
	}
}
