using System.Collections.Generic;
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
		void UpdateContent();
		bool HitTest(object control);
		string GetName();
		ModuleConfig GetModuleConfig();
		Thickness GetPositionAndSize();
		void SetPosition(double left, double top);
		void SetSize(double width, double height);
		void SwitchEditMode(bool on);
		void MouseMove(bool on);
		void OverlapEvent(Visibility visibility);
		Visibility GetVisibility();
		ModuleSpecificConfig GetModuleSpecificConfig();
		void CleanupModuleSpecificConfig();
		void Save();
		Dictionary<string,string> GetHelp();
		(bool,string) Validate();
		(bool success, string messages) Test();
	}
}
