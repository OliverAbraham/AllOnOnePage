namespace AllOnOnePage.Plugins
{
	public class ApplicationDirectories
	{
        public string   ProgramDirectory     { get; set; }
        public string   PluginDirectory      { get; set; }
        public string   DataDirectory        { get; set; }

		public ApplicationDirectories Clone()
		{
            var New = new ApplicationDirectories();
            New.CopyPropertiesFrom(this);
            return New;
        }

		public void CopyPropertiesFrom(ApplicationDirectories source)
		{
            ProgramDirectory = source.ProgramDirectory;
            PluginDirectory  = source.PluginDirectory;
            DataDirectory    = source.DataDirectory;
		}
	}
}