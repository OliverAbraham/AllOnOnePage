using Abraham.ProgramSettingsManager;
using AllOnOnePage.Plugins;
using System;
using System.IO;
using System.Reflection;

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
		private ApplicationData _applicationDirectories;
		private int _moduleNumber;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public ConfigurationManager(HelpTexts texts, ApplicationData applicationDirectories)
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

			try
			{
				_configurationManager.Load();
				_config = _configurationManager.Data;
			}
			catch (Exception)
			{
				_config = null;
			}

			if (_config is null || _config.Modules.Count == 0)
			{
				_config = CreateSeedData();
			}

			_config.AssignUniqueIDsToModules();
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
		private Configuration CreateSeedData()
		{
			var userDocumentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			_applicationDirectories.DataDirectory = userDocumentsDirectory + Path.DirectorySeparatorChar + "All on one page";

			// copy the demo excel file to the users' documents directory
			var sourceFile = Path.Combine(_applicationDirectories.ProgramDirectory, "DemoExcelFile.xlsx");
			var targetFile = Path.Combine(_applicationDirectories.DataDirectory, "DemoExcelFile.xlsx");
			try
			{
				File.Copy(sourceFile, targetFile, true); 
			} 
			catch { } // never ever break the first program start experience when this fails!

			var seed = new Configuration();
            seed.FullScreenDisplay = false;
            seed.Background = Configuration.BackgroundType.Image;
            seed.BackgroundImage = "default.jpg";

			var modText = Create("ModText", 200, 200, 800, 200,  30, "#FFFFFFFF");
			var modTextData = new ModText_MyConfiguration();
			modTextData.Text = _texts[HelpTexts.ID.DEMOTILE];
			modText.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(modTextData);
			seed.Modules.Add(modText);

			seed.Modules.Add(Create("ModDate"   , 200, 430, 600, 120,  80, "#FF4169E1"));
			seed.Modules.Add(Create("ModTime"   , 820, 430, 200, 120,  80, "#FFC71585"));
			seed.Modules.Add(Create("ModWeather", 200, 560, 205, 150, 100, "#FFFFFFFF"));

			var modExcel = Create("ModExcel"  , 500, 600, 400, 100, 100, "#FFFFFFFF");
			modExcel.FontSize = 40;
			seed.Modules.Add(modExcel);
			return seed;
		}
		#endregion



		#region ------------- Implementation ------------------------------------------------------
		private ModuleConfig Create(string type, int x, int y, int w, int h, int fontsize, string textcolor)
		{
			return new ModuleConfig()
            { 
                ModuleName      = (_moduleNumber++).ToString(), 
                TileType        = type, 
                X               = x, 
                Y               = y, 
                W               = w, 
                H               = h, 
                FontSize        = fontsize, 
				ApplicationData = _applicationDirectories,
				TextColor       = textcolor
            };
		}
		#endregion
	}

	internal class ModText_MyConfiguration
	{
        public string   ModuleName           { get; set; } = "0"; 
        public string   TileType             { get; set; } = "TEXTBLOCK";
        public int      X                    { get; set; } = 100;
        public int      Y                    { get; set; } = 100;
        public int      W                    { get; set; } = 400;
        public int      H                    { get; set; } = 200;
        public int      FontSize             { get; set; } = 12;
        public string   ModulePrivateData    { get; set; } = "";
		public bool     IsFrameVisible       { get; set; } = false;
		public string   FrameColor           { get; set; } = "#FF333333"; // dark gray
		public int      FrameThickness       { get; set; } = 0;
		public int      FrameRadius          { get; set; } = 10;
		public string   BackgroundColor      { get; set; } = "#00FFFFFF"; // Transparent
		public string   TextColor            { get; set; } = "#FFFFFFFF"; // White
        public int      TextPadding          { get; set; } = 20;
		public string   Text                 { get; set; }
	}
}
