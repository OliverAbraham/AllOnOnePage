using HtmlAgilityPack;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Abraham.Weather
{
	public class WeatherConverter
    {
        #region ------------- Methods -------------------------------------------------------------

        public List<Forecast> ExtractWeatherDataFromPage(string inhalt)
        {
            var results = new List<Forecast>();

            var weather = ExtractWeatherData(inhalt);

            for (int i=0; i<weather.HoursArray.GetLength(0); i++)
            {
                var forecast = new Forecast();
                forecast.Hour = ConvertWeatherTime(weather.HoursArray[i]);
                forecast.Temp = ConvertTemperature(weather.TempsArray[i]);
                
                (forecast.Icon, 
                 forecast.IconDescription,
                 forecast.WeatherDescription) = ConvertWeatherIcon(weather.IconsArray[i]);

                results.Add(forecast);
            }
            return results;
        }

        public string ConvertIconToUnicode(Forecast.WeatherIcon icon)
        {
            switch (icon)
            {
                case Forecast.WeatherIcon.Cloud:               return char.ConvertFromUtf32(0x2601);
                case Forecast.WeatherIcon.CloudWithLightning:  return char.ConvertFromUtf32(0x26C8);
                case Forecast.WeatherIcon.CloudWithRain:       return char.ConvertFromUtf32(0x2614); // 0x1F327);
                case Forecast.WeatherIcon.CloudWithSnow:       return char.ConvertFromUtf32(0x2603); // 0x1F328);
                case Forecast.WeatherIcon.MediumCloud:         return char.ConvertFromUtf32(0x26C5);
                case Forecast.WeatherIcon.SmallCloud:          return char.ConvertFromUtf32(0x26C5); // 0x1F324);
                case Forecast.WeatherIcon.Sun:                 return char.ConvertFromUtf32(0x2600);
                case Forecast.WeatherIcon.SunCloudRain:        return char.ConvertFromUtf32(0x26C5); // 0x1F326);
                case Forecast.WeatherIcon.ThunderCloudAndRain: return char.ConvertFromUtf32(0x26C8);
                case Forecast.WeatherIcon.Moon:                return char.ConvertFromUtf32(0x263D); // 0x1F319);
                case Forecast.WeatherIcon.Snow:                return char.ConvertFromUtf32(0x2603);
                case Forecast.WeatherIcon.Fog:                 return char.ConvertFromUtf32(0x2601); // 0x1F32B);
                case Forecast.WeatherIcon.Unknown: default:    return char.ConvertFromUtf32(0x26C4);                
            }
        }

        public double FindTemperatureForTime(List<Forecast> entries, DateTime time)
        {
            return FindEntry(entries, time).Temp;
        }

        public Forecast.WeatherIcon FindIconForTime(List<Forecast> entries, DateTime time)
        {
            return FindEntry(entries, time).Icon;
        }

        public Forecast FindEntry(List<Forecast> entries, DateTime date)
        {
            return FindEntry(entries, date.TimeOfDay);
        }

        public Forecast FindEntry(List<Forecast> entries, TimeSpan time)
        {
            var entry = (from e in entries where e.Hour.Hour == time.Hours select e).FirstOrDefault();
            if (entry == null)
                entry = entries[0]; //throw new Exception("time not found in weather data");
            return entry;
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------

        private HtmlParts ExtractWeatherData(string inhalt)
        {
            var Doc = new HtmlDocument();
            Doc.LoadHtml(inhalt);
            //System.IO.File.WriteAllText("current_page.html", inhalt);

            var Body = Doc.DocumentNode.SelectSingleNode("//body");
            if (Body != null)
            {
                // XML Selector to find the div we need
                var DayBoxes1 = Body.SelectNodes($".//div[contains(@class, 'box-slider__container')]"); 
                var DayBoxes2 = Body.SelectNodes(".//div[@class='box-slider__container']");
                var DayBoxes = Body.SelectNodes(".//div[@class='base-box weather-daybox v-spacing-md base-box--level-0']");

                foreach (var box in DayBoxes)
                {
                    var Hours = box.SelectNodes(".//div[@class='weather-daybox__main__hours__hour']");
                    var Icons = box.SelectNodes(".//div[@class='weather-icon weather-daybox__icon']");
                    var Temps = box.SelectNodes(".//div[@class='weather-daybox__main__hours__temp']");

                    var results = new HtmlParts();
                    results.HoursArray = ConvertInnerHtml(Hours);
                    results.IconsArray = ConvertInnerHtml(Icons);
                    results.TempsArray = ConvertInnerHtml(Temps);
                    return results;
                }
            }
            throw new Exception("Weather data not found!");
        }

        private string[] ConvertInnerHtml(HtmlNodeCollection nodes)
        {
            var TextArray = new string[nodes.Count];
            int i = 0;
            foreach (var node in nodes)
                TextArray[i++] = (!string.IsNullOrWhiteSpace(node.InnerText)) ? node.InnerText : node.InnerHtml;
            return TextArray;
        }

        private DateTime ConvertWeatherTime(string html)
        {
            html = html.Trim(new char[] { '\n', '\r', ' ', '\t' });
            var time = DateTime.Parse(html + ":00");
            if (time.Hour >= 22)
                return time.AddHours(-22);
            else
                return time.AddHours(2);
        }

        private double ConvertTemperature(string html)
        {
            if (html.StartsWith("&"))
                return double.NaN;

            var index = html.IndexOf('&');
            if (index > 0)
                html = html.Substring(0, index);

            html = html.Replace("°", "").Replace("°", "");
            if (html.Length > 1 && !char.IsDigit(html[html.Length-1]))
                html = html.Substring(0, html.Length-1);

            if (Double.TryParse(html, out double value))
                return value;
            else
                return 0;
        }

        private (Forecast.WeatherIcon, string, string) ConvertWeatherIcon(string html)
        {
            var icon  = GetTag(html, "src=");
            var text  = GetTag(html, "alt=");
            var title = GetTag(html, "title=");
            
            var cIcon = ConvertImageSourceToIcon(icon);
            return (cIcon, text, title);
        }

        private string GetTag(string html, string tag)
        {
            int start = html.IndexOf(tag);
            if (start >= 0)
            {
                start += tag.Length+1;
                if (start >= html.Length)
                    return "";
                int end = html.IndexOf("\"", start);
                if (end > start)
                    return html.Substring(start, end-start);
            }
            return "";
        }

        private Forecast.WeatherIcon ConvertImageSourceToIcon(string html)
        {
            if (html.Contains("sonne_woelkchen"))
                return Forecast.WeatherIcon.SmallCloud;
            if (html.Contains("sonne_wolke"))
                return Forecast.WeatherIcon.MediumCloud;
            if (html.Contains("sonne_auf_unter"))
                return Forecast.WeatherIcon.Sun;
            if (html.Contains("mond"))
                return Forecast.WeatherIcon.Moon;
            if (html.Contains("schnee"))
                return Forecast.WeatherIcon.Snow;
            if (html.Contains("nebel"))
                return Forecast.WeatherIcon.Fog;
            if (html.Contains("wolke_regen"))
                return Forecast.WeatherIcon.CloudWithRain;
            if (html.Contains("sonne"))
                return Forecast.WeatherIcon.Sun;
            if (html.Contains("wolke.38262afa.svg"))
                return Forecast.WeatherIcon.Cloud;
            if (html.Contains("wolke.graupel"))
                return Forecast.WeatherIcon.Cloud;
            if (html.Contains("wolke"))
                return Forecast.WeatherIcon.MediumCloud;
            
            File.AppendAllText("WeatherConverter-problems.log", $"{DateTime.Now} - Cannot convert: {html}\n");
            return Forecast.WeatherIcon.Unknown;
        }
        #endregion
    }
}
