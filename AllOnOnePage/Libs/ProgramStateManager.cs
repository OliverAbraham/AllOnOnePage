using Abraham.ProgramSettings;
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
		private ProgramSettingsManager<List<ModuleDefinition>> _StateManager;
		private List<ModuleDefinition> _ActiveModules;
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		/// <summary>
		/// We save the program state periodically
		/// </summary>
		public void Read_saved_state_from_disk()
        {
            try
            {
                _StateManager = new ProgramSettingsManager<List<ModuleDefinition>>(StateFilename)
                    .UseDotNetJsonSerializer();
                var Temp  = _StateManager.Load();
                if (Temp != null)
                    _ActiveModules = Temp;
            }
            catch (Exception)
            {
                //MessageBox.Show("There was a problem reading the state file from disk.\n" + ex.ToString());
                File.Delete(StateFilename);
            }
        }

		public void Save_state_to_disk()
        {
            if (_StateManager != null)
                _StateManager.Save(_ActiveModules);
        }
		#endregion
	}
}
