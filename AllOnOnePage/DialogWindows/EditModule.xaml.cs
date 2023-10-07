using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using AllOnOnePage.Plugins;

namespace AllOnOnePage.DialogWindows
{
    public partial class EditModule : Window, INotifyPropertyChanged
    {
        #region ------------- Properties ----------------------------------------------------------
		#endregion



		#region ------------- Control Properties --------------------------------------------------
		public string  Name           { get { return _plugin?.GetName(); } }
		public string  ModuleName     { get { return C.ModuleName;     } set {C.ModuleName     = value; } }
        public string  Type           { get { return C.TileType;       } set {C.TileType       = value; } }
        public int     ModuleFontSize { get { return C.FontSize;       } set {C.FontSize       = value; } }
        public bool    IsFrameVisible { get { return C.IsFrameVisible; } set {C.IsFrameVisible = value; } }
        public int     FrameThickness { get { return C.FrameThickness; } set {C.FrameThickness = value; } }
        public int     FrameRadius    { get { return C.FrameRadius;    } set {C.FrameRadius    = value; } }
		
        public Color FrameColor
        { 
            get 
            {
                if (_frameColor == null)
                    _frameColor = ColorManager.CreateColor(C.FrameColor);
                return _frameColor;
            } 
            set 
            {
                _frameColor = value; 
                C.FrameColor = ColorManager.ConvertColorToRgbString(_frameColor);
            } 
        }
        private Color _frameColor;
		
        public Color BackgroundColor
        { 
            get 
            {
                if (_backgroundColor == null)
                    _backgroundColor = ColorManager.CreateColor(C.BackgroundColor);
                return _backgroundColor;
            } 
            set 
            {
                _backgroundColor = value; 
                C.BackgroundColor = ColorManager.ConvertColorToRgbString(_backgroundColor);
            } 
        }
        private Color _backgroundColor;
		
        public Color TextColor
        { 
            get 
            {
                if (_textColor == null)
                    _textColor = ColorManager.CreateColor(C.TextColor);
                return _textColor;
            } 
            set 
            {
                _textColor = value; 
                C.TextColor = ColorManager.ConvertColorToRgbString(_textColor);
            } 
        }
        private Color _textColor;

		#endregion



		#region ------------- INotifyPropertyChanged ----------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _propertyChanged += value;
            }
            remove
            {
                _propertyChanged -= value;
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler Handler = _propertyChanged; // avoid race condition
            if (Handler != null)
                Handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion



        #region ------------- Fields --------------------------------------------------------------
        private IPlugin _plugin;
		private Window _parentWindow;
		private HelpTexts _texts;

		private ModuleConfig C => _plugin.GetModuleConfig();
        private ModuleConfig _config => _plugin.GetModuleConfig();

		public bool DeleteModule { get; private set; }

		private ModuleConfig _configBackup;
        [NonSerialized]
        private PropertyChangedEventHandler _propertyChanged;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public EditModule(IPlugin plugin, Window parentWindow, HelpTexts texts)
		{
            _plugin       = plugin       ?? throw new ArgumentNullException(nameof(plugin      ));
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
            _texts        = texts        ?? throw new ArgumentNullException(nameof(texts       ));

            _configBackup = _plugin.GetModuleConfig().Clone();
			InitializeComponent();
            DataContext = this;
            _backgroundColor = ColorManager.CreateColor(C.BackgroundColor); 
            _frameColor      = ColorManager.CreateColor(C.FrameColor); 
            _textColor       = ColorManager.CreateColor(C.TextColor); 
            _propertyGrid.SelectedObject = this;
		}
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ModuleName     ));
            NotifyPropertyChanged(nameof(Type           ));
            NotifyPropertyChanged(nameof(FontSize       ));
            NotifyPropertyChanged(nameof(IsFrameVisible ));
            NotifyPropertyChanged(nameof(FrameThickness ));
            NotifyPropertyChanged(nameof(FrameColor     ));
            NotifyPropertyChanged(nameof(BackgroundColor));
            NotifyPropertyChanged(nameof(TextColor      ));
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
			_plugin.UpdateLayout();
            DialogResult = true;
            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            _config.CopyPropertiesFrom(_configBackup);
            DialogResult = false;
            Close();
        }

		private void Button_ModuleSettings_Click(object sender, RoutedEventArgs e)
		{
			var wnd = new EditModuleSettings(_plugin, _texts);
			wnd.Owner = this;
			var result = wnd.ShowDialog();
			if (result == true)
			{
				_plugin.UpdateLayout();
				DialogResult = true;
                Close();
			}
		}

		private void Button_Delete_Click(object sender, RoutedEventArgs e)
		{
            DialogResult = true;
            DeleteModule = true;
            Close();
		}

        private void _propertyGrid_SelectedPropertyItemChanged(object sender, RoutedPropertyChangedEventArgs<Xceed.Wpf.Toolkit.PropertyGrid.PropertyItemBase> e)
        {
            _plugin.UpdateLayout();
        }
        #endregion
    }
}
