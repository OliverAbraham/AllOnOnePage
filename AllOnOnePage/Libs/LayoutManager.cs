using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Controls;
using System.Windows;

namespace AllOnOnePage
{
    /// <summary>
    /// Klasse zur einfachen Speicherung und Wiederherstellung von WPF-Fensterpositionen
    /// </summary>
    /// 
    /// <remarks>
    ///  Abraham Beratung 12/2013
    ///  Oliver Abraham
    ///  www.oliver-abraham.de
    ///  mail@oliver-abraham.de
    /// </remarks>
    public class LayoutManager
	{
		#region ------------- Properties ----------------------------------------------------------
		public LayoutContainer Data { get; set; }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
        private string _filename;
        private Window _window;
        private string _key;
		private bool _dataWasLoaded;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public LayoutManager()
        {
            _window = null;
            Data = new LayoutContainer();
            _filename = Environment.CurrentDirectory + @"\" + "UI-Layout.xml";
        }

        public LayoutManager(string dateiname)
        {
            _window = null;
            Data = new LayoutContainer();
            _filename = dateiname;
        }

        public LayoutManager(Window window, string key)
        {
            _window = window;
            _window.Loaded  += Window_Loaded;
            _window.Closing += Window_Closing;
            _window.Closed  += Window_Closed;
            _key = key;
            Data = new LayoutContainer();
            _filename = Environment.CurrentDirectory + @"\" + "UI-Layout.xml";

            if (Load())
			{
                _dataWasLoaded = true;
                //window.Visibility = Visibility.Hidden;
                RestoreWindowSizeAndPosition(_window, _key);
			}
        }
		#endregion



        #region ------------ Methoden -------------------------------------------------------------
        public bool Load()
        {
            if (!File.Exists(_filename))
                return false;
            XmlSerializer serializer = new XmlSerializer(typeof(LayoutContainer));
            FileStream fs = new FileStream(_filename, FileMode.Open);
            LayoutContainer Temp = (LayoutContainer)serializer.Deserialize(fs);
            fs.Close();

            if (Temp == null)
                return false;

            Data = Temp;
            return true;
        }

        public void Save()
		{
            XmlSerializer serializer = new XmlSerializer(typeof(LayoutContainer));
            FileStream fs = new FileStream(_filename, FileMode.Create);
            serializer.Serialize(fs, Data);
            fs.Close();
		}

        public void SaveSizeAndPosition(Window ctl, string key = "MainWindow")
        {
            LayoutElement e = FindOrCreateElement(key);
            e.WindowState = (int)ctl.WindowState;
            e.WindowStyle = (int)ctl.WindowStyle + 1000;
            e.Left        = (int)ctl.Left;
            e.Top         = (int)ctl.Top;
            e.Width       = (int)ctl.Width;
            e.Height      = (int)ctl.Height;
        }

        public void RestoreWindowSizeAndPosition(Window ctl, string key = "MainWindow")
        {
            LayoutElement e = FindElement(key);
            if (e == null)
                return;
            ctl.Left   = e.Left;
            ctl.Top    = e.Top;
            ctl.Width  = e.Width;
            ctl.Height = e.Height;
            if (e.WindowState == (int)WindowState.Maximized)
                ctl.WindowState = WindowState.Maximized;
            if (e.WindowState == (int)WindowState.Minimized)
                ctl.WindowState = WindowState.Minimized;
            if (e.WindowStyle-1000 == (int)WindowStyle.None)
                ctl.WindowStyle = WindowStyle.None;
        }

        public void SaveListboxColumnWidths(GridView ctl, string key = "Listbox1")
        {
            LayoutElement e = FindOrCreateElement(key);
            e.Values = GetListboxColumnWidths(ctl);
        }

        public void RestoreListboxColumnWidths(GridView ctl, string key = "Listbox1")
        {
            LayoutElement e = FindElement(key);
            if (e == null)
                return;
            RestoreListboxColumnWidths(ctl, e.Values);
        }

        public List<int> GetListboxColumnWidths(GridView control)
        {
            List<int> Breiten = new List<int>();
            foreach (GridViewColumn c in control.Columns)
                Breiten.Add((int)c.Width);
            return Breiten;
        }

