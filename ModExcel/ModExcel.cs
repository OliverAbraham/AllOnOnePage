using Abraham.Office;
using PluginBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;

namespace AllOnOnePage.Plugins
{
    // read this:
    // https://github.com/ExcelDataReader/ExcelDataReader
    //
    // Important note on .NET Core
    // By default, ExcelDataReader throws a NotSupportedException "No data is available for encoding 1252." 
    // on .NET Core.
    // 
    // To fix, add a dependency to the package System.Text.Encoding.CodePages and then add code to register 
    // the code page provider during application initialization (f.ex in Startup.cs):
    // System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    // 
    // This is required to parse strings in binary BIFF2-5 Excel documents encoded with DOS-era code pages. 
    // These encodings are registered by default in the full .NET Framework, but not on .NET Core.

    public class ModExcel : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string Filename { get; set; }
			public string CellName { get; set; }
			public string Format   { get; set; }
		}
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			InitConfiguration();
			InitExcelFileReader();
		}

		private void InitExcelFileReader()
		{
			LoadAssembly("ExcelDataReader.dll");
			ExcelReader.RegisterCodepageProvider();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public override ModuleSpecificConfig GetModuleSpecificConfig()
		{
			return _myConfiguration;
		}

		public override void CreateSeedData()
		{
			_myConfiguration = new MyConfiguration();
            _myConfiguration.Filename = @"DemoExcelFile.xlsx";
            _myConfiguration.CellName = @"A1";
            _myConfiguration.Format   = @"{0} €";
		}

		public override void Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void UpdateContent(ServerDataObjectChange? dataObject)
        {
			if (!File.Exists(_myConfiguration.Filename))
			{
				Value = $"This excel file doesn't exist! ({Path.GetFullPath(_myConfiguration.Filename)}";
				NotifyPropertyChanged(nameof(Value));
				return;
			}

			try
			{
				Value = ExcelReader.ReadCellValueFromExcelFile(_myConfiguration.Filename, _myConfiguration.CellName);

				if (!string.IsNullOrWhiteSpace(_myConfiguration.Format) &&
					_myConfiguration.Format.Contains("{0}"))
				{
					Value = _myConfiguration.Format.Replace("{0}", Value);
				}
			}
			catch (Exception) 
			{
				Value = "???";
			}

			NotifyPropertyChanged(nameof(Value));
        }

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"This module displays the contents of a cell from an Excel file.
Must be stated:
- the full file name with path.
- the cell (e.g. A1, B13 etc).
A different format can be specified in the 'Format' field.
An example: You have an Excel file in which you record your body weight.
A cell contains the current weight as a number.
To display the weight with the addition of 'kg', enter the following in Format:
{0} kg
");
            return texts;
        }

		public override (bool,string) Validate()
		{
			if (!File.Exists(_myConfiguration.Filename))
			{
				return (false, $"This excel file doesn't exist! ({Path.GetFullPath(_myConfiguration.Filename)})");
			}
            return (true, "");
        }

		public override (bool,string) Test()
		{
			try
			{
				Value = ExcelReader.ReadCellValueFromExcelFile(_myConfiguration.Filename, _myConfiguration.CellName);
			}
			catch (Exception) 
			{
				return (false, $"This excel file doesn't exist!");
			}

			try
			{
				if (!string.IsNullOrWhiteSpace(_myConfiguration.Format) &&
					_myConfiguration.Format.Contains("{0}"))
				{
					Value = _myConfiguration.Format.Replace("{0}", Value);
				}
			}
			catch (Exception) 
			{
				return (false, $"The given format doesn't work!");
			}

            return (true, $"Cell contents: {Value}");
		}
        #endregion



		#region ------------- Implementation ------------------------------------------------------
		private void InitConfiguration()
		{
			try
			{
				_myConfiguration = System.Text.Json.JsonSerializer.Deserialize<MyConfiguration>(_config.ModulePrivateData);
			}
            catch (Exception)
			{
			}

			if (_myConfiguration == null)
				CreateSeedData();
		}
        #endregion
    }
}
