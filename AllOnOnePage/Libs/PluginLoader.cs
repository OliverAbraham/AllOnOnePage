using Abraham.PluginManager;
using AllOnOnePage.Plugins;
using System.Collections.Generic;

namespace AllOnOnePage.Libs
{
	class PluginLoader
	{
		#region ------------- Properties ----------------------------------------------------------
		public string Messages { get; set; }
		public List<Processor> Processors { get; private set; }
		public string ActualPluginDirectory { get; private set; }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private PluginManager _pluginManager;
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public void LoadPlugins(string programDirectory)
		{
			_pluginManager = new PluginManager();
			_pluginManager.StartDirectory = programDirectory;
			_pluginManager.LoadPlugins<ModBase>();//IPlugin>();
			Processors = _pluginManager.Processors;
			ActualPluginDirectory = _pluginManager.ActualPluginDirectory;
		}

		public void InitPlugins()
		{
			foreach (var processor in Processors)
				Send_Init_event(processor);
		}

		public void StopPlugins()
		{
			foreach (var processor in Processors)
				Send_Stop_event(processor);
		}

		public Processor InstantiateProcessor(Processor processor)
		{
			return _pluginManager.InstantiateProcessor<IPlugin>(processor.Assembly, processor.Filename, processor.Type);
		}
		#endregion



		#region ------------- Implementation ------------------------------------------------------

		private void Send_Init_event(Processor processor)
		{
			
		}

		private void Send_Stop_event(Processor processor)
		{
			((IPlugin)processor.Instance).Stop();
		}
		#endregion
	}
}
