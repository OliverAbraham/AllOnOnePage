using System;
using System.IO;

namespace CreateVersionForDeploy
{
	class Program
	{
		static void Main(string[] args)
		{
			const string quelldatei      = "MainWindow.xaml.cs";
			const string zielverzeichnis = @"bin\debug\netcoreapp3.1";
			const string zieldatei       = @"bin\debug\netcoreapp3.1\setversion.cmd";


			Console.WriteLine("Create version file for deploy");
			if (!File.Exists(quelldatei))
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Kann Quelldatei nicht finden: {quelldatei}");
				return;
			}
			if (!Directory.Exists(zielverzeichnis))
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Kann Zielverzeichnis nicht finden: {zielverzeichnis}");
				return;
			}


			string fileContents = "";
			try
			{
				fileContents = File.ReadAllText(quelldatei);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Kann Quelldatei nicht lesen! {ex.ToString()}");
				return;
			}


			int position = fileContents.IndexOf("private const string VERSION =");
			if (position < 0)
			{
				Console.WriteLine($"ERROR: Kann Versionsinformation in der Klasse {quelldatei} nicht finden!");
				Console.WriteLine($"ERROR: Erwartet wird diese Zeile: private const string VERSION = \"2020-11-05\"; ");
				return;
			}


			fileContents = fileContents.Substring(position);
			var parts = fileContents.Split(new char[] { '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.GetLength(0) < 2)
			{
				Console.WriteLine($"ERROR: Kann Versionsinformation in der Klasse {quelldatei} nicht finden!");
				Console.WriteLine($"ERROR: Erwartet wird diese Zeile: private const string VERSION = \"2020-11-05\"; ");
				return;
			}

			var version = parts[1].Trim();

			if (!version.StartsWith('"') || !version.EndsWith  ('"'))
			{
				Console.WriteLine($"ERROR: Kann Versionsinformation in der Klasse {quelldatei} nicht finden!");
				Console.WriteLine($"ERROR: Erwartet wird diese Zeile: private const string VERSION = \"2020-11-05\"; ");
				return;
			}


			var versionCommand = "";
			try
			{
				versionCommand = $"set VERSION={version}\n";
				versionCommand += $"set VERSION2={version.Trim('"')}\n";
				File.WriteAllText(zieldatei, versionCommand);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Kann Zieldatei nicht erzeugen! {ex.ToString()}");
				return;
			}

			Console.WriteLine($"******************************************************************************************************************************************************");
			Console.WriteLine($"******************* CreateVersionForDeploy: Versionsdatei erzeugt: {zieldatei} Version ist {version} *******************");
			Console.WriteLine($"******************************************************************************************************************************************************");
			return;
		}
	}
}
