using System;
using System.IO;

namespace AllOnOnePage
{
	class Logger
	{
		private Configuration _configuration;

		public Logger()
		{
			_configuration = null;
		}

		public Logger(Configuration configuration)
		{
			_configuration = configuration;
		}

		public void Log(string message)
		{
			if (_configuration == null)
				return;

			if (_configuration.LogToConsole) 
			{
				Console.WriteLine(message);
			}
			
			if (_configuration.LogToFile) 
			{
				try
				{
					string Line = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}    {message}\r\n";
					File.AppendAllText(_configuration.LogfileName, Line);
				}
				catch (Exception) { }
			}
		}
	}
}
