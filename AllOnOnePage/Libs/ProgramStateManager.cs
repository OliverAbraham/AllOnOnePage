using Abraham.ProgramSettingsManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;

namespace AllOnOnePage.Libs
{
	class ProgramStateManager
	{
		#region ------------- Properties ----------------------------------------------------------
		public string StateFilename { get; private set; }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private ProgramSettingsManager<List<ModuleDefinition>> _stateManager;
		private List<ModuleDefinition> _activeModules;
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		/// <summary>
		/// We save the program state periodically
		/// </summary>
		public void Read_saved_state_from_disk()
        {
            try
            {
                _stateManager = new ProgramSettingsManager<List<ModuleDefinition>>()
                    .UseFilename(StateFilename)
                    .Load();
                var Temp  = _stateManager.Data;
                if (Temp != null)
                    _activeModules = Temp;
            }
            catch (Exception)
            {
                //MessageBox.Show("There was a problem reading the state file from disk.\n" + ex.ToString());
                File.Delete(StateFilename);
            }
        }

		public void Save_state_to_disk()
        {
            if (_stateManager != null)
                _stateManager.Save(_activeModules);
        }
		#endregion
	}
}
