using System;
using System.IO;

namespace CreateVersionForDeploy
{
	class Program
	{
		static void Main(string[] args)
		{
			string versionInfoFile  = "Version.cs";
			string sourcedirectory  = args[0];
			string setupDirectory   = args[1];
			string destinationfile1 = Path.Combine(sourcedirectory, "setversion.cmd");
			string destinationfile2 = Path.Combine(setupDirectory, "setinstallerversion.cmd");


			Console.WriteLine($"-------------------------------------------------------------------------------------------------");
			Console.WriteLine($"CREATE VERSION INFORMATION FOR DEPLOY AND SETUP        (CreateVersionForDeploy)");
			Console.WriteLine($"");
			Console.WriteLine($"Cmdline arguments        : {args[1]}");
			Console.WriteLine($"Version info file        : {Path.GetFullPath(versionInfoFile)} (hard coded)");
			Console.WriteLine($"Bin dir                  : {Path.GetFullPath(sourcedirectory)}");
			Console.WriteLine($"Setup dir                : {Path.GetFullPath(setupDirectory)}");
			Console.WriteLine($"-------------------------------------------------------------------------------------------------");
			

			if (!Directory.Exists(sourcedirectory))
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Directory doesn't exist: {Path.GetFullPath(sourcedirectory)} please check the command line parameters");
				return;
			}
			Console.WriteLine($"Directory exists: '{Path.GetFullPath(sourcedirectory)}'");


			if (!File.Exists(versionInfoFile))
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Kann Quelldatei nicht finden: {versionInfoFile} Dieses Verzeichnis ist im Quellcode hart verdrahtet!");
				return;
			}
			Console.WriteLine($"Source file exists: '{Path.GetFullPath(versionInfoFile)}'");
			string fileContents = "";
			try
			{
				fileContents = File.ReadAllText(versionInfoFile);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Cannot read file! {ex}");
				return;
			}
			Console.WriteLine($"File contents : '{Path.GetFullPath(fileContents)}'");
			Console.WriteLine($"now extracting the version");

			int position = fileContents.IndexOf("private const string VERSION =");
			if (position < 0)
				position = fileContents.IndexOf("public const string VERSION =");
			if (position < 0)
			{
				Console.WriteLine($"ERROR: Cannot find version information in {versionInfoFile}!");
				Console.WriteLine($"ERROR: Expecting this line: private const string VERSION = \"2020-11-05\"; (or public) ");
				Console.WriteLine($"ERROR: Check the whitespace!");
				return;
			}
			fileContents = fileContents.Substring(position);
			var parts = fileContents.Split(new char[] { '=', ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.GetLength(0) < 2)
			{
				Console.WriteLine($"ERROR: Cannot find version information in {versionInfoFile}!");
				Console.WriteLine($"ERROR: Expecting this line: private const string VERSION = \"2020-11-05\"; (or public) ");
				Console.WriteLine($"ERROR: Check the whitespace!");
				return;
			}
			var version = parts[1].Trim();
			if (!version.StartsWith('"') || !version.EndsWith  ('"'))
			{
				Console.WriteLine($"ERROR: Cannot find version information in {versionInfoFile}!");
				Console.WriteLine($"ERROR: Expecting this line: private const string VERSION = \"2020-11-05\"; (or public) ");
				Console.WriteLine($"ERROR: Check the whitespace!");
				return;
			}
			Console.WriteLine($"Version is set to: '{version}'");


			Console.WriteLine($"Creating batch file '{Path.GetFullPath(destinationfile1)}'");
			var batchFileContents = $"set VERSION={version}\nset VERSION2={version.Trim('"')}\n";
			try
			{
				File.WriteAllText(destinationfile1, batchFileContents);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Cannot create file! {ex}");
				return;
			}
			Console.WriteLine($"File created");
			Console.WriteLine($"File contents:\n{batchFileContents}");


			Console.WriteLine($"Creating 2nd batch file for installer creation '{Path.GetFullPath(destinationfile2)}'");
			try
			{
				File.WriteAllText(destinationfile2, batchFileContents);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"CreateVersionForDeploy ERROR: Cannot create file! {ex}");
				return;
			}
			Console.WriteLine($"File created");
			Console.WriteLine($"File contents:\n{batchFileContents}");


			Console.WriteLine($"******************************************************************************************************************************************************");
			Console.WriteLine($"***************************************** CreateVersionForDeploy: Files created **********************************************************************");
			Console.WriteLine($"******************************************************************************************************************************************************");
			return;
		}
	}
}
