using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using Abraham.Prtg;
using Abraham.Prtg.Models;
using System.Threading.Tasks;
using Abraham.Scheduler;
using System.Xml.Serialization;
using System.Xml;
using HomenetBase;

namespace AllOnOnePage.Plugins
{
	public class ModPrtg : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string PrtgURL                   { get; set; }
			public bool   UsePasshashAuthentication { get; set; }
			public string Username                  { get; set; }
			public string Passwort                  { get; set; }
			public bool   UseApitokenAuthentication { get; set; }
			public string Apitoken                  { get; set; }
			public bool   BypassSslValidation       { get; set; }
			public int    UpdateIntervalInSeconds   { get; set; }
			public int    TimeoutInSeconds			{ get; set; }
			public int	  SensorID					{ get; set; }
			public string Property					{ get; set; }
			public string Format					{ get; set; }
		}
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
		private static string   _messages;
		private bool            _readError;
		private PrtgClient      _client;
		private Scheduler		_scheduler;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);

			base.LoadAssembly("RestSharp.dll");
			base.LoadAssembly("Abraham.PrtgClient.dll");
			base.LoadAssembly("Abraham.Scheduler.dll");

			InitConfiguration();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public override ModuleSpecificConfig GetModuleSpecificConfig()
		{
			return _myConfiguration;
		}

		public override void CreateSeedData()
		{
			_myConfiguration						   = new MyConfiguration();
            _myConfiguration.PrtgURL                   = "(address of your PRTG server)";
            _myConfiguration.UsePasshashAuthentication = true; // "true to use username/password authentication, otherwise false";
            _myConfiguration.Username                  = "(your PRTG username)"; //"the PRTG accounts' name";
            _myConfiguration.Passwort                  = "(your PRTG password)"; //the PRTG accounts' password";
            _myConfiguration.UseApitokenAuthentication = false; //"true to use an apitoken to authenticate, otherwise false";
            _myConfiguration.Apitoken                  = "(your token if you use apitoken authentication)";
            _myConfiguration.BypassSslValidation       = true; //"true to skip SSL certificate validation";
            _myConfiguration.UpdateIntervalInSeconds   = 60;   // "how often to re-read the complete sensor tree";
			_myConfiguration.TimeoutInSeconds		   = 30;   // "when to about reading";
		}

		public override void Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void Recreate()
        {
            base.Recreate();
        }

        public override void UpdateContent(Abraham.HomenetBase.Models.DataObject? dataObject)
		{
			UpdateDisplay();
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", @"Dieses Modul fragt einen PRTG server ab und liest alle Sensoren ein.");
            return texts;
        }

		public override (bool,string) Validate()
		{
            return (true, "");
        }

		public override (bool,string) Test()
		{
			try
			{
				ReadSensorTreeNow().GetAwaiter().GetResult();
				Value = FormatDisplay();
				return (true, $"Daten vom PRTG Server gelesen!");
			}
			catch (Exception ex) 
			{
				return (false, _messages + ex.ToString());
			}
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

			
			_scheduler = new Scheduler()
				.UseAsyncAction(async () => await ReadSensorTreeNow())
				.UseFirstStartRightNow()
				.UseIntervalSeconds(_myConfiguration.UpdateIntervalInSeconds)
				.Start();
		}

		private void UpdateDisplay()
		{
            if (_readError)
			{
				Value = "???";
				NotifyPropertyChanged(nameof(Value));
				return;
			}

			try
			{
				if (!string.IsNullOrWhiteSpace(_messages) || _client?.SensorTree is null)
				{
					Value = "";
					NotifyPropertyChanged(nameof(Value));
					return;
				}
				Value = FormatDisplay();
			}
			catch (Exception)
			{
				Value = "???";
			}

			NotifyPropertyChanged(nameof(Value));
		}

		private string FormatDisplay()
		{
			var sensors = GetAllSensors();
			var sensor = sensors.FirstOrDefault(x => x.Ids.Contains(_myConfiguration.SensorID));
			if (sensor is null)
				sensor = sensors.First();
			if (sensor is null)
				return "??? sensor not found";

			var property = _myConfiguration.Property switch
			{
				"Name"                   => sensor.Name                 ,
				"Ids"                    => string.Join(',', sensor.Ids),
				"Url"                    => sensor.Url                  ,
				"Tags"                   => sensor.Tags                 ,
				"Priority"               => sensor.Priority             .ToString(),
				"Fixed"                  => sensor.Fixed                .ToString(),
				"Hascomment"             => sensor.Hascomment           .ToString(),
				"Sensortype"             => sensor.Sensortype           ,
				"Sensorkind"             => sensor.Sensorkind           ,
				"Interval"               => sensor.Interval             .ToString(),
				"StatusRaw"              => sensor.StatusRaw            .ToString(),
				"Status"                 => sensor.Status               ,
				"Datamode"               => sensor.Datamode             .ToString(),
				"Lastvalue"              => sensor.Lastvalue            ,
				"LastvalueRaw"           => sensor.LastvalueRaw         ,
				"Statusmessage"          => sensor.Statusmessage        ,
				"StatussinceRawUtc"      => sensor.StatussinceRawUtc    ,
				"LasttimeRawUtc"         => sensor.LasttimeRawUtc       ,
				"LastokRawUtc"           => sensor.LastokRawUtc         ,
				"LasterrorRawUtc"        => sensor.LasterrorRawUtc      ,
				"LastupRawUtc"           => sensor.LastupRawUtc         ,
				"LastdownRawUtc"         => sensor.LastdownRawUtc       ,
				"CumulateddowntimeRaw"   => sensor.CumulateddowntimeRaw ,
				"CumulateduptimeRaw"     => sensor.CumulateduptimeRaw   ,
				"CumulatedsinceRaw"      => sensor.CumulatedsinceRaw    ,
				"Active"                 => sensor.Active               .ToString(),
				"Text"                   => sensor.Text                 ,
				_							   => "Property not found"
			};
			
			return string.Format(_myConfiguration.Format, property);
		}

		private Abraham.Prtg.Models.Sensor GetSensorById(int id)
		{
			var sensors = GetAllSensors();
			return sensors.FirstOrDefault(x => x.Ids.Contains(id));

			//if (_client.SensorTree is null)
			//	return null;
            //var nodes = _client.GetNodes(_client.SensorTree);
            //foreach(var node in nodes)
            //{
			//	if (node.Group is not null)
			//	{
			//		System.Diagnostics.Debug.WriteLine($"Group {node.Group.Name,-20}");
			//		foreach (var probeNode in node.Group?.Probenodes)
			//			System.Diagnostics.Debug.WriteLine($"        ProbeNode {probeNode.Name,-20}");
			//		foreach (var group in node.Group.Groups)
			//			System.Diagnostics.Debug.WriteLine($"        Subgroup {group.Name,-20}");
			//		foreach (var device in node.Group.Devices)
			//		{
			//			System.Diagnostics.Debug.WriteLine($"        Device IDs {string.Join(',', device.Ids)} {device.Name,-20}");
			//			foreach (var sensor in device.Sensors)
			//			{
			//				System.Diagnostics.Debug.WriteLine($"SensorIDs: {string.Join(',', sensor.Ids)} {sensor.Name}");
			//				if (sensor.Ids.Contains(id))
			//					return sensor;
			//			}
			//		}
			//	}
			//	if (node.Device is not null)
            //    {
            //        foreach (var sensor in node.Device.Sensors)
			//		{
			//			System.Diagnostics.Debug.WriteLine($"SensorIDs: {string.Join(',', sensor.Ids)} {sensor.Name}");
			//			if (sensor.Ids.Contains(id))
			//				return sensor;
			//		}
            //    }
            //}
			//
			//return null;
		}

		private List<Abraham.Prtg.Models.Sensor> GetAllSensors()
		{
			var allSensors = new List<Abraham.Prtg.Models.Sensor>();
			if (_client.SensorTree is null)
				return allSensors;

            var nodes = _client.GetNodes(_client.SensorTree);
            foreach(var node in nodes)
            {
				if (node.Group is not null)
				{
					System.Diagnostics.Debug.WriteLine($"Group {node.Group.Name,-20}");
					foreach (var probeNode in node.Group?.Probenodes)
						System.Diagnostics.Debug.WriteLine($"        ProbeNode {probeNode.Name,-20}");
					foreach (var group in node.Group.Groups)
						System.Diagnostics.Debug.WriteLine($"        Subgroup {group.Name,-20}");
					foreach (var device in node.Group.Devices)
					{
						System.Diagnostics.Debug.WriteLine($"        Device IDs {string.Join(',', device.Ids)} {device.Name,-20}");
						allSensors.AddRange(device.Sensors);
					}
				}
				if (node.Device is not null)
                {
                    foreach (var sensor in node.Device.Sensors)
						System.Diagnostics.Debug.WriteLine($"SensorIDs: {string.Join(',', sensor.Ids)} {sensor.Name}");
					allSensors.AddRange(node.Device.Sensors);
                }
            }

			return allSensors;
		}

		private async Task ReadSensorTreeNow()
		{
            if (string.IsNullOrWhiteSpace(_myConfiguration.PrtgURL))
			{
				_readError = false;
                return;
			}

			try
			{
				_client = new PrtgClient()
					.UseURL(_myConfiguration.PrtgURL);

				if (_myConfiguration.UsePasshashAuthentication)
					_client.UsePasshashAuthentication(_myConfiguration.Username, _myConfiguration.Passwort);

				if (_myConfiguration.UseApitokenAuthentication)
					_client.UseApiTokenAuthentication(_myConfiguration.Apitoken);

				if (_myConfiguration.BypassSslValidation)
					_client.UseSSLValidationBypass();

				if (_myConfiguration.TimeoutInSeconds > 0)
						_client.UseConnectionTimeout(_myConfiguration.TimeoutInSeconds);

				// read the complete sensor tree (once).
				// you can control with your own code how often you query for updates.
				// you could use my "Abraham.Scheduler" nuget package to read on a regular basis.

				await _client.GetSensorTree();
				if (_client?.SensorTree is null)
					throw new Exception("Kein Zugriff auf den PRTG server!");

				_messages = "";
				_readError = false;
			}
			catch (Exception ex)
			{
				_messages = ex.ToString();
				_readError = true;
			}
		}
        #endregion
    }
}
