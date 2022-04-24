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

			Data.Add(ID.INTRODUCTION1, "Willkommen bei All on one page!"); 
			Data.Add(ID.INTRODUCTION2, 
@"Dies ist der erste Programmstart.
Du kannst jetzt loslegen, Deine Page mit Inhalten zu füllen.

Doppelklicke auf den Hintergrund, um ihn anzupassen.
Doppelklicke auf ein Modul, um es anzupassen.
Klicke auf Plus, um neue Module hinzuzufügen.

Leg los!");

			Data.Add(ID.ERROR_HEADING, "Upps das sollte nicht passieren!");

			Data.Add(ID.DEMOTILE, 
@"Willkommen zu Deiner ersten Page!
Doppelklicke hier, um mich anzupassen.
Doppelklicke auf den Hintergrund, um ihn anzupassen.");

			Data.Add(ID.CLICKHERETOEND,  "hier klicken zum Beenden   "    + char.ConvertFromUtf32(0x27A1)); // arrow left: (0x2B05);
            Data.Add(ID.CLICKHERETOEDIT, "Neues Modul anlegen   " + char.ConvertFromUtf32(0x27A1));
            Data.Add(ID.CLICKHERETOFULL, "Vollbild   "   + char.ConvertFromUtf32(0x27A1));
			Data.Add(ID.UPDATETITLE    , "Ein Update ist verfügbar");
			Data.Add(ID.UPDATEAVAILABLE, 
@"Ein Update ist verfügbar.

Sie haben Version {0}.
Verfügbar ist {1}.

Wollen Sie das neueste Update laden?

");
            Data.Add(ID.MODULE_HELP    , "Hilfe zum Modul");

            Data.Add(ID.DELETE_TITLE   , "Löschen?");
            Data.Add(ID.DELETE_QUESTION, "Willst Du das Modul wirklich löschen?");
		}
		#endregion
	}
}
