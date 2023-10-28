using Newtonsoft.Json;

namespace AllOnOnePage.Plugins
{
	public class ModuleConfig
    {
        #region ------------- Properties ----------------------------------------------------------
        public int      ID                   { get; set; }
        public string   ModuleName           { get; set; }
        public int      X                    { get; set; }
        public int      Y                    { get; set; }
        public int      W                    { get; set; }
        public int      H                    { get; set; }
        public int      FontSize             { get; set; }
        public string   TileType             { get; set; }
        public bool     DismissIfOverlapped  { get; set; }
        public string   ModuleData           { get; set; }
        public string   ModulePrivateData    { get; set; }
		public bool     IsFrameVisible       { get; set; }
		public string   FrameColor           { get; set; }
		public int      FrameThickness       { get; set; }
		public int      FrameRadius          { get; set; }
		public string   BackgroundColor      { get; set; }
		public string   TextColor            { get; set; }
        public int      TextPadding          { get; set; }
        #endregion



        #region ------------- Fields - not saved --------------------------------------------------
        [JsonIgnore] // exclude from Newtonsoft.Json serialization 
        public ApplicationData ApplicationData;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public ModuleConfig()
		{
            ModuleName         = "0"; 
            TileType           = "TEXTBLOCK";
            X                  = 100;
            Y                  = 100;
            W                  = 400;
            H                  = 200;
            FontSize           = 12;
			IsFrameVisible     = true;
			FrameColor         = "#FF333333"; // dark gray
			FrameThickness     = 3;
			FrameRadius        = 10;
			BackgroundColor    = "#FF778899"; // LightSlateGray
            ModulePrivateData  = "";
            TextColor          = "#FF000000"; // Black
            ApplicationData    = new ApplicationData();
            TextPadding        = 20;
		}
        #endregion



		#region ------------- Methods -------------------------------------------------------------
		public override string ToString()
        {
            return $"{ModuleName}";
        }

        public ModuleConfig Clone()
        {
            var New = new ModuleConfig();
            New.CopyPropertiesFrom(this);
            return New;
        }

		public void CopyPropertiesFrom(ModuleConfig source)
		{
            ID                     = source.ID;
            ModuleName             = source.ModuleName + " copy";
            X                      = source.X;
            Y                      = source.Y;
            W                      = source.W;
            H                      = source.H;
            FontSize               = source.FontSize;
            TileType               = source.TileType;
            DismissIfOverlapped    = source.DismissIfOverlapped;
            ModuleData             = source.ModuleData;
            IsFrameVisible         = source.IsFrameVisible;
            FrameColor             = source.FrameColor;
            FrameThickness         = source.FrameThickness;
		    FrameRadius            = source.FrameRadius;
		    BackgroundColor        = source.BackgroundColor;
		    TextColor              = source.TextColor;
            ApplicationData        = source.ApplicationData;
            ModulePrivateData      = source.ModulePrivateData;
		}
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        private string StringEntry(string fieldName, string value)
        {
            return $" {fieldName}: {Quoted(value),-30},";
        }

        private string NumberEntry(string fieldName, int value)
        {
            return $" {fieldName}: {value,4},";
        }

        private string Bool__Entry(string fieldName, bool value)
        {
            return $" {fieldName}: {value.ToString().ToLower(),5},";
        }

        private string Quoted(string name)
        {
            return "\"" + name + "\"";
        }
        #endregion
    }
}
