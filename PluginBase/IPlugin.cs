﻿using PluginBase;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AllOnOnePage.Plugins
{
	public interface IPlugin
	{
		void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher);
		void CreateSeedData();
		void Stop();
		void Delete();
		void Recreate();
		void Time();
		void UpdateLayout();
		void UpdateContent(ServerDataObjectChange? dataObject);
		bool HitTest(object control, Point mousePosition);
		string GetName();
		ModuleConfig GetModuleConfig();
		Thickness GetPositionAndWidth();
		Thickness GetPositionAndCorrectSize();
		void SetPosition(double left, double top);
		void SetSize(double width, double height);
		void SwitchEditMode(bool on);
		void MouseMove(bool on);
		void OverlapEvent(Visibility visibility);
		Visibility GetVisibility();
		ModuleSpecificConfig GetModuleSpecificConfig();
		void CleanupModuleSpecificConfig();
		Task Save();
        void Clone(ModuleConfig existingModuleConfig);
		Dictionary<string,string> GetHelp();
		Task<(bool,string)> Validate();
		Task<(bool success, string messages)> Test();
    }
}
