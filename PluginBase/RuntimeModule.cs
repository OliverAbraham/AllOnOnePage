namespace AllOnOnePage.Plugins
{
	public class RuntimeModule
    {
		public ModuleConfig Config { get; set; }
        public IPlugin      Plugin { get; set; }

		public RuntimeModule(ModuleConfig config)
		{
			Config = config;
		}

        public override string ToString()
        {
            return Config.ModuleName;
        }
    }
}
