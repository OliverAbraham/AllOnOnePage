using AllOnOnePage.Plugins;
using System;
using System.Collections.Generic;

namespace AllOnOnePage
{
	public class Configuration
	{
		#region ------------- Types and constants -------------------------------------------------
		public enum BackgroundType { Image, Color }
		#endregion



		#region ------------- Properties ----------------------------------------------------------
        public BackgroundType     Background                   { get; set; }
        public string             BackgroundImage              { get; set; }
        public string             BackgroundColorRGB           { get; set; }
        public int                UpdateIntervalInSeconds      { get; set; }
        public bool               FullScreenDisplay            { get; set; }
        public bool               LogToConsole                 { get; set; }
        public bool               LogToFile                    { get; set; }
        public string             LogfileName                  { get; set; }
		public bool               WelcomeScreenDisabled        { get; set; }
		public bool               EditModeWasEntered           { get; set; }
        public bool               DisableUpdate                { get; set; }
        public List<ModuleConfig> Modules                      { get; set; }
        public string             HomeAutomationServerUrl      { get; set; }
        public string             HomeAutomationServerUser     { get; set; }
        public string             HomeAutomationServerPassword { get; set; }
        public int				  HomeAutomationServerTimeout  { get; set; }
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public Configuration()
		{
            Modules = new List<ModuleConfig>();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public Configuration Clone()
		{
            var New = new Configuration();
            New.CopyPropertiesFrom(this);
            return New;
        }

		public void CopyPropertiesFrom(Configuration source)
		{
			Background				     = Background			       ;
			BackgroundImage              = BackgroundImage             ;
			BackgroundColorRGB           = BackgroundColorRGB          ;
			UpdateIntervalInSeconds      = UpdateIntervalInSeconds     ;
			FullScreenDisplay            = FullScreenDisplay           ;
			LogToConsole                 = LogToConsole                ;
			LogToFile                    = LogToFile                   ;
			LogfileName                  = LogfileName                 ;
			WelcomeScreenDisabled        = WelcomeScreenDisabled       ;
			EditModeWasEntered           = EditModeWasEntered          ;
			Modules					     = Modules				       ;
			DisableUpdate			     = DisableUpdate			   ;
			HomeAutomationServerUrl      = HomeAutomationServerUrl     ;
			HomeAutomationServerUser     = HomeAutomationServerUser    ;
			HomeAutomationServerPassword = HomeAutomationServerPassword;
		}
		#endregion



		#region ------------- Implementation ------------------------------------------------------
		#endregion
	}
}
