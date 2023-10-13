using Abraham.HomenetBase.Connectors;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace AllOnOnePage.Plugins
{
	public class ApplicationData
	{
        public string               ProgramDirectory { get; set; }
        public string               PluginDirectory  { get; set; }
        public string               DataDirectory    { get; set; }
        
        // exclude from Newtonsoft.Json serialization 
        [JsonIgnore]
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