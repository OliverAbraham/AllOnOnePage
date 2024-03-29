﻿using AllOnOnePage.Plugins;
using System.Collections.Generic;
using System.ComponentModel;

namespace AllOnOnePage
{
    public class Configuration
	{
		#region ------------- Types and constants -------------------------------------------------
		public enum BackgroundType { Image, Color }
		#endregion



		#region ------------- Properties ----------------------------------------------------------
        [Browsable(false)]
		public BackgroundType     Background                   { get; set; }
        [Browsable(false)]
		public string             BackgroundImage              { get; set; }
        [Browsable(false)]
		public string             BackgroundColorRGB           { get; set; }
        public int                UpdateIntervalInSeconds      { get; set; }
        [Browsable(false)]
		public bool               FullScreenDisplay            { get; set; }
        [Browsable(false)]
        public bool               LogToConsole                 { get; set; }
        public bool               LogToFile                    { get; set; }
        public string             LogfileName                  { get; set; }
		public bool               WelcomeScreenDisabled        { get; set; }
		[Browsable(false)]
		public bool               EditModeWasEntered           { get; set; }
        public bool               DisableUpdate                { get; set; }
		[Browsable(false)]
        public List<ModuleConfig> Modules                      { get; set; }
        public string             HomeAutomationServerUrl      { get; set; }
        public string             HomeAutomationServerUser     { get; set; }
        public string             HomeAutomationServerPassword { get; set; }
        public int				  HomeAutomationServerTimeout  { get; set; }
        public string             MqttBrokerUrl                { get; set; }
        public string             MqttBrokerUser               { get; set; }
        public string             MqttBrokerPassword           { get; set; }
        public int				  MqttBrokerTimeout            { get; set; }
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
			Background				     = source.Background			       ;
			BackgroundImage              = source.BackgroundImage              ;
			BackgroundColorRGB           = source.BackgroundColorRGB           ;
			UpdateIntervalInSeconds      = source.UpdateIntervalInSeconds      ;
			FullScreenDisplay            = source.FullScreenDisplay            ;
			LogToConsole                 = source.LogToConsole                 ;
			LogToFile                    = source.LogToFile                    ;
			LogfileName                  = source.LogfileName                  ;
			WelcomeScreenDisabled        = source.WelcomeScreenDisabled        ;
			EditModeWasEntered           = source.EditModeWasEntered           ;
			Modules					     = source.Modules				       ;
			DisableUpdate			     = source.DisableUpdate			       ;
			HomeAutomationServerUrl      = source.HomeAutomationServerUrl      ;
			HomeAutomationServerUser     = source.HomeAutomationServerUser     ;
			HomeAutomationServerPassword = source.HomeAutomationServerPassword ;
			HomeAutomationServerTimeout  = source.HomeAutomationServerTimeout  ;
			MqttBrokerUrl                = source.MqttBrokerUrl                ;
			MqttBrokerUser               = source.MqttBrokerUser               ;
			MqttBrokerPassword           = source.MqttBrokerPassword           ;
			MqttBrokerTimeout            = source.MqttBrokerTimeout            ;
		}

        public void AssignUniqueIDsToModules()
        {
			int enumerator = 1;
			foreach (var module in Modules)
			{
				if (module.ID == 0)
					module.ID = enumerator++;
			}
        }
        #endregion
    }
}
