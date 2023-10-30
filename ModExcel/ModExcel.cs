using Abraham.Office;
using PluginBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
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
            _myConfiguration.Filename = @"C:\MeineExceldatei.xlsx";
            _myConfiguration.CellName = @"A1";
            _myConfiguration.Format   = @"{0} €";
		}

		public override void Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void UpdateContent(ServerDataObjectChange? dataObject)
        {
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
@"Dieses Modul zeigt aus einer Excel-Datei den Inhalt einer Zelle an.
Anzugeben sind:
- der volle Dateiname mit Pfad.
- die Zelle (z.B. A1, B13 usw).
Im Feld 'Format' kann noch ein abweichendes Format angegeben werden.
EIn Beispiel: Sie haben eine Excel-Datei, in der Sie ihr Körpergewicht aufzeichnen.
Eine Zelle enthält das aktuelle Gewicht als Zahl.
Um hier das Gewicht mit dem Zusatz 'kg' anzuzeigen, geben sie bei Format ein:
{0} kg
");
            return texts;
        }

		public override (bool,string) Validate()
		{
			if (!File.Exists(_myConfiguration.Filename))
			{
				return (false, $"Die angegebene Datei existiert nicht!");
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
				return (false, $"Die angegebene Datei konnte nicht gelesen werden!");
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
				return (false, $"Das angegebene Format funktioniert nicht!");
			}

            return (true, $"Inhalt der Zelle: {Value}");
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
