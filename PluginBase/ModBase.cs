using System;
using System.ComponentModel;
using System.Windows.Shapes;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections.Generic;
using System.Reflection;
using PluginBase;
using System.Threading.Tasks;
using System.Linq;

namespace AllOnOnePage.Plugins
{
    public abstract class ModBase: IPlugin, INotifyPropertyChanged
    {
        #region ------------- Properties ----------------------------------------------------------
        public Brush        Background          { get; set; }
		public Rectangle    Frame               { get; set; }
		public Thickness    TextBlockMargin     { get; set; }
        public double       Width               { get; set; }
        public double       Height              { get; set; }
        public string       Value               { get; set; }
        public Visibility   ValueVisibility     { get; set; }
        public string       Name                { get { return _config?.ModuleName; } set { } }
        public Visibility   NameVisibility      { get; set; }
        #endregion



        #region ------------- Fields --------------------------------------------------------------
        protected ModuleConfig                        _config;
        protected Grid                                _Parent;
        protected System.Windows.Threading.Dispatcher _Dispatcher { get; set; }
        protected string                              _fontName = "Yu Gothic UI Light";
        protected FontFamily                          _fontFamily;
        protected TextBlock                           _ValueControl;
        protected TextBlock                           _NameControl;
        protected Timer                               _FadeOutTimer;
        protected bool                                _ControlIsFadedOut;
        protected bool                                _EditModeOn;
		protected SolidColorBrush                     _backgroundBrush;
		protected SolidColorBrush                     _frameBrush;
		protected SolidColorBrush                     _textColor;
		protected SolidColorBrush                     _editModeBackgroundColor;
		protected SolidColorBrush                     _editModeBackgroundColorMouseOver;
		protected Canvas                              _canvas;
        protected double                              _fontRelativeCapsHeight = 1.0;
        protected double                              _fontRelativeLineSpacing = 1.0;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public ModBase()
		{
			TextBlockMargin = new Thickness(0);
			ValueVisibility = Visibility.Visible;
			NameVisibility = Visibility.Hidden;
			Create_FadeOut_Timer();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public virtual void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
        {
            _config     = config     ?? throw new ArgumentNullException(nameof(config    ));
            _Parent     = parent     ?? throw new ArgumentNullException(nameof(parent    ));
            _Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

			CreateBackground();
            SetPositionAndSize();
            CreateValueControl();
            CreateNameControl();
        }

		public virtual void CreateSeedData()
		{
		}

		public virtual void Stop()
		{
		}

		public virtual async Task<(bool,string)> Validate()
		{
            return (true, "");
		}

		public virtual async Task<(bool success, string messages)> Test()
		{
            return (false, "Dieses Modul bietet keinen Test");
		}

		public virtual async Task Save()
		{
		}

        public void Clone(ModuleConfig existingModuleConfig)
        {
            _config = existingModuleConfig.Clone();
        }

        public virtual void Delete()
		{
            _canvas.Children.Remove(_ValueControl);
            _canvas.Children.Remove(_NameControl);
            _canvas.Children.Remove(Frame);
            _Parent.Children.Remove(_canvas);
            _canvas = null;
		}

        public virtual void UpdateLayout()
        {
            Delete();
            CreateBackground();
            SetPositionAndSize();
            CreateValueControl();
            CreateNameControl();
        }

        public virtual void Recreate()
        {
            Delete();
			CreateBackground();
            SetPositionAndSize();
            CreateValueControl();
            CreateNameControl();
        }

        public virtual void Time()
        {
        }

        public virtual void UpdateContent(ServerDataObjectChange? dataObject)
        {
        }

        public virtual bool HitTest(object module, Point mousePosition)
        {
            return module == this.Frame || 
                   module == this._ValueControl || 
                   module == this._NameControl;
        }

        public virtual string GetName() => _config.ModuleName;

        public virtual ModuleConfig GetModuleConfig() => _config;

        public virtual Thickness GetPositionAndWidth()
        {
            return new Thickness(_config.X, _config.Y, _config.W, _config.H);
        }

        public virtual Thickness GetPositionAndCorrectSize()
        {
            return new Thickness(_config.X, _config.Y, _config.X + _config.W, _config.Y + _config.H);
        }
        
        public virtual void SetPosition(double left, double top)
        {
            _config.X = (int)left;
            _config.Y = (int)top;
            SetPositionAndSize();
        }
		
        public virtual void SetSize(double width, double height)
        { 
            _config.W = (int)width;
            _config.H = (int)height;
            SetPositionAndSize();
        }
		
        public virtual void SwitchEditMode(bool on)
        {
        }

		public virtual void MouseMove(bool mouseOver)
		{
            Frame.Fill = mouseOver ? _editModeBackgroundColorMouseOver : _backgroundBrush;
            NotifyPropertyChanged(nameof(Background));
		}

        public virtual Visibility GetVisibility()
        {
            return ValueVisibility;
        }

		public virtual ModuleSpecificConfig GetModuleSpecificConfig()
		{
            return new ModuleSpecificConfig();
		}

		public virtual void CleanupModuleSpecificConfig()
		{
		}

		public virtual Dictionary<string,string> GetHelp()
		{
            return new Dictionary<string,string>();
		}

        public virtual void LoadAssembly(string filename)
		{
            Assembly.LoadFrom(_config.ApplicationData.PluginDirectory + System.IO.Path.DirectorySeparatorChar + filename);
		}

        public bool PointIsInsideRectangle(Point p, Thickness rect)
        {
            return PointIsInsideRectangle(p.X, p.Y, rect);
        }

        public bool PointIsInsideRectangle(double x, double y, Thickness rect)
        {
            return rect.Left <= x && x <= rect.Right && 
                   rect.Top  <= y && y <= rect.Bottom;
        }
        #endregion



        #region ------------- Implementation Controls ---------------------------------------------

		protected void CreateBackground()
		{
			_backgroundBrush                  = ColorManager.CreateBrush(_config.BackgroundColor);
			_frameBrush                       = ColorManager.CreateBrush(_config.FrameColor);
			_textColor                        = ColorManager.CreateBrush(_config.TextColor);
			_editModeBackgroundColor          = Brushes.DarkGray;
			_editModeBackgroundColorMouseOver = _backgroundBrush;

			_canvas = new Canvas();
			_Parent.Children.Add(_canvas);

			Background = Brushes.Transparent;

			Frame = new Rectangle()
			{
				Margin           = new Thickness(_config.X, _config.Y, 0, 0),
				Width            = _config.W,
				Height           = _config.H,
				Stroke           = _config.IsFrameVisible ? _frameBrush : Brushes.Transparent,
				StrokeThickness  = _config.IsFrameVisible ? _config.FrameThickness : 0,
				RadiusX          = _config.FrameRadius,
				RadiusY          = _config.FrameRadius,
				Fill             = _backgroundBrush,
				IsHitTestVisible = true,
			};
			_canvas.Children.Add(Frame);




			TextBlockMargin = new Thickness(0);
			NotifyPropertyChanged(nameof(TextBlockMargin));

			Width = _config.W;
			NotifyPropertyChanged(nameof(Width));

			Height = _config.H;
			NotifyPropertyChanged(nameof(Height));
		}

        protected void SetPositionAndSize()
        {
            Frame.Margin = new Thickness(_config.X, _config.Y, 0, 0);
            Frame.Width = _config.W;
            Frame.Height = _config.H;

            TextBlockMargin = new Thickness(_config.X, _config.Y - _config.FontSize * _fontRelativeCapsHeight/2, 0, 0);
            NotifyPropertyChanged(nameof(TextBlockMargin));
            
            Width = _config.W;
            NotifyPropertyChanged(nameof(Width));
            
            Height = _config.H * _fontRelativeLineSpacing;
            NotifyPropertyChanged(nameof(Height));
        }

        protected void CreateValueControl()
        {
            _fontFamily = new System.Windows.Media.FontFamily(_fontName);
            _fontRelativeCapsHeight  = (_fontFamily.FamilyTypefaces.Any()) ? _fontFamily.FamilyTypefaces[0].CapsHeight : 1.0;
            _fontRelativeLineSpacing = _fontFamily.LineSpacing;

            _ValueControl = CreateTextBlock(_textColor, _config.FontSize);
            _canvas.Children.Add(_ValueControl);
            _ValueControl.FontSize = _config.FontSize; 
            _ValueControl.FontStretch = FontStretch.FromOpenTypeStretch(5);
            CreatePropertyBinding(nameof(Value)          , _ValueControl, TextBlock.TextProperty);
            CreatePropertyBinding(nameof(ValueVisibility), _ValueControl, TextBlock.VisibilityProperty);
            NotifyPropertyChanged(nameof(Value));
        }

        protected void CreateNameControl()
        {
            _NameControl = CreateTextBlock(_textColor, 12);
            _canvas.Children.Add(_NameControl);
            CreatePropertyBinding(nameof(Name)           , _NameControl , TextBlock.TextProperty);
            CreatePropertyBinding(nameof(NameVisibility) , _NameControl , TextBlock.VisibilityProperty);
            Panel.SetZIndex(_NameControl, -1);
        }

        protected TextBlock CreateTextBlock(Brush foreground, int fontSize)
        {
            var control                 = new TextBlock();
            control.Padding             = new Thickness(_config.TextPadding);
            control.Foreground          = foreground;
            control.FontSize            = fontSize;
            control.FontFamily          = new System.Windows.Media.FontFamily(_fontName);
            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment   = VerticalAlignment.Top;
            CreatePropertyBinding(nameof(TextBlockMargin), control , TextBlock.MarginProperty);
            CreatePropertyBinding(nameof(Width)          , control , TextBlock.WidthProperty);
            CreatePropertyBinding(nameof(Height)         , control , TextBlock.HeightProperty);
            CreatePropertyBinding(nameof(Background)     , control , TextBlock.BackgroundProperty);
            return control;
        }

        protected void CreatePropertyBinding(string propertyPath, UIElement control, DependencyProperty dp)
        {
            Binding myBinding = CreateOneWayBinding(propertyPath, this);
            BindingOperations.SetBinding(control, dp, myBinding);
        }

        protected Binding CreateOneWayBinding(string propertyPath, object source)
        {
            return CreateBinding(propertyPath, source, BindingMode.OneWay);
        }

        protected Binding CreateTwoWayBinding(string propertyPath, object source)
        {
            return CreateBinding(propertyPath, source, BindingMode.TwoWay);
        }

        protected Binding CreateBinding(string propertyPath, object source, BindingMode bindingMode)
        {
            Binding myBinding = new Binding();
            myBinding.Source = source;
            myBinding.Path = new PropertyPath(propertyPath);
            myBinding.Mode = bindingMode;
            myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            return myBinding;
        }

        private void EnsureControlTextIsVisible()
        {
            if (_ValueControl.Opacity != 1.0)
                Cancel_FadeOut();
            if (ValueVisibility != Visibility.Visible)
            {
                ValueVisibility = Visibility.Visible;
                NotifyPropertyChanged(nameof(ValueVisibility));
            }
            if (string.IsNullOrWhiteSpace(Value))
            {
                Value = "---------------------------------------";
                NotifyPropertyChanged(nameof(Value));
            }
        }

        #endregion



        #region ------------- Implementation Animations -------------------------------------------

        protected void SetValueControlVisible()
        {
            _ValueControl.Opacity = 1.0;
            ValueVisibility = Visibility.Visible;
            NotifyPropertyChanged(nameof(ValueVisibility));
        }

        /// <summary>
        /// We are getting informed that a different module is 
        /// overlapping us and has just changed his visibility state
        /// </summary>
        public virtual void OverlapEvent(Visibility visibilityOfOtherModule)
        {
            if (GetModuleConfig().DismissIfOverlapped)
            {
                if (visibilityOfOtherModule == Visibility.Visible &&
                    ValueVisibility == Visibility.Visible)
                    Start_FadeOut(0);
                else if (ValueVisibility != Visibility.Visible)
                    FadeIn();
            }
        }

        protected void SetNameControlVisible()
        {
            _NameControl.Opacity = 1.0;
            NameVisibility = Visibility.Visible;
            NotifyPropertyChanged(nameof(NameVisibility));
        }

        protected void Create_FadeOut_Timer()
        {
            _FadeOutTimer = new Timer(1);
            _FadeOutTimer.AutoReset = false;
            _FadeOutTimer.Elapsed += _FadeOutTimer_Elapsed;
        }

        protected void Start_FadeOut(int minutes)
        {
            if (_ControlIsFadedOut)
                return;
            _ControlIsFadedOut = true;

            _FadeOutTimer.Interval = (minutes > 0) ? (minutes * 60 * 1000) : 100;
            _FadeOutTimer.Start();
        }

        protected void Cancel_FadeOut()
        {
            _ControlIsFadedOut = false;
            _FadeOutTimer.Stop();
            FadeInModule();
        }

        protected void _FadeOutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _Dispatcher.Invoke(() =>
            {
                FadeOutModule();

                // inform all other modules, maybe one can become visible again
                if (!_EditModeOn)
                {
                    ValueVisibility = Visibility.Hidden;
                }
            });
        }

        protected void FadeIn()
        {
            if (!_ControlIsFadedOut)
                return;
            _ControlIsFadedOut = false;
            _FadeOutTimer.Stop();
            FadeInModule();
        }

        protected virtual void FadeInModule()
        {
            WpfAnimations.FadeInImmediatelyTextBlock(_ValueControl);
        }

        protected virtual void FadeOutModule()
        {
            WpfAnimations.FadeOutTextBlock(_ValueControl);
        }

		#endregion



        #region ------------- INotifyPropertyChanged ----------------------------------------------
        [NonSerialized]
        private PropertyChangedEventHandler _PropertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _PropertyChanged += value;
            }
            remove
            {
                _PropertyChanged -= value;
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler Handler = _PropertyChanged; // avoid race condition
            if (Handler != null)
                Handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
