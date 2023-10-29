using System.Collections.Generic;

namespace AllOnOnePage
{
	public class HelpTexts
	{
		#region ------------- Types and constants -------------------------------------------------
		public class ID
		{
			public const string INTRODUCTION1   = "INTRODUCTION1";
			public const string INTRODUCTION2   = "INTRODUCTION2";
			public const string ERROR_HEADING   = "ERROR_HEADING";
			public const string DEMOTILE        = "DEMOTILE";
			public const string CLICKHERETOEND  = "CLICKHERETOEND";
            public const string CLICKHERETOEDIT = "CLICKHERETOEDIT";
			public const string CLICKHERETOFULL = "CLICKHERETOFULL";
			public const string UPDATETITLE     = "UPDATETITLE";
			public const string UPDATEAVAILABLE = "UPDATEAVAILABLE";
			public const string MODULE_HELP     = "MODULE_HELP";
			public const string DELETE_TITLE    = "DELETE_TITLE";
			public const string DELETE_QUESTION = "DELETE_QUESTION";
		}
		#endregion



		#region ------------- Properties ----------------------------------------------------------
		public Dictionary<string,string> Data { get; private set; }

		public string  this[string index]
		{
			get { return Data[index]; }
		}
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public HelpTexts()
		{
			Data = new Dictionary<string, string>();

			Data.Add(ID.INTRODUCTION1, "Welcome to All on one page!"); 
			Data.Add(ID.INTRODUCTION2, 
@"This is the first start of the program.
You can now start filling your page with content.

Double-click the background to customize it.
Double-click a module to customize it.
Click Plus to add new modules.

Let's go!");

			Data.Add(ID.ERROR_HEADING, "Oops, that shouldn't happen!");

			Data.Add(ID.DEMOTILE, 
@"Welcome to your first page!
Double click here to customize.
Double-click the background to customize it.");

			Data.Add(ID.CLICKHERETOEND,  "click here to end   "    + char.ConvertFromUtf32(0x27A1)); // arrow left: (0x2B05);
            Data.Add(ID.CLICKHERETOEDIT, "create a new module   " + char.ConvertFromUtf32(0x27A1));
            Data.Add(ID.CLICKHERETOFULL, "Fullscreen   "   + char.ConvertFromUtf32(0x27A1));
			Data.Add(ID.UPDATETITLE    , "An update is available!");
			Data.Add(ID.UPDATEAVAILABLE, 
@"An update is available.

You have version {0}.
Available is {1}.

Do you want to download the latest update?

");
            Data.Add(ID.MODULE_HELP    , "Help for module");

            Data.Add(ID.DELETE_TITLE   , "Delete?");
            Data.Add(ID.DELETE_QUESTION, "Do you really want to delete the module?");
		}
		#endregion
	}
}
