using System;
using System.Reflection;

namespace Abraham.PluginManager
{
	public class Processor
	{
		public string   Filename { get; internal set; }
		public Assembly Assembly { get; internal set; }
		public Type     Type     { get; internal set; }
		public object   Instance { get; internal set; }
	}
}
