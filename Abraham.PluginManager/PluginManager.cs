using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Abraham.PluginManager
{
	public class PluginManager
	{
		#region ------------- Properties ----------------------------------------------------------
		public string          StartDirectory                { get; set; }
		public string          PluginFolderName              { get; set; } = "plugins";
		public bool            SearchForPluginsInOwnAssembly { get; set; }
		public List<string>    Filenames                     { get; set; }
		public List<Processor> Processors                    { get; set; }
		public string          Messages                      { get; private set; }
		public string          ActualPluginDirectory         { get; private set; }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public PluginManager()
		{
			StartDirectory = Directory.GetCurrentDirectory();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public void LoadPlugins<T>()
		{
			Filenames = FindPluginDlls();
			Processors = LoadAndActivatePlugins<T>(Filenames);
		}

		public List<string> FindPluginDlls()
		{
			var foundDllFilenames = new List<string>();

			var dir = StartDirectory;
			if (Directory.Exists(dir + Path.DirectorySeparatorChar + PluginFolderName))
			{
				dir += Path.DirectorySeparatorChar + PluginFolderName;
			}
			else if (Directory.Exists(dir + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + PluginFolderName))
			{
				dir += Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + PluginFolderName;
			}
			else
			{
				Log($"No plugin directory found, trying the current directory.");
			}


			if (!string.IsNullOrWhiteSpace(dir))
			{
				Log($"Searching for plugins in plugin subdirectory '{dir}'");
				var filenames = Directory.GetFiles(dir, "Mod*.dll");
				foundDllFilenames.AddRange(filenames);
				ActualPluginDirectory = dir;
			}

			return foundDllFilenames;
		}

		public List<Processor> LoadAndActivatePlugins<T>(List<string> filenames)
		{
			var allProcessors = new List<Processor>();

			foreach (var filename in filenames)
			{
				var processors = TryLoadingOneAssembly<T>(filename);
				allProcessors.AddRange(processors);
			}

			if (SearchForPluginsInOwnAssembly)
			{
				//Assembly ourAssembly = typeof(RuleProcessing.BaseRule).Assembly;
				//Search_and_activate_plugins(foundProcessors, ourAssembly);
			}

			return allProcessors;
		}

		public List<Processor> TryLoadingOneAssembly<T>(string filename)
		{
			Log($"attempting to load plugin assembly '{filename}'");
			try
			{
				var assembly = Assembly.LoadFile(filename);
				if (assembly != null)
					return SearchAndInstantiateProcessorsInAssembly<T>(assembly, filename);
			}
			catch (Exception ex)
			{
				Log($"error loading assembly {filename}': {ex.ToString()}");
			}
			return new List<Processor>();
		}

		public List<Processor> SearchAndInstantiateProcessorsInAssembly<T>(Assembly assembly, string filename)
		{
			var allProcessors = new List<Processor>();
			foreach (Type type in assembly.GetTypes())
			{
				var ImplementsInterface = typeof(T).IsAssignableFrom(type) && type.IsClass;
				if (ImplementsInterface)
				{
					var processor = InstantiateProcessor<T>(assembly, filename, type);
					allProcessors.Add(processor);
				}
			}
			return allProcessors;
		}

		public Processor InstantiateProcessor<T>(Assembly assembly, string filename, Type type)
		{
			Log($"found processor of type '{typeof(T).ToString()}'");
			var processor = new Processor();
			processor.Filename = filename;
			processor.Assembly = assembly;
			processor.Type = type;
			processor.Instance = Activator.CreateInstance(type);
			Log($"instantiated processor of type '{type.ToString()}'");
			return processor;
		}
		#endregion



		#region ------------- Implementation ------------------------------------------------------
		private void Log(string message)
		{
			Messages += message + "\n";
		}
		#endregion
	}
}
