using Abraham.ProgramSettingsManager;
using AllOnOnePage.Plugins;
using System;
using System.IO;

namespace AllOnOnePage.Libs
{
	class ConfigurationManager
	{
		#region ------------- Types and constants -------------------------------------------------
		private string _configurationFilename = $"appsettings.hjson";
		#endregion



		#region ------------- Properties ----------------------------------------------------------
		public Configuration Config => _config;
		public string DataDirectory { get; private set; }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
        private Configuration _config;
        private ProgramSettingsManager<Configuration> _configurationManager;
		private HelpTexts _texts;
		private ApplicationDirectories _applicationDirectories;
		private int _moduleNumber;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public ConfigurationManager(HelpTexts texts, ApplicationDirectories applicationDirectories)
		{
			_texts = texts;
			_applicationDirectories = applicationDirectories;
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public bool Load()
        {
			_configurationManager = new ProgramSettingsManager<Configuration>()
				.UseFilename(_configurationFilename);

			CreateSeedData();

			_configurationManager.Load();
			_config = _configurationManager.Data;
			if (_config == null)
				throw new Exception($"No valid configuration found!\nExpecting file '{_configurationManager.ConfigFilename}'");

			int enumerator = 1;
			foreach (var module in _config.Modules)
			{
				if (module.ID == 0)
					module.ID = enumerator++;
			}
            return true;
        }

		public void Save()
		{
			_configurationManager.Save(_config);
		}

		public void SetCurrentDirectoryToDataDirectory()
		{
			Directory.SetCurrentDirectory(DataDirectory);
		}

		public void CreateDataDirectoryIfNotExists()
		{
			var userDocumentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			DataDirectory = userDocumentsDirectory + Path.DirectorySeparatorChar + "All on one page";
			if (!Directory.Exists(DataDirectory))
				Directory.CreateDirectory(DataDirectory);
		}
		#endregion



		#region ------------- Seed data for first start -------------------------------------------
		private void CreateSeedData()
		{
			if (File.Exists(_configurationFilename))
				return;

			var seed = new Configuration();
            seed.FullScreenDisplay = false;
            seed.Background = Configuration.BackgroundType.Image;
            seed.BackgroundImage = "Paper.jpg";

			var modText = Create("ModText"   , 200, 200, 800, 200,  30);
			var modTextData = new ModText_MyConfiguration();
			modTextData.Text = _texts[HelpTexts.ID.DEMOTILE];
			modText.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(modTextData);
			seed.Modules.Add(modText);

			seed.Modules.Add(Create("ModDate"   , 200, 430, 600, 120,  80));
			seed.Modules.Add(Create("ModTime"   , 820, 430, 200, 120,  80));
			seed.Modules.Add(Create("ModWeather", 200, 560, 205, 185, 100));

			_configurationManager.Save(seed);
		}
		#endregion



		#region ------------- Implementation ------------------------------------------------------
		private ModuleConfig Create(string type, int x, int y, int w, int h, int fontsize)
		{
			return new ModuleConfig()
            { 
                ModuleName             = (_moduleNumber++).ToString(), 
                TileType               = type, 
                X                      = x, 
                Y                      = y, 
                W                      = w, 
                H                      = h, 
                FontSize               = fontsize, 
				ApplicationDirectories = _applicationDirectories,
            };
		}
		#endregion
	}

	internal class ModText_MyConfiguration
	{
		public string Text { get; set; }
	}
}
