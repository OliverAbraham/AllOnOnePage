using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace AllOnOnePage.Plugins
{
    public class TextEncoder
	{
		public static string EscapeJsonCharacters(string input)
		{
			var output = new StringBuilder();

			for (int i=0; i<input.Length; i++)
			{
				if (input[i] == '[')
				{
					output.Append("|||1");
				}
				else if (input[i] == ']')
				{
					output.Append("|||2");
				}
				else if (input[i] == '{')
				{
					output.Append("|||3");
				}
				else if (input[i] == '}')
				{
					output.Append("|||4");
				}
				else
				{
					output.Append(input[i]);
				}
			}

			return output.ToString();
		}

		public static string UnescapeJsonCharacters(string input)
		{
			var output = new StringBuilder();

			for (int i=0; i<input.Length-3; i++)
			{
				if (     input[i+0] == '|' &&
					     input[i+1] == '|' &&
					     input[i+2] == '|' &&
					     input[i+3] == '1')
				{
					output.Append("[");
					i += 3;
				}
				else if (input[i+0] == '|' &&
						 input[i+1] == '|' &&
						 input[i+2] == '|' &&
						 input[i+3] == '2')
				{
					output.Append("]");
					i += 3;
				}
				else if (input[i+0] == '|' &&
					     input[i+1] == '|' &&
					     input[i+2] == '|' &&
					     input[i+3] == '3')
				{
					output.Append("{");
					i += 3;
				}
				else if (input[i+0] == '|' &&
						 input[i+1] == '|' &&
						 input[i+2] == '|' &&
						 input[i+3] == '4')
				{
					output.Append("}");
					i += 3;
				}
				else
				{
					output.Append(input[i]);
				}
			}

			return output.ToString();
		}

		public static string DecodeUnicodeCharacters(string input)
		{
			input = input.Replace("\\/", "/");
			input = HttpUtility.UrlDecode(input);
			input = Regex.Replace(input, @"\\u(?<code>\d{4})", CharMatch);
			return input;
		}

		private static string CharMatch(Match match)
		{
			var code = match.Groups["code"].Value;
			int value = Convert.ToInt32(code, 16);
			return ((char) value).ToString();
		}

		public string DecodeHtmlCharacters(string input)
		{
			input = HttpUtility.HtmlDecode(input);
			return input;
		}
	}
}