        public void RestoreListboxColumnWidths(GridView control, List<int> breiten)
        {
            if (breiten == null || breiten.Count == 0)
                return;

            int AnzahlSpalten = control.Columns.Count;
            int Index = 0;
            foreach (int Breite in breiten)
            {
                if (Index >= AnzahlSpalten)
                    break;
                control.Columns[Index].Width = Breite;
                Index++;
            }
        }

        public void SaveListboxColumnOrder(GridView ctl, string key = "Listbox1")
        {
            LayoutElement e = FindOrCreateElement(key);
            e.Values = GetListboxColumnOrder(ctl);
        }

        public void RestoreListboxColumnOrder(GridView ctl, string key = "Listbox1")
        {
            LayoutElement e = FindElement(key);
            if (e == null)
                return;
            RestoreListboxColumnOrder(ctl, e.Values);
        }

        public List<int> GetListboxColumnOrder(GridView control)
        {
            List<int> Breiten = new List<int>();
            //foreach (GridViewColumn c in control.Columns)
            //    Breiten.Add(c.);
            return Breiten;
        }

        public void RestoreListboxColumnOrder(GridView control, List<int> breiten)
        {
            //if (breiten == null || breiten.Count == 0)
            //    return;

            //int AnzahlSpalten = control.Columns.Count;
            //int Index = 0;
            //foreach (int Breite in breiten)
            //{
            //    if (Index >= AnzahlSpalten)
            //        break;
            //    control.Columns[Index].Width = Breite;
            //    Index++;
            //}
        }

        //public void SaveSplitContainer(SplitContainer ctl, string key = "SplitContainer1")
        //{
        //    LayoutElement e = FindOrCreateElement(key);
        //    e.Value = ctl.SplitterDistance;
        //}

        //public void RestoreSplitContainer(SplitContainer ctl, string key = "SplitContainer1")
        //{
        //    LayoutElement e = FindElement(key);
        //    if (e == null)
        //        return;
        //    ctl.SplitterDistance = e.Value;
        //}
        
        public LayoutElement FindOrCreateElement(string key)
        {
            LayoutElement e = FindElement(key);
            if (e == null)
            {
                Data.Elements.Add(new LayoutElement(key));
                e = FindElement(key);
            }
            return e;
        }

        public LayoutElement FindElement(string key)
        {
            foreach (LayoutElement e in Data.Elements)
            {
                if (e.Key == key)
                    return e;
            }
            return null;
        }
        #endregion



		#region ------------- Implementation ------------------------------------------------------
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_dataWasLoaded)
			{
                RestoreWindowSizeAndPosition(_window, _key);
                //_window.Visibility = Visibility.Visible;
			}
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSizeAndPosition(_window, _key);
            Save();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _window.Loaded  -= Window_Loaded;
            _window.Closing -= Window_Closing;
            _window.Closed  -= Window_Closed;
        }
        #endregion
    }



    #region ------------- DTO ---------------------------------------------------------------------

    public class LayoutContainer
    {
        public List<LayoutElement> Elements            { get; set; }
        public int                 Splitter1Distance   { get; set; }
        public List<int>           Listbox1Columns     { get; set; }
        public List<int>           Listbox2Columns     { get; set; }
        public List<int>           Listbox3Columns     { get; set; }

        public LayoutContainer()
        {
            Elements = new List<LayoutElement>();
            Listbox1Columns = new List<int>();
            Listbox2Columns = new List<int>();
            Listbox3Columns = new List<int>();
        }
    }

    public class LayoutElement
    {
        public string    Key         { get; set; }
        public int       Left        { get; set; }
        public int       Top         { get; set; }
        public int       Width       { get; set; }
        public int       Height      { get; set; }
        public int       WindowState { get; set; }
        public int       Value       { get; set; }
        public List<int> Values      { get; set; }
		public int       WindowStyle { get; set; }

		public LayoutElement()
        {
            Values = new List<int>();
        }

        public LayoutElement(string key)
        {
            Key = key;
            Values = new List<int>();
        }
    }
    
    #endregion
}
