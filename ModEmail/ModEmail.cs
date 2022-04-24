using OpenPop.Mime;
using OpenPop.Pop3;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Text;

namespace AllOnOnePage.Plugins
{
	public class ModEmail : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string Email          { get; set; }
			public string Username       { get; set; }
			public string Passwort       { get; set; }
			public string Pop3Server     { get; set; }
			public int    Pop3Port       { get; set; }
			public bool   UseSSL         { get; set; }
			public int    UpdateInterval { get; set; }
		}
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration    _myConfiguration;
		private Stopwatch          _stopwatch;
		private const int          ONE_MINUTE = 60 * 1000;
		private int                _updateIntervalInMinutes = 240;
		private static string      _messages;
		private List<Message>      _emails;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			InitConfiguration();
			InitEmailReader();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public override ModuleSpecificConfig GetModuleSpecificConfig()
		{
			return _myConfiguration;
		}

		public override void CreateSeedData()
		{
			_myConfiguration                = new MyConfiguration();
            _myConfiguration.Email          = "(Deine Email-Adresse)";
            _myConfiguration.Username       = "(Login für Dein Email-Konto, meist auch die Email-Adresse)";
            _myConfiguration.Passwort       = "(Dein Email-Kenwort)";
            _myConfiguration.Pop3Server     = "(Servername POP3)";
            _myConfiguration.Pop3Port       = 25;
            _myConfiguration.UseSSL         = false;
			_myConfiguration.UpdateInterval = 15;
		}

		public override void Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void Recreate()
        {
        }

        public override void UpdateContent()
		{
			ReadNewEmails();
			UpdateDisplay();
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"Dieses Modul sucht bestimmte Emails in Deinem Postfach und zeigt die Betreffzeilen an.
Die Zugangsdaten werden verschlüsselt abgelegt.
");
            return texts;
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

		private void UpdateDisplay()
		{
			if (!string.IsNullOrWhiteSpace(_messages))
			{
				Value = "";
				NotifyPropertyChanged(nameof(Value));
				return;
			}

			int anzahlDhlPakete = 0;
			var emails = (from e in _emails orderby e.Headers.DateSent select e).ToList();
			foreach (var email in emails)
			{
				if (email.Headers.Subject.Contains("Ihr DHL Paket kommt bald"))
				{
					anzahlDhlPakete++;
					var rawMessage = Encoding.ASCII.GetString(email.RawMessage);
				}
			}
				
			Value = $"{anzahlDhlPakete} DHL-{((anzahlDhlPakete > 1) ? "Pakete" : "Paket")}";
			NotifyPropertyChanged(nameof(Value));
		}

		private void InitEmailReader()
		{
			base.LoadAssembly("OpenPop.dll");
			RegisterCodepageProvider();
		}

		private void ReadNewEmails()
		{
			if (_stopwatch == null)
			{
				_stopwatch = Stopwatch.StartNew();
				ReadNewEmailsNow();
			}
			else
			{
				if (_stopwatch.ElapsedMilliseconds > _updateIntervalInMinutes * ONE_MINUTE)
				{
					ReadNewEmailsNow();
					_stopwatch.Restart();
				}
			}
		}

		private void ReadNewEmailsNow()
		{
            if (string.IsNullOrWhiteSpace(_myConfiguration.Pop3Server) ||
				string.IsNullOrWhiteSpace(_myConfiguration.Username  ) ||
				string.IsNullOrWhiteSpace(_myConfiguration.Passwort  ) ||
				string.IsNullOrWhiteSpace(_myConfiguration.Email     ))
                return;

			try
			{
				_emails = ReadAllEmails(_myConfiguration.Pop3Server, 
										_myConfiguration.Pop3Port, 
										_myConfiguration.UseSSL, 
										_myConfiguration.Username, 
										_myConfiguration.Passwort);
				_messages = "";
			}
			catch (Exception ex)
			{
				_messages = ex.ToString();
			}
		}

        private List<Message> ReadAllEmails(string hostname, int port, bool useSsl, string username, string password)
        {
            // The client disconnects from the server when being disposed
            using(Pop3Client client = new Pop3Client())
            {
                client.Connect(hostname, port, useSsl);
                client.Authenticate(username, password);

                int messageCount = client.GetMessageCount();
                List<Message> allMessages = new List<Message>(messageCount);

                // Messages are numbered in the interval: [1, messageCount]
                // Ergo: message numbers are 1-based.
                // Most servers give the latest message the highest number
                for (int i = messageCount; i > 0; i--)
                {
                    allMessages.Add(client.GetMessage(i));
                }
                return allMessages;
            }
        }

		public void RegisterCodepageProvider()
		{
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
		}
        #endregion
    }
}
