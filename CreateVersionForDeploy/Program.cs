using System;
using System.IO;

namespace CreateVersionForDeploy
{
	class Program
	{
		static void Main(string[] args)
		{
			const string quelldatei      = "Version.cs";
			const string zielverzeichnis = @"bin\debug\net6.0-windows";
			const string zieldatei       = @"bin\debug\net6.0-windows\setversion.cmd";


			Console.WriteLine("Create version file for deploy");
			if (!File.Exists(quelldatei))
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Kann Quelldatei nicht finden: {quelldatei} Dieses Verzeichnis ist im Quellcode hart verdrahtet!");
				return;
			}
			Console.WriteLine($"Datei existiert: '{Path.GetFullPath(quelldatei)}'");

			if (!Directory.Exists(zielverzeichnis))
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Kann Zielverzeichnis nicht finden: {zielverzeichnis} Dieses Verzeichnis ist im Quellcode hart verdrahtet!");
				return;
			}
			Console.WriteLine($"Zielverzeichnis existiert: '{zielverzeichnis}'");


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
			Console.WriteLine($"Datei eingelesen: '{Path.GetFullPath(quelldatei)}'");


			int position = fileContents.IndexOf("private const string VERSION =");
			if (position < 0)
				position = fileContents.IndexOf("public const string VERSION =");
			if (position < 0)
			{
				Console.WriteLine($"ERROR: Kann Versionsinformation in der Klasse {quelldatei} nicht finden!");
				Console.WriteLine($"ERROR: Erwartet wird diese Zeile: private const string VERSION = \"2020-11-05\"; (oder public) ");
				Console.WriteLine($"ERROR: Achte auf die Leerzeichen!");
				return;
			}


			fileContents = fileContents.Substring(position);
			var parts = fileContents.Split(new char[] { '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.GetLength(0) < 2)
			{
				Console.WriteLine($"ERROR: Kann Versionsinformation in der Klasse {quelldatei} nicht finden!");
				Console.WriteLine($"ERROR: Erwartet wird diese Zeile: private const string VERSION = \"2020-11-05\"; (oder public) ");
				Console.WriteLine($"ERROR: Achte auf die Leerzeichen!");
				return;
			}

			var version = parts[1].Trim();

			if (!version.StartsWith('"') || !version.EndsWith  ('"'))
			{
				Console.WriteLine($"ERROR: Kann Versionsinformation in der Klasse {quelldatei} nicht finden!");
				Console.WriteLine($"ERROR: Erwartet wird diese Zeile: private const string VERSION = \"2020-11-05\"; (oder public) ");
				Console.WriteLine($"ERROR: Achte auf die Leerzeichen!");
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
