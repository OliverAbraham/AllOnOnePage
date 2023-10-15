using Abraham.HomenetBase.Connectors;
using Newtonsoft.Json;

namespace AllOnOnePage.Plugins
{
    public class ApplicationData
	{
        public string               ProgramDirectory { get; set; }
        public string               PluginDirectory  { get; set; }
        public string               DataDirectory    { get; set; }
        
        [JsonIgnore] // exclude from Newtonsoft.Json serialization 
        public DataObjectsConnector _homenetConnector;

		public ApplicationData Clone()
		{
            var New = new ApplicationData();
            New.CopyPropertiesFrom(this);
            return New;
        }

		public void CopyPropertiesFrom(ApplicationData source)
		{
            ProgramDirectory  = source.ProgramDirectory;
            PluginDirectory   = source.PluginDirectory;
            DataDirectory     = source.DataDirectory;
            _homenetConnector = source._homenetConnector;
		}
	}
}