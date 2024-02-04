using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using PluginBase;
using System.Threading.Tasks;
using Abraham.Mail;


namespace AllOnOnePage.Plugins
{
	public class ModEmail : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string Email          { get; set; }
			public string Username       { get; set; }
			public string Password       { get; set; }
			public string ImapServer     { get; set; }
			public int    ImapPort       { get; set; }
			public bool   UseSSL         { get; set; }
			public int    UpdateInterval { get; set; }
		}
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
		private Stopwatch       _stopwatch;
		private const int       _oneMinute = 60 * 1000;
		private int             _updateIntervalInMinutes = 60;
		private static string   _messages;
		private bool		    _readError;
        private List<Message>   _emails;
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
            _myConfiguration.Email          = "(Your email address)";
            _myConfiguration.Username       = "(Login for your email account, often your email address)";
            _myConfiguration.Password       = "(Your password)";
            _myConfiguration.ImapServer     = "(Server name IMAP)";
            _myConfiguration.ImapPort       = 993;
            _myConfiguration.UseSSL         = true;
			_myConfiguration.UpdateInterval = 30;
		}

		public override async Task Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void Recreate()
        {
        }

        public override void UpdateContent(ServerDataObjectChange? dataObject)
		{
			// we're not interested in MQTT or Home Automation messages
			if (dataObject is not null)
				return;
			System.Diagnostics.Debug.WriteLine("ModEmail: UpdateContent()");
			ReadNewEmails();
			_messages = "";
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"This module displays the number of unread emails in a given postbox");
            return texts;
        }

		public override async Task<(bool,string)> Validate()
		{
            return (true, "");
        }

		public override async Task<(bool,string)> Test()
		{
			try
			{
				ReadNewEmailsNow();
				UpdateDisplay();
				return (false, _messages);
			}
			catch (Exception) 
			{
				return (false, _messages);
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
				if (!string.IsNullOrWhiteSpace(_messages) && _emails is null)
				{
					Value = "???";
					NotifyPropertyChanged(nameof(Value));
					return;
				}

				if (_emails is null)
				{
					Value = "???";
				}
				else
				{
					var unreadEmails = _emails.ToList();
					Value = unreadEmails.Count().ToString();
				}
			}
			catch (Exception)
			{
				Value = "???";
			}

			NotifyPropertyChanged(nameof(Value));
		}

		private void InitEmailReader()
		{
			base.LoadAssembly("Abraham.Mail.dll");
			base.LoadAssembly("MailKit.dll");
			base.LoadAssembly("MimeKit.dll");
			RegisterCodepageProvider();
		}

		private void ReadNewEmails()
		{
			try
			{
				if (_stopwatch == null)
				{
					_stopwatch = Stopwatch.StartNew();
					ReadNewEmailsNow();
					UpdateDisplay();
				}
				else
				{
					if (_stopwatch.ElapsedMilliseconds > _updateIntervalInMinutes * _oneMinute)
					{
						ReadNewEmailsNow();
						UpdateDisplay();
						_stopwatch.Restart();
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
			}
		}

		private void ReadNewEmailsNow()
		{
			try
			{
				ReadAllEmails();
				_readError = false;
			}
			catch (Exception ex)
			{
				_messages += ex.ToString();
				_readError = true;
				_emails = null;
			}
		}

        private void ReadAllEmails()
        {
			_messages = "";

			_messages += "Connecting to the Email server...\n";
			var client = new Abraham.Mail.ImapClient()
				.UseHostname(_myConfiguration.ImapServer)
				.UseSecurityProtocol(_myConfiguration.UseSSL ? Abraham.Mail.Security.Ssl : Abraham.Mail.Security.None)
				.UsePort(_myConfiguration.ImapPort)
				.UseAuthentication(_myConfiguration.Username, _myConfiguration.Password)
				.Open();

			_messages += "Reading the folders...\n";
			var folders = client.GetAllFolders().ToList();

			_messages += "Selecting the inbox...\n";
			var inbox = client.GetFolderByName(folders, "inbox");

			_messages += "Reading all unread messages from inbox...\n";
			_emails = client.GetUnreadMessagesFromFolder(inbox).ToList();

			if (_emails is null)
				_messages += "Unable to read your inbox!\n";
            else
				_messages += $"{_emails?.Count()} Emails were read!";
        }

		public void RegisterCodepageProvider()
		{
            //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
		}
        #endregion
    }
}
