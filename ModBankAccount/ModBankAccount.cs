using Abraham.Security;
using libfintx;
using libfintx.Data;
using libfintx.Sample.Ui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AllOnOnePage.Plugins
{
	public class ModBankAccount : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig, INotifyPropertyChanged
		{
			public string IBAN          { get; set; }
			public string BIC           { get; set; }
			public string LoginName     { get; set; }
			public string LoginPIN      { get; set; }
			public string Format        { get; set; }
			public string Kontonummer   { get; set; }

			public string BLZ           ;
			public string BankURL       ;
			public string Zentrale      ;
			public string HBCIVersion   ;


			#region ------------- INotifyPropertyChanged ---------------------------
			[NonSerialized]
			private PropertyChangedEventHandler _PropertyChanged;
			public event PropertyChangedEventHandler PropertyChanged
			{
				add
				{
					_PropertyChanged += value;
				}
				remove
				{
					_PropertyChanged -= value;
				}
			}

			public void NotifyPropertyChanged(string propertyName)
			{
				PropertyChangedEventHandler Handler = _PropertyChanged; // avoid race condition
				if (Handler != null)
					Handler(this, new PropertyChangedEventArgs(propertyName));
			}
			#endregion
		}
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
		private Stopwatch       _stopwatch;
		private const int       ONE_MINUTE = 60 * 1000;
		private int             _updateIntervalInMinutes = 240;
		private static string   _messages;
		private TANDialog       _tanDialog;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			InitAccountReader();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public override ModuleSpecificConfig GetModuleSpecificConfig()
		{
			DecryptData();
			return _myConfiguration;
		}

		public override void CleanupModuleSpecificConfig()
		{
			_myConfiguration = null;
		}

		public override void CreateSeedData()
		{
			_myConfiguration               = new MyConfiguration();
            _myConfiguration.BLZ           = "Gib die Bankleitzahl Deiner Bank hier ein, z.B. 20010010";
            _myConfiguration.Kontonummer   = "Gib Deine Kontonummer hier ein, z.B. 1234567890";
            _myConfiguration.IBAN          = "Gib Deine IBAN hier ein, z.B. DE000000000000000001";
            _myConfiguration.BIC           = "Gib Deinen BIC hier ein, z.B. NOLADE21HOL";
            _myConfiguration.LoginName     = "Gib Deinen Bank-Login hier ein";
            _myConfiguration.LoginPIN      = "Gib Dein Passwort hier ein";
            _myConfiguration.Format        = "{0} €";
            _myConfiguration.Zentrale      = "";
            _myConfiguration.BankURL       = "";
			EncryptData();
		}

		public override (bool,string) Validate()
		{
			Bank bank = null;
			if (_myConfiguration.BIC.StartsWith("Gib Deinen"))
			{
				_myConfiguration.BIC = "";
				_myConfiguration.NotifyPropertyChanged(nameof(_myConfiguration.BIC));
			}

			if (!string.IsNullOrWhiteSpace(_myConfiguration.BLZ))
				bank = Bank.TryGetBankByBlz(_myConfiguration.BLZ);
			
			if (bank == null && !string.IsNullOrWhiteSpace(_myConfiguration.BIC))
				bank = Bank.TryGetBankByBIC(_myConfiguration.BIC);

			if (bank != null)
			{
				_myConfiguration.Zentrale    = bank.BlzZentrale;
				_myConfiguration.BIC         = bank.Bic;
				_myConfiguration.BLZ         = bank.Blz;
				_myConfiguration.BankURL     = bank.Url;
				_myConfiguration.HBCIVersion = "300";
				_myConfiguration.NotifyPropertyChanged(nameof(_myConfiguration.BLZ));
				_myConfiguration.NotifyPropertyChanged(nameof(_myConfiguration.BIC));
			}
			if (bank == null)
				return (false, "Die Bank konnte nicht ermittelt werden.\nBitte gib einen gültige BIC ein!");

			if (string.IsNullOrWhiteSpace(_myConfiguration.IBAN) ||
				_myConfiguration.IBAN.StartsWith("Gib Deine"))
				return (false, "Bitte gib eine gültige IBAN ein!");

			if (string.IsNullOrWhiteSpace(_myConfiguration.LoginName) ||
				_myConfiguration.LoginName.StartsWith("Gib Deinen"))
				return (false, "Bitte gib Deinen Bank-Login ein!");

			if (string.IsNullOrWhiteSpace(_myConfiguration.LoginPIN) ||
				_myConfiguration.LoginPIN.StartsWith("Gib Dein"))
				return (false, "Bitte gib eine gültige IBAN ein!");

			return (true, "");
		}

		public override (bool success, string messages) Test()
		{
			try
			{
				Task<string> task = Readbalance_internal();
				task.Wait();
				var value = task.Result;
				return (true, $"Testergebnis: '{value}'");
			}
			catch (Exception ex)
			{
				return (false, $"Testergebnis: '{ex.ToString()}'");
			}
		}

		public override void Save()
		{
			EncryptData();
		}

        public override void Recreate()
        {
        }

        public override void UpdateContent()
		{
			ReadNewBalanceEveryHour();
			UpdateDisplay();
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"Dieses Modul zeigt den aktuellen Kontostand eines Bankkontos an.
Die Kontodaten (IBAN, BIC, PIN etc.) werden verschlüsselt abgelegt.
");
            return texts;
        }
		#endregion



		#region ------------- Implementation ------------------------------------------------------
		private void DecryptData()
		{
			var encryptionLogic = new Encryption();
			try
			{
				encryptionLogic.Password = GeneratePassword();
				var plaintext = encryptionLogic.Decrypt(_config.ModulePrivateData);
				_myConfiguration = System.Text.Json.JsonSerializer.Deserialize<MyConfiguration>(plaintext);
				plaintext = "";
			}
            catch (Exception)
			{
			}
			finally
			{
				encryptionLogic.Password = "";
				encryptionLogic = null;
			}

			if (_myConfiguration == null)
				CreateSeedData();
		}

		private void EncryptData()
		{
			var encryptionLogic = new Encryption();
			try
			{
				var serializedConfig = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
				encryptionLogic.Password = GeneratePassword();
				_config.ModulePrivateData = encryptionLogic.Encrypt(serializedConfig);
			}
            catch (Exception)
			{
			}
			finally
			{
				encryptionLogic.Password = "";
				encryptionLogic = null;
			}
		}

		private string GeneratePassword()
		{
			return "sdk$jfh&asiudfh(kjg73§§453tkadgkd";
		}

		private void UpdateDisplay()
		{
			NotifyPropertyChanged(nameof(Value));
		}

		private void InitAccountReader()
		{
			base.LoadAssembly("SixLabors.Core.dll");
			base.LoadAssembly("SixLabors.Fonts.dll");
			base.LoadAssembly("SixLabors.ImageSharp.dll");
			base.LoadAssembly("SixLabors.ImageSharp.Drawing.dll");
			base.LoadAssembly("StatePrinter.dll");
			base.LoadAssembly("Zlib.Portable.dll");
			base.LoadAssembly("BouncyCastle.Crypto.dll");
			base.LoadAssembly("Microsoft.Extensions.Logging.dll");
			base.LoadAssembly("System.Security.Cryptography.Xml.dll");
		}

		private void ReadNewBalanceEveryHour()
		{
			if (_stopwatch == null)
			{
				_stopwatch = Stopwatch.StartNew();
				ReadBalance();
			}
			else
			{
				if (_stopwatch.ElapsedMilliseconds > _updateIntervalInMinutes * ONE_MINUTE)
				{
					ReadBalance();
					_stopwatch.Restart();
				}
			}
		}

		private async void ReadBalance()
		{
			DecryptData();
			(bool success, string messages) = Validate();
			if (!success)
				return;

			Value = await Readbalance_internal();

			_myConfiguration = null;
		}

		private async Task<string> Readbalance_internal()
		{
			string value = "";
			try
			{
				if (string.IsNullOrWhiteSpace(_myConfiguration.Kontonummer))
					return "";

				_tanDialog = new TANDialog(WaitForTanAsync, null);//pBox_tan);

				var connectionDetails = new ConnectionDetails()
				{
					Url     = _myConfiguration.BankURL,
					Account = _myConfiguration.Kontonummer,
					Blz     = Convert.ToInt32(_myConfiguration.BLZ),
					Pin     = _myConfiguration.LoginPIN,
					UserId  = _myConfiguration.LoginName,
					Iban    = _myConfiguration.IBAN,
					Bic     = _myConfiguration.BIC,
				};

				var client = new FinTsClient(connectionDetails);


				HBCIDialogResult sync;
				try
				{
					sync = await client.Synchronization();
				}
				catch (Exception ex)
				{
					_messages = ex.ToString();
					return "";
				}

				HBCIOutput(sync.Messages);

				if (sync.IsSuccess)
				{
					client.HIRMS = "";//txt_tanverfahren.Text;
					if (!await InitTANMedium(client))
						return "";
					var balance = await client.Balance(_tanDialog);
					HBCIOutput(balance.Messages);
					if (!balance.IsSuccess)
						return "";
					
					if (balance.Data != null)
						_messages = Convert.ToString(balance.Data.Balance);

					value = _messages;
					if (!string.IsNullOrWhiteSpace(_myConfiguration.Format) &&
						_myConfiguration.Format.Contains("{0}"))
					{
						value = _myConfiguration.Format.Replace("{0}", value);
					}
				}
			}
			catch (Exception ex)
			{
				_messages = ex.ToString();
			}
			return value;
		}

		private async Task<bool> InitTANMedium(FinTsClient client)
        {
            // TAN-Medium-Name
            var accounts = await client.Accounts(_tanDialog);
            if (!accounts.IsSuccess)
            {
                HBCIOutput(accounts.Messages);
                return false;
            }
            var conn = client.ConnectionDetails;
            AccountInformation accountInfo = UPD.HIUPD?.GetAccountInformations(conn.Account, conn.Blz.ToString());
            if (accountInfo != null && accountInfo.IsSegmentPermitted("HKTAB"))
            {
                client.HITAB = "";// txt_tan_medium.Text;
            }

            return true;
        }

        //private static async void Accounts(FinTsClient client)
        //{
        //    var result = await client.Accounts(new TANDialog(WaitForTanAsync));
        //    if (!result.IsSuccess)
        //    {
        //        HBCIOutput(result.Messages);
        //        return;
        //    }
		//
		//	_messages = $"Account count: {result.Data.Count}";
        //    foreach (var account in result.Data)
        //        _messages += $"Account - Holder: {account.AccountOwner}, Number: {account.AccountNumber}";
        //}
		//
        //private static async void Balance(FinTsClient client)
        //{
        //    var result = await client.Balance(new TANDialog(WaitForTanAsync));
        //    if (!result.IsSuccess)
        //        HBCIOutput(result.Messages);
		//	else
		//		_messages = $"Balance is: {result.Data.Balance}\u20AC";
        //}
		//
        //private static async void Transactions(FinTsClient client)
        //{
        //    var result = await client.Transactions(new TANDialog(WaitForTanAsync));
        //    if (!result.IsSuccess)
        //    {
        //        HBCIOutput(result.Messages);
        //        return;
        //    }
		//
		//	_messages = $"Transaction count: {result.Data.Count}";
        //    foreach (var trans in result.Data)
		//		_messages += $"Transaction - Start Date: {trans.StartDate}, Amount: {trans.EndBalance - trans.StartBalance}\u20AC";
        //}

        private static void HBCIOutput(IEnumerable<HBCIBankMessage> hbcimsg)
        {
			_messages = string.Join('\n', hbcimsg);
        }

        static async Task<string> WaitForTanAsync(TANDialog tanDialog)
        {
            foreach (var msg in tanDialog.DialogResult.Messages)
                Console.WriteLine(msg);

            return await Task.FromResult(Console.ReadLine());
        }

        #endregion
    }
}
