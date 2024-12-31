using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Threading;
using AllOnOnePage.DialogWindows;
using Abraham.PluginManager;
using AllOnOnePage.Plugins;
using AllOnOnePage.Libs;
using PluginBase;
using Abraham.WPFWindowLayoutManager;
using System.Timers;
using System.Diagnostics;
using System.Reflection;

namespace AllOnOnePage
{
    class ViewModel : INotifyPropertyChanged
	{
        #region ------------- Properties ----------------------------------------------------------
        public Visibility       EditModeControlsVisibility    { get; private set; }
        public string           EditControlName               { get; set; }
        public string           ErrorMessage                  { get; set; }
        public Visibility       ErrorMessageVisibility        { get; set; }
        public Dispatcher       Dispatcher                    { get; set; }
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



        #region ------------- Callback properties -------------------------------------------------
        public delegate void OnSaveConfiguration_Handler();
        public OnSaveConfiguration_Handler SaveConfiguration
        {
            get
            {
                return _SaveConfiguration;
            }
            set
            {
                if (value != null)
                    _SaveConfiguration = value;
                else
                    _SaveConfiguration = delegate () { };  // Null object pattern
            }
        }

        public WindowLayoutManager LayoutManager { get; internal set; }

        private OnSaveConfiguration_Handler _SaveConfiguration;
        #endregion



        #region ------------- Private classes -----------------------------------------------------
        private class HighLight
        {
            public List<UIElement> Elements;
            public Align Align;
            public Point SnapPoint;

            public HighLight()
            {
                Elements = new List<UIElement>();
                SnapPoint = new Point();
            }
        }

        private enum Align
        {
            Top,
            Left
        }
        #endregion



        #region ------------- Fields --------------------------------------------------------------
        private Configuration                _configuration;
		private HelpTexts                    _texts;
		private PluginLoader                 _pluginManager;
		private List<Processor>              _processors;
        private List<RuntimeModule>          _runtimeModules;
        private PropertyChangedEventHandler  _propertyChanged;
		private MainWindow                   _parentWindow;
		private ApplicationData              _applicationData;

		#region Visual editor
		private Grid                 _parentGrid;
        private Canvas               _canvas;
        private Rectangle            _dragRect;
        private const int            _dragRectStroke = 4;
        private const int            _borderSnapPixels = 5;
        private bool                 _mouseOnTopEdge;
        private bool                 _mouseOnLeftEdge;
        private bool                 _mouseOnRightEdge;
        private bool                 _mouseOnBottomEdge;
        private bool                 _mouseOnCorner1;
        private bool                 _mouseOnCorner2;
        private bool                 _mouseOnCorner3;
        private bool                 _mouseOnCorner4;
		private bool                 _editMode;
        private bool                 _changeModulePosition;
        private bool                 _changeModuleWidthLeft;
        private bool                 _changeModuleWidthRight;
        private bool                 _changeModuleHeightTop;
        private bool                 _changeModuleHeightBottom;
        private bool                 _changeModuleSizeTopLeft;
        private bool                 _changeModuleSizeTopRight;
        private bool                 _changeModuleSizeBottomRight;
        private bool                 _changeModuleSizeBottomLeft;
        private Thickness            _initialPosAndSize;
        private Point                _initialMouse;
        private IPlugin              _currentModule;
        private int                  _delta_to_Subtract_from_Window_width = 16;
		private bool                 _mouseIsOverWastebasket;
        private HighLight?           _hoveredModule;
        private HighLight?           _selectedModule;
        private const int            _mouseMoveEventThreshold = 100;
        private Timer                _perimeterTimer;
        private const int            _removePerimeterAfter = 60;
        private int                  _mouseMoveEventCounter = 0;
        private Timer                _mouseMoveDetectorTimer;

        private bool                 _enableRuler      = true;
        private bool                 _snapToRuler      = true;
        private int                  _rulerSnapPixels  = 20;
        private double               _rulerThickness   = 1;
        private SolidColorBrush      _rulerStrokeColor = Brushes.Yellow;
        private bool                 _rulerDashedLine  = false;
        private HighLight?           _ruler;
        #endregion
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public ViewModel(MainWindow parentWindow, Configuration configuration, HelpTexts texts, ApplicationData applicationData)
        {
            _parentWindow    = parentWindow    ?? throw new ArgumentNullException(nameof(parentWindow)); 
            _configuration   = configuration   ?? throw new ArgumentNullException(nameof(configuration)); 
            _texts           = texts           ?? throw new ArgumentNullException(nameof(texts)); 
            _applicationData = applicationData ?? throw new ArgumentNullException(nameof(applicationData)); 

            EditModeControlsVisibility = Visibility.Hidden;
        }
        #endregion



        #region ------------- Methods -------------------------------------------------------------

        public void DisplayHardError(string message)
        {
            ErrorMessage = message;
            ErrorMessageVisibility = Visibility.Visible;
            NotifyPropertyChanged(nameof(ErrorMessage));
            NotifyPropertyChanged(nameof(ErrorMessageVisibility));
        }

        public void Init_all_modules(PluginLoader pluginManager, List<Processor> processors, Grid parentGrid, Canvas canvas)
        {
            _pluginManager = pluginManager;
            _processors    = processors;
            _parentGrid    = parentGrid;
            _canvas        = canvas;
            
            _runtimeModules = new List<RuntimeModule>();
            foreach (var config in _configuration.Modules)
            {
                config.ApplicationData = _applicationData;
                var runtime = new RuntimeModule(config);
                Init_one_module(runtime);
                _runtimeModules.Add(runtime);
            }

            CreateDragRectInvisible();
            StartMouseMoveDetectorTimer();
        }

        public void Stop_all_modules(PluginLoader pluginLoader, List<Processor> processors, Grid mainGrid, Canvas canvas)
        {
            try
            {
			    foreach (var module in _runtimeModules)
    			    module.Plugin.Stop();
                StopMouseMoveDetectorTimer();
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

		internal void Init_module(RuntimeModule moduleDef)
		{
            Init_one_module(moduleDef);
		}

        public bool Button_Edit_Click()
		{
			return EnterOrLeaveEditMode();
		}

		public void VisibilityStateChange_CompleteUpdate()
        {
            foreach(var module in _runtimeModules)
            {
                if (module.Plugin.GetVisibility() != Visibility.Visible)
                {
                    VisibilityStateChange(module.Plugin, Visibility.Hidden);
                }
            }
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        #region ------------- Basic -------------------------------------------
        private void Init_one_module(RuntimeModule moduleDef)
        {
            try
			{
                Init_one_module_internal(moduleDef);
			}
            catch (Exception ex)
			{
                MessageBox.Show(ex.ToString());
                var wnd = new MessageBoxWindow(_parentWindow);
                wnd.ContentBox.Text = ex.ToString();
                wnd.Title = _texts[HelpTexts.ID.ERROR_HEADING];
			    wnd.ShowDialog();
			}
        }

        private void Init_one_module_internal(RuntimeModule moduleDef)
		{
			Processor processor = FindProcessorByType(moduleDef.Config.TileType);
			if (processor == null)
			{
                moduleDef.Plugin = new ModDummy();
			    moduleDef.Plugin.Init(moduleDef.Config, _parentGrid, Dispatcher);
			    moduleDef.Plugin.UpdateLayout();
				return;
			}

			var newProcessor = _pluginManager.InstantiateProcessor(processor);
            if (newProcessor.Instance != null)
			{
                moduleDef.Plugin = (IPlugin)newProcessor.Instance;
			    moduleDef.Plugin.Init(moduleDef.Config, _parentGrid, Dispatcher);
			    moduleDef.Plugin.UpdateLayout();
			}
            else
			{
                moduleDef.Plugin = new ModDummy();
			    moduleDef.Plugin.Init(moduleDef.Config, _parentGrid, Dispatcher);
			    moduleDef.Plugin.UpdateLayout();
			}
		}

		private Processor FindProcessorByType(string type)
		{
			return _processors.Where(x => x.Type.Name == type).FirstOrDefault();
		}

		/// <summary>
		/// A module informs us that is has become visible or invisible.
		/// We inform all other modules who overlap with this and can hide.
		/// </summary>
		private void VisibilityStateChange(IPlugin plugin, Visibility visibility)
        {
            var ourPos = plugin.GetPositionAndWidth();

            foreach(var otherModule in _runtimeModules)
            {
                if (ModulesAreNotTheSame(otherModule.Plugin, plugin))
                {
                    var otherPos = otherModule.Plugin.GetPositionAndWidth();
                    if (TwoRectanglesOverlap(ourPos, otherPos))
                    {
                        otherModule.Plugin.OverlapEvent(visibility);
                    }
                }
            }
        }

        private bool ModulesAreNotTheSame(IPlugin a, IPlugin b)
        {
            return a.GetHashCode() != b.GetHashCode();
        }
        #endregion
        #region ------------- Module editor -----------------------------------
        #region ------------- Mouse events ------------------------------------
        #region ------------- Methods -----------------------------------------
		public void Window_MouseMove(Window sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!EnoughMoveMovementsDetected(e))
                return;

            if (_runtimeModules == null)
                return;

            HighlightWastebasketIfMousePointerIsOver(e.GetPosition(sender));

            if (AModuleIsSelected() && AnySizeChangeIsInProgress())
            {
                ChangeModule(sender, e, _currentModule);
            }
            else
            {
                var module = FindModuleUnderMouse(sender, e);
                if (module == null)
                    ChangeModuleEnd();
                else
                    ChangeModule(sender, e, module.Plugin);
            }
        }

        public void Window_MouseLeftButtonDown(Window sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_runtimeModules is null) 
                return;
            var module = FindModuleUnderMouse(sender, e);
            if (module == null)
                return;

            _editMode = true;
            _initialPosAndSize = module.Plugin.GetPositionAndWidth();
            _initialMouse = e.GetPosition(sender);

            UpdateDragRectangle(module.Plugin.GetPositionAndWidth());

            if      (_mouseOnCorner1)     _changeModuleSizeTopLeft     = true;
            else if (_mouseOnCorner2)     _changeModuleSizeTopRight    = true;
            else if (_mouseOnCorner3)     _changeModuleSizeBottomLeft  = true;
            else if (_mouseOnCorner4)     _changeModuleSizeBottomRight = true;
            else if (_mouseOnLeftEdge)    _changeModuleWidthLeft       = true;
            else if (_mouseOnRightEdge)   _changeModuleWidthRight      = true;
            else if (_mouseOnTopEdge)     _changeModuleHeightTop       = true;
            else if (_mouseOnBottomEdge)  _changeModuleHeightBottom    = true;

            SetMouseCursorShape();
            _changeModulePosition = true;
            SelectModuleUnderMouse();
        }

        public void Window_MouseLeftButtonUp(Window sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_editMode)
                return;

            var module = FindModuleUnderMouse(sender, e);
            if (module != null)
            {
                if (AnySizeChangeIsInProgress())
                {
                    SnapToRulerTheLastTime();
                    SaveConfiguration();
                }
            }

            UpdateModuleSelectionIndicator();

            ClearAllSizeChangers();

            if (_mouseIsOverWastebasket)
            {
                if (AskIfUserWantsToDelete())
                {
                    DeleteModule(module);
                }
            }
        }

        public void MouseDoubleClick(Window sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var module = FindModuleUnderMouse(sender, e);
            if (module != null)
            {
                if (!_editMode)
                    EnterOrLeaveEditMode();
                OpenEditDialog(e, module);
			}
            else
			{
                if (_editMode)
                    EnterOrLeaveEditMode();
                else
                    OpenBackgroundEditDialog();
			}
        }

        public void MouseRightButtonDown(Window sender, MouseButtonEventArgs e)
        {
            //var module = FindModuleUnderMouse(sender, e);
            //if (module != null)
            //    DuplicateModule(module, e);
        }

		public void Button_Editmode_Click()
		{
			EnterOrLeaveEditMode();
		}

		public void Button_Wastebasket_Click()
		{
		}
        #endregion

        private bool EnterOrLeaveEditMode()
		{
			_editMode = !_editMode;
			EditModeControlsVisibility = _editMode ? Visibility.Visible : Visibility.Hidden;
			NotifyPropertyChanged(nameof(EditModeControlsVisibility));

			if (_editMode)
			{
                _configuration.EditModeWasEntered = true;
				SetBackgroundForAllModules();
			}
			else
			{
                RemovePerimeterRectangles();
				ResetBackgroundForAllModules();
				HideDragRectangle();
				Update_all_modules();
				VisibilityStateChange_CompleteUpdate();
                SaveConfiguration();
			}

			return _editMode;
		}

        private void ClearAllSizeChangers()
        {
            _changeModulePosition        = false;
            _changeModuleWidthLeft       = false;
            _changeModuleWidthRight      = false;
            _changeModuleHeightTop       = false;
            _changeModuleHeightBottom    = false;
            _changeModuleSizeTopLeft     = false;
            _changeModuleSizeTopRight    = false;
            _changeModuleSizeBottomRight = false;
            _changeModuleSizeBottomLeft  = false;
        }

        private bool AnySizeChangeIsInProgress()
        {
            return 
                _changeModulePosition        ||
                _changeModuleWidthLeft       ||
                _changeModuleWidthRight      ||
                _changeModuleHeightTop       ||
                _changeModuleHeightBottom    ||
                _changeModuleSizeTopLeft     ||
                _changeModuleSizeTopRight    ||
                _changeModuleSizeBottomRight ||
                _changeModuleSizeBottomLeft;
        }

        private bool AskIfUserWantsToDelete()
		{
			var result = MessageBox.Show(_texts[HelpTexts.ID.DELETE_QUESTION], 
                _texts[HelpTexts.ID.DELETE_TITLE], MessageBoxButton.YesNo);
            return result == MessageBoxResult.Yes;
		}

		private void DeleteModule(RuntimeModule module)
		{
            RemovePerimeterRectangles();

            module.Plugin.Delete();
            _runtimeModules.Remove(module);
			_parentWindow.InvalidateVisual();

            var idToDelete = module.Plugin.GetModuleConfig().ID;
            _configuration.Modules = (from c in _configuration.Modules 
                                      where c.ID != idToDelete
                                      select c).ToList();
            SaveConfiguration();
            module = null;
		}

        private RuntimeModule FindModuleUnderMouse(Window sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_runtimeModules is null)
                return null;

            var pos = e.GetPosition(sender);

            IInputElement element = sender.InputHitTest(pos);
            if (element is null)
                return null;

            foreach (var module in _runtimeModules)
            {
                var size = module.Plugin.GetPositionAndCorrectSize();
                if (size.Left - _borderSnapPixels <= pos.X && pos.X <= size.Right  + _borderSnapPixels &&
                    size.Top  - _borderSnapPixels <= pos.Y && pos.Y <= size.Bottom + _borderSnapPixels)
                    return module;
            }
            return null;
        }

        private void SwitchToNextModule(IPlugin module)
        {
            if (AModuleIsSelected())
                ResetModuleUnderMouse();
            else
                SetModuleUnderMouse(module);
        }

        private Thickness CalculateMouseDeltas(Point mouse, Thickness pos)
        {
            return new Thickness(
                Math.Abs(mouse.X - pos.Left),
                Math.Abs(mouse.Y - pos.Top),
                Math.Abs(mouse.X - pos.Right - pos.Left),
                Math.Abs(mouse.Y - pos.Bottom - pos.Top));
        }

        private void SetModuleUnderMouse(IPlugin plugin)
        {
            _currentModule = plugin;
            SendMouseMove();
        }

        private void ResetModuleUnderMouse()
        {
            if (AModuleIsSelected())
            {
                ResetEditModeMouseOver();
                _currentModule = null;
            }
        }

        private void SendMouseMove()
        {
            _currentModule.MouseMove(true);
        }

        private void ResetEditModeMouseOver()
        {
            _currentModule.MouseMove(false);
        }

        private void SetBackgroundForAllModules()
        {
            if (_runtimeModules is null) 
                return;
            foreach (var module in _runtimeModules)
                module.Plugin.SwitchEditMode(true);
        }

        private void ResetBackgroundForAllModules()
        {
            if (_runtimeModules is null) 
                return;
            foreach (var module in _runtimeModules)
                module.Plugin.SwitchEditMode(false);
        }

        private void SetMousePointerOnBorderOfDragRect()
        {
            //_DragRect.Visibility = Visibility.Visible;
            _dragRect.Stroke = Brushes.LightGreen; //Brushes.Red;
        }

        private void ResetMousePointerOnBorderOfDragRect()
        {
            _dragRect.Stroke = Brushes.LightGreen;
            _dragRect.Visibility = Visibility.Hidden;
        }

        private void CreateDragRectInvisible()
        {
            _dragRect = new Rectangle
            {
                Stroke = Brushes.Transparent,
                StrokeThickness = _dragRectStroke
            };
            Canvas.SetLeft(_dragRect, 50);
            Canvas.SetTop(_dragRect, 50);
            _canvas.Children.Add(_dragRect);
        }

        private void UpdateDragRectangle(Thickness pos)
        {
            Canvas.SetLeft(_dragRect, (int)pos.Left - _dragRectStroke);
            Canvas.SetTop (_dragRect, (int)pos.Top  - _dragRectStroke);
            _dragRect.Width  = (int)pos.Right  + 2*_dragRectStroke;
            _dragRect.Height = (int)pos.Bottom + 2*_dragRectStroke;
        }

        private void HideDragRectangle()
        {
            _dragRect.Stroke = Brushes.Transparent;
        }

		private void HighlightWastebasketIfMousePointerIsOver(Point pos)
		{
            // var basket = _parentWindow.Button_Wastebasket;
            // var L = _parentWindow.Width
            //         - _delta_to_Subtract_from_Window_width
            //         - basket.Margin.Right
            //         - basket.ActualWidth;
            // var R = L + basket.ActualWidth;
            // var T = basket.Margin.Top;
            // var B = T + basket.ActualHeight;
            // 
            // if (L <= pos.X && pos.X <= R && T <= pos.Y && pos.Y <= B)
            // {
            //     _parentWindow.Button_Wastebasket.Opacity = 1.0;
            //     _mouseIsOverWastebasket = true;
            // }
            // else
            // {
            //     _parentWindow.Button_Wastebasket.Opacity = 0.5;
            //     _mouseIsOverWastebasket = false;
            // }
        }

        private void ChangeModule(Window sender, MouseEventArgs e, IPlugin plugin)
        {
            var mouse = e.GetPosition(sender);
            var pos = plugin.GetPositionAndWidth();
            UpdateDragRectangle(pos);

            if (plugin != _currentModule)
                SwitchToNextModule(plugin);

            if (_currentModule is not null)
                UpdateModuleHoverIndicator(mouse);

            var updateSelectionIndicators = true;
            if      (_changeModuleSizeTopLeft    ) ChangeWidthGrabbedTopLeft(mouse);
            else if (_changeModuleSizeTopRight   ) ChangeWidthGrabbedTopRight(mouse);
            else if (_changeModuleSizeBottomRight) ChangeWidthGrabbedBottomRight(mouse);
            else if (_changeModuleSizeBottomLeft ) ChangeWidthGrabbedBottomLeft(mouse);
            else if (_changeModuleSizeTopLeft    ) ChangeWidthGrabbedLeft(mouse);
            else if (_changeModuleSizeTopRight   ) ChangeWidthGrabbedRight(mouse);
            else if (_changeModuleWidthLeft      ) ChangeWidthGrabbedLeft(mouse);
            else if (_changeModuleWidthRight     ) ChangeWidthGrabbedRight(mouse);
            else if (_changeModuleHeightTop      ) ChangeHeightGrabbedTop(mouse);
            else if (_changeModuleHeightBottom   ) ChangeHeightGrabbedBottom(mouse);
            else if (_changeModulePosition       ) MoveModule(mouse);
            else                                   updateSelectionIndicators = false;

            SetMouseCursorShape();

            if (updateSelectionIndicators)
                UpdateModuleSelectionIndicator();
        }

        private void SetMouseCursorShape()
        {
            if      (_mouseOnBottomEdge || _mouseOnTopEdge)   _parentWindow.Cursor = Cursors.SizeNS;
            else if (_mouseOnLeftEdge   || _mouseOnRightEdge) _parentWindow.Cursor = Cursors.SizeWE;
            else if (_mouseOnCorner1    || _mouseOnCorner4)   _parentWindow.Cursor = Cursors.SizeNWSE;
            else if (_mouseOnCorner2    || _mouseOnCorner3)   _parentWindow.Cursor = Cursors.SizeNESW;
            else                                              SetStandardMouseCursorShape();
        }

        private void SetStandardMouseCursorShape()
        {
            _parentWindow.Cursor = Cursors.Arrow;
        }

        private void ChangeModuleEnd()
        {
            ResetMousePointerOnBorderOfDragRect();
            ResetModuleUnderMouse();
            RemoveModuleHoverIndicator();
            SetStandardMouseCursorShape();
        }
        #endregion
        #region ------------- Move and resize objects -------------------------
        
        /// <summary>
        /// The user drags a module to a new position.
        /// </summary>
        private void MoveModule(Point mouse)
        {
            if (AModuleIsSelected())
            {
                DisplayRulerIfWeAlignToAnotherModuleAndSnapIn(mouse);
                var x = _initialPosAndSize.Left + mouse.X - _initialMouse.X;
                var y = _initialPosAndSize.Top  + mouse.Y - _initialMouse.Y;
                _currentModule.SetPosition(x, y);
                UpdateModuleSelectionIndicator();
                UpdateModuleHoverIndicator(mouse);
            }
            ResetMousePointerOnBorderOfDragRect();
        }

        /// <summary>
        /// When releasing the mouse button, often the mouse moves a bit.
        /// We snap onto the visible ruler one last time.
        /// </summary>
        private void SnapToRulerTheLastTime()
        {
            if (!_enableRuler || !_snapToRuler || _ruler is null)
                return;

            var mouse = _ruler.SnapPoint;

            var x = _initialPosAndSize.Left + mouse.X - _initialMouse.X;
            var y = _initialPosAndSize.Top  + mouse.Y - _initialMouse.Y;
            _currentModule.SetPosition(x, y);
            UpdateModuleSelectionIndicator();
            UpdateModuleHoverIndicator(mouse);

            DeleteRuler();
        }

        private bool AModuleIsSelected()
        {
            return _currentModule != null;
        }

        private void ChangeWidthGrabbedLeft(Point mouse)
        {
            var x = _initialPosAndSize.Left + mouse.X - _initialMouse.X;
            var y = _initialPosAndSize.Top  + mouse.Y - _initialMouse.Y;
            var dx = _initialMouse.X - mouse.X;
            _currentModule.SetSize(_initialPosAndSize.Right  + dx, _initialPosAndSize.Bottom);
            _currentModule.SetPosition(x, y);
        }

        private void ChangeHeightGrabbedTop(Point mouse)
        {
            var width = _initialPosAndSize.Right;
            var height = _initialPosAndSize.Bottom + mouse.Y - _initialMouse.Y;
            var x = _initialPosAndSize.Left + mouse.X - _initialMouse.X;
            var y = _initialPosAndSize.Top + mouse.Y - _initialMouse.Y;
            var dy = _initialMouse.Y - mouse.Y;
            if (width > 0 && height > 0)
            {
                _currentModule.SetSize(width, _initialPosAndSize.Bottom + dy);
                _currentModule.SetPosition(x, y);
            }
        }

        private void ChangeWidthGrabbedRight(Point mouse)
        {
            _currentModule.SetSize(_initialPosAndSize.Right  + mouse.X - _initialMouse.X,
                                    _initialPosAndSize.Bottom);
        }

        private void ChangeHeightGrabbedBottom(Point mouse)
        {
            var width = _initialPosAndSize.Right;
            var height = _initialPosAndSize.Bottom + mouse.Y - _initialMouse.Y;
            if (width > 0 && height > 0)
            {
                _currentModule.SetSize(width, height);
            }
        }

        private void ChangeWidthGrabbedTopLeft(Point mouse)
        {
            var x = _initialPosAndSize.Left + mouse.X - _initialMouse.X;
            var y = _initialPosAndSize.Top  + mouse.Y - _initialMouse.Y;
            var dx = _initialMouse.X - mouse.X;
            var dy = _initialMouse.Y - mouse.Y;
            _currentModule.SetSize(_initialPosAndSize.Right  + dx, _initialPosAndSize.Bottom + dy);
            _currentModule.SetPosition(x, y);
        }

        private void ChangeWidthGrabbedTopRight(Point mouse)
        {
            var x = _initialPosAndSize.Left;
            var y = _initialPosAndSize.Top  + mouse.Y - _initialMouse.Y;
            var dx = mouse.X - _initialMouse.X;
            var dy = _initialMouse.Y - mouse.Y;
            _currentModule.SetSize(_initialPosAndSize.Right  + dx, _initialPosAndSize.Bottom + dy);
            _currentModule.SetPosition(x, y);
        }

        private void ChangeWidthGrabbedBottomLeft(Point mouse)
        {
            var x = _initialPosAndSize.Left + mouse.X - _initialMouse.X;
            var y = _initialPosAndSize.Top;
            var width = _initialPosAndSize.Right;
            var height = _initialPosAndSize.Bottom + mouse.Y - _initialMouse.Y;
            var dx = _initialMouse.X - mouse.X;
            var dy = mouse.Y - _initialMouse.Y;
            if (width > 0 && height > 0)
            {
                _currentModule.SetSize(width + dx, height + dy);
                _currentModule.SetPosition(x, y);
            }
        }

        private void ChangeWidthGrabbedBottomRight(Point mouse)
        {
            var width = _initialPosAndSize.Right;
            var height = _initialPosAndSize.Bottom + mouse.Y - _initialMouse.Y;
            var dx = mouse.X - _initialMouse.X;
            if (width > 0 && height > 0)
            {
                _currentModule.SetSize(width + dx, height);
            }
        }
        #endregion
        #region ------------- Automatic ruler ---------------------------------
        private Point DisplayRulerIfWeAlignToAnotherModuleAndSnapIn(Point position)
        {
            if (!_enableRuler)
                return position;

            // Shift keys disable the ruler
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                return position;

            var ourModule = _runtimeModules.Where(m => m.Plugin == _currentModule).FirstOrDefault();
            var allOtherModulesExceptUs = _runtimeModules.Where(m => m.Plugin != _currentModule);

            foreach (var module in allOtherModulesExceptUs)
            {
                var us = ourModule.Plugin.GetPositionAndCorrectSize();
                var it = module.Plugin.GetPositionAndCorrectSize();

                var weAlignOnTop = PointsAreNear(us.Top, it.Top);
                if (weAlignOnTop)
                {
                    CreateOrUpdateRuler(us, it, Align.Top);
                    if (_snapToRuler)
                    {
                        var delta = Math.Floor(it.Top - us.Top);
                        if (delta != 0.0)
                        {
                            position.Y += delta;
                            _ruler.SnapPoint.X = position.X;
                            _ruler.SnapPoint.Y = position.Y;
                        }
                    }
                    return position;
                }

                var weAlignLeft = PointsAreNear(us.Left, it.Left);
                if (weAlignLeft)
                {
                    CreateOrUpdateRuler(us, it, Align.Left);
                    if (_snapToRuler)
                    {
                        var delta = Math.Floor(it.Left - us.Left);
                        if (delta != 0.0)
                        {
                            position.X += delta;
                            _ruler.SnapPoint.X = position.X;
                            _ruler.SnapPoint.Y = position.Y;
                        }
                    }
                    return position;
                }
            }

            RemoveRuler();

            if (_ruler is not null)
            {
                _ruler.SnapPoint.X = position.X;
                _ruler.SnapPoint.Y = position.Y;
            }
            return position;
        }

        private bool PointsAreNear(double p1, double p2)
        {
            return Math.Abs(p1 - p2) < _rulerSnapPixels;
        }

        private void CreateOrUpdateRuler(Thickness ourPosition, Thickness anotherModule, Align align)
        {
            Line line;
            if (_ruler is null)
            {
                _ruler = new HighLight();
                _ruler.Align = align;

                line = new Line { Stroke = _rulerStrokeColor, StrokeThickness = _rulerThickness };
                if (_rulerDashedLine)
                    line.StrokeDashArray = new DoubleCollection { 2 };

                _ruler.Elements.Add(line);
            }
            else
            {
                line = _ruler.Elements[0] as Line;
            }

            if (align == Align.Top)
            {
                line.X1 = Math.Min(ourPosition.Left, anotherModule.Left);
                line.X2 = Math.Max(ourPosition.Right, anotherModule.Right);
                line.Y1 = anotherModule.Top;
                line.Y2 = anotherModule.Top;
            }
            else if (align == Align.Left)
            {
                line.X1 = anotherModule.Left;
                line.X2 = anotherModule.Left;
                line.Y1 = Math.Min(ourPosition.Top, anotherModule.Top);
                line.Y2 = Math.Max(ourPosition.Bottom, anotherModule.Bottom);
            }

            Canvas.SetLeft(line, 0);
            Canvas.SetTop(line, 0);

            RemoveRuler();
            AddRuler();
        }

        private void AddRuler()
        {
            if (_ruler is not null)
                foreach(var element in _ruler.Elements)
                    _canvas.Children.Add(element);
        }

        private void RemoveRuler()
        {
            if (_ruler is not null)
                foreach(var element in _ruler.Elements)
                    _canvas.Children.Remove(element);
        }

        private void DeleteRuler()
        {
            RemoveRuler();
            _ruler = null;
        }
        #endregion
        #region ------------- Module highlight and module select --------------

        private void SelectModuleUnderMouse()
        {
            if (_currentModule is null)
                return;
            RemovePerimeterRectangle(ref _selectedModule);
            CreatePerimeterRectangle(ref _selectedModule, null, false);
        }

        private void RemovePerimeterRectangles()
        {
            RemovePerimeterRectangle(ref _selectedModule);
            RemovePerimeterRectangle(ref _hoveredModule);
        }

        private void UpdateModuleHoverIndicator(Point mouse)
        {
            RemovePerimeterRectangle(ref _hoveredModule);
            CreatePerimeterRectangle(ref _hoveredModule, mouse, true);
        }

        private void RemoveModuleHoverIndicator()
        {
            RemovePerimeterRectangle(ref _hoveredModule);
        }

        private void RemoveModuleSelectionIndicator()
        {
            RemovePerimeterRectangle(ref _selectedModule);
        }

        private void UpdateModuleSelectionIndicator()
        {
            RemovePerimeterRectangle(ref _selectedModule);
            CreatePerimeterRectangle(ref _selectedModule, null, false);
        }

        private void CreatePerimeterRectangle(ref HighLight shape, Point? mouse, bool dashed)
        {
            if (_currentModule is null)
                return;

            var thickness = 2;
            var strokeColor = Brushes.LightGreen;
            var hoverColor = Brushes.OrangeRed;
            var fillColor = Brushes.White;

            var p = _currentModule.GetPositionAndCorrectSize();
            shape = new HighLight();

            CreateCorder(shape, thickness, strokeColor, hoverColor, fillColor, p.Left  - 5, p.Left  + 7, p.Top    - 3, p.Top    + 7, mouse, ref _mouseOnCorner1);
            CreateCorder(shape, thickness, strokeColor, hoverColor, fillColor, p.Right - 7, p.Right + 5, p.Top    - 5, p.Top    + 7, mouse, ref _mouseOnCorner2);
            CreateCorder(shape, thickness, strokeColor, hoverColor, fillColor, p.Left  - 5, p.Left  + 7, p.Bottom - 7, p.Bottom + 5, mouse, ref _mouseOnCorner3);
            CreateCorder(shape, thickness, strokeColor, hoverColor, fillColor, p.Right - 7, p.Right + 5, p.Bottom - 7, p.Bottom + 5, mouse, ref _mouseOnCorner4);

            var mouseOnAnyCorner = _mouseOnCorner1 || _mouseOnCorner2 || _mouseOnCorner3 || _mouseOnCorner4;

            CreateEdge  (shape, dashed, thickness, strokeColor, hoverColor,    p.Left  + 7, p.Top       , p.Right  - 7, p.Top       , mouse, ref _mouseOnTopEdge   , mouseOnAnyCorner);
            CreateEdge  (shape, dashed, thickness, strokeColor, hoverColor,    p.Left     , p.Top    + 7, p.Left      , p.Bottom - 7, mouse, ref _mouseOnLeftEdge  , mouseOnAnyCorner);
            CreateEdge  (shape, dashed, thickness, strokeColor, hoverColor,    p.Right    , p.Top    + 7, p.Right     , p.Bottom - 7, mouse, ref _mouseOnRightEdge , mouseOnAnyCorner);
            CreateEdge  (shape, dashed, thickness, strokeColor, hoverColor,    p.Left  + 7, p.Bottom    , p.Right  - 7, p.Bottom    , mouse, ref _mouseOnBottomEdge, mouseOnAnyCorner);

            CreateGridTextBlock(shape, strokeColor, 12, _currentModule.GetName(), p.Left+ 7, p.Top + 7);

            foreach (var element in shape.Elements)
                _canvas.Children.Add(element);

            StartPerimeterTimer();
        }

        private void CreateEdges(HighLight shape, bool dashed, int thickness, SolidColorBrush strokeColor, double left, double top, double right, double bottom)
        {
            UIElement e = new Rectangle { Stroke = strokeColor, StrokeThickness = thickness, Width = right-left, Height = bottom-top };
            Canvas.SetLeft(e, left);
            Canvas.SetTop(e, top);
            if (dashed)
                (e as Rectangle).StrokeDashArray = new DoubleCollection { 2 };
            shape.Elements.Add(e);
        }

        private void CreateEdge(HighLight shape, bool dashed, int thickness, SolidColorBrush strokeColor, SolidColorBrush nearColor, double left, double top, double right, double bottom, Point? mouse, ref bool mouseHovering, bool mouseOnAnyCorner)
        {
            if (!mouseOnAnyCorner && 
                mouse is not null &&
                left-10 <= mouse.Value.X && mouse.Value.X <= right +10 && 
                top -10 <= mouse.Value.Y && mouse.Value.Y <= bottom+10)
            {
                strokeColor = nearColor;
                mouseHovering = true;
            }
            else
                mouseHovering = false;

            UIElement e = new Line { Stroke = strokeColor, StrokeThickness = thickness, X1=0, X2=right-left, Y1=0, Y2=bottom-top };
            Canvas.SetLeft(e, left);
            Canvas.SetTop(e, top);
            if (dashed)
                (e as Line).StrokeDashArray = new DoubleCollection { 2 };
            shape.Elements.Add(e);
        }

        private void CreateGridTextBlock(HighLight shape, Brush foreground, int fontSize, string text, double left, double top)
        {
            var control                 = new TextBlock();
            control.Foreground          = foreground;
            control.FontSize            = fontSize;
            control.FontFamily          = new System.Windows.Media.FontFamily("Yu Gothic UI Light");
            control.FontStretch         = FontStretch.FromOpenTypeStretch(3);
            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment   = VerticalAlignment.Top;
            control.Margin              = new Thickness(left, top, 0, 0);
            control.Text                = text;
            shape.Elements.Add(control);
        }

        private void CreateCorder(HighLight shape, int thickness, SolidColorBrush strokeColor, SolidColorBrush nearColor, SolidColorBrush fillColor, 
            double left, double right, double top, double bottom, Point? mouse, ref bool mouseHovering)
        {
            if (mouse is not null &&
                left <= mouse.Value.X && mouse.Value.X <= right && 
                top  <= mouse.Value.Y && mouse.Value.Y <= bottom)
            {
                strokeColor = nearColor;
                fillColor = nearColor;
                mouseHovering = true;
            }
            else
                mouseHovering = false;

            UIElement e = new Ellipse { Stroke = strokeColor, StrokeThickness = thickness, Fill = fillColor, Width = right-left, Height = bottom-top};
            Canvas.SetLeft(e, left);
            Canvas.SetTop(e, top);
            shape.Elements.Add(e);
        }

        private void RemovePerimeterRectangle(ref HighLight shape)
        {
            if (shape is not null)
            {
                foreach(var element in shape.Elements)
                    _canvas.Children.Remove(element);
                shape = null;
            }
        }

        /// <summary>
        /// Every time we display the perimeter rectangles, we also set a timer.
        /// It will remove the perimeter rectangles after some time.
        /// This is a safety net for production dashboards just in case somebody moves the mouse.
        /// (i.e. on the touchscreen)
        /// </summary>
        private void StartPerimeterTimer()
        {
            if (_perimeterTimer is null)
            {
                _perimeterTimer = new Timer();
                _perimeterTimer.Interval = _removePerimeterAfter * 1000;
                _perimeterTimer.Elapsed += RemovePerimeterIndicator;
                _perimeterTimer.AutoReset = false;
                _perimeterTimer.Start();
            }
            else
            {
                _perimeterTimer.Stop();
                _perimeterTimer.Start();
            }
        }

        private void RemovePerimeterIndicator(object? sender, ElapsedEventArgs e)
        {
            _perimeterTimer.Stop();
            Dispatcher.Invoke(() =>
            {
                RemoveModuleSelectionIndicator();
                RemoveModuleHoverIndicator();
                SetStandardMouseCursorShape();
            });
        }
        #endregion
        #region ------------- Graphics basics ---------------------------------
        private bool TwoRectanglesOverlap(Thickness a, Thickness b)
        {
            return PointIsInRectangle(a.Left        , a.Top         , b) ||
                   PointIsInRectangle(a.Left+a.Right, a.Top         , b) ||
                   PointIsInRectangle(a.Left        , a.Top+a.Bottom, b) ||
                   PointIsInRectangle(a.Left+a.Right, a.Top+a.Bottom, b) ||
                   PointIsInRectangle(b.Left        , b.Top         , a) ||
                   PointIsInRectangle(b.Left+a.Right, b.Top         , a) ||
                   PointIsInRectangle(b.Left        , b.Top+a.Bottom, a) ||
                   PointIsInRectangle(b.Left+a.Right, b.Top+a.Bottom, a);
        }

        private bool PointIsInRectangle(double x, double y, Thickness rect)
        {
            return rect.Left <= x && x <= rect.Right && 
                   rect.Top  <= y && y <= rect.Bottom;
        }
        #endregion
        #region ------------- Add new module ----------------------------------
        public void AddNewModule()
		{
            try
			{
			    AddnewModule_internal();
			}
            catch (Exception ex)
			{
                var wnd = new MessageBoxWindow(_parentWindow);
                wnd.ContentBox.Text = ex.ToString();
                wnd.Title = _texts[HelpTexts.ID.ERROR_HEADING];
			    wnd.ShowDialog();
			}
		}

		private void AddnewModule_internal()
		{
			var wnd = new NewModule(_applicationData);
            wnd.LayoutManager = LayoutManager;
			wnd.Owner = _parentWindow;
			wnd.Processors = _processors;
			wnd.ShowDialog();
			if (wnd.DialogResult == true)
			{
				Processor processor = FindProcessorByType(wnd.ModuleType);
				if (processor != null)
				{
                    var newConfig = wnd.Module.Config.Clone();
                    newConfig.ID = GenerateUniqueID();
					var newModule = new RuntimeModule(newConfig);
					var newProcessor = _pluginManager.InstantiateProcessor(processor);
                    newModule.Plugin = (IPlugin)newProcessor.Instance;
			        newModule.Plugin.Init(newConfig, _parentGrid, Dispatcher);
                    newModule.Plugin.CreateSeedData();
                    _runtimeModules.Add(newModule);
					_configuration.Modules.Add(newModule.Config);
					SaveConfiguration();
                    if (!_editMode)
                        EnterOrLeaveEditMode();
                    newModule.Plugin.UpdateContent(null);
				}
			}
		}

		private int GenerateUniqueID()
		{
            int maxID = _runtimeModules.Select(x => x.Config.ID).Max();
            return maxID+1;
		}
        #endregion
        #region ------------- Edit background dialog --------------------------
		private void OpenBackgroundEditDialog()
		{
            var wnd = new EditBackground(_parentWindow, _configuration);
            wnd.Owner = _parentWindow;
            wnd.LayoutManager = LayoutManager;
            wnd.ShowDialog();
            if (wnd.DialogResult == true)
                SaveConfiguration();
		}
        #endregion
        #region ------------- Edit module dialog ------------------------------
		private void OpenEditDialog(MouseButtonEventArgs e, RuntimeModule module)
		{
			var wnd = new EditModule(module.Plugin, _parentWindow, _texts, LayoutManager, Dispatcher);
			wnd.Owner = _parentWindow;
			wnd.Left = e.GetPosition(_parentWindow).X + _parentWindow.Left;
			wnd.Top = e.GetPosition(_parentWindow).Y;
			var result = wnd.ShowDialog();
			if (result == true)
			{
                if (wnd.DeleteModule)
				{
                    if (AskIfUserWantsToDelete())
				    {
                        DeleteModule(module);
				    }
				}
                else
				{
                    module.Plugin.Recreate();
				    module.Plugin.UpdateLayout();
				    module.Plugin.UpdateContent(null);
				    SaveConfiguration();
				}
			}
            else if (wnd.UserWantsToDuplicateTheModule)
            {
                DuplicateModule(module);
            }
		}

        #endregion
        #region ------------- Duplicate module --------------------------------
        private void DuplicateModule(RuntimeModule module)
        {
            Processor processor = FindProcessorByType(module.Config.TileType);
            if (processor != null)
            {
                var newConfig = module.Config.Clone();
                newConfig.ID = GenerateUniqueID();
                var newModule = new RuntimeModule(newConfig);

                // center the copy
                newModule.Config.X = ((int)_parentWindow.Width / 2 - newModule.Config.W / 2);
                newModule.Config.Y = ((int)_parentWindow.Height / 2 - newModule.Config.H / 2);

                var newProcessor = _pluginManager.InstantiateProcessor(processor);
                newModule.Plugin = (IPlugin)newProcessor.Instance;
                newModule.Plugin.Clone(module.Config);
                newModule.Plugin.Init(newConfig, _parentGrid, Dispatcher);
                newModule.Plugin.UpdateLayout();                
                Update_one_module(newModule, null);

                _runtimeModules.Add(newModule);
                _configuration.Modules.Add(newModule.Config);
                SaveConfiguration();
            }
        }
        #endregion
        #region ------------- Value updates -----------------------------------
		public void Time()
        {
            if (_runtimeModules is null) 
                return;
            foreach (var module in _runtimeModules)
                module.Plugin.Time();
        }

        public void Update_all_modules(ServerDataObjectChange? @do = null)
        {
            if (_runtimeModules is null) 
                return;
            
            var stopwatch = new Stopwatch();

			foreach (var module in _runtimeModules)
				Update_one_module(module, @do);

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 10)
                System.Diagnostics.Debug.WriteLine($"Update_all_modules took {stopwatch.ElapsedMilliseconds} ms");
		}

		private void Update_one_module(RuntimeModule module, ServerDataObjectChange? @do = null)
		{
            try
			{
    			module.Plugin.UpdateContent(@do);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
                ((ModBase)module.Plugin).Value = "???";
			}
		}
        #endregion
        #region ------------- Mouse movement detector -------------------------
        /// <summary>
        /// On a dashboard, nobody moves the mouse and the mouse pointer will most likely always be in the middle of the screen.
        /// My DashboardPowerManager (separate Project) sends mouse clicks to reactivate the screen 
        /// when the screen is off and a motion detector detects a person.
        /// 
        /// The problem is now that a mouse click will activate the highlight function of the module editor here,
        /// when there's a module in the middle of the screen.
        /// 
        /// To avoid this, the editor only gets activated after at least 200 mouse events have encountered.
        /// So the editor will only do sth when a real person is moving the mouse.
        /// </summary>
        private bool EnoughMoveMovementsDetected(MouseEventArgs e)
        {
            if (_mouseMoveEventCounter >= _mouseMoveEventThreshold)
                return true;
            _mouseMoveEventCounter++;
            return false;
        }

        private void StartMouseMoveDetectorTimer()
        {
            System.Diagnostics.Debug.WriteLine("StartMouseMoveDetectorTimer");
            if (_mouseMoveDetectorTimer is null)
            {
                _mouseMoveDetectorTimer = new Timer();
                _mouseMoveDetectorTimer.Interval = 1 * 1000;
                _mouseMoveDetectorTimer.Elapsed += MouseMoveDetectorReset;
                _mouseMoveDetectorTimer.Start();
            }
            else
            {
                _mouseMoveDetectorTimer.Stop();
                _mouseMoveDetectorTimer.Start();
            }
        }

        private void StopMouseMoveDetectorTimer()
        {
            System.Diagnostics.Debug.WriteLine("StopMouseMoveDetectorTimer");
            if (_mouseMoveDetectorTimer is not null)
                _mouseMoveDetectorTimer.Stop();
        }

        private void MouseMoveDetectorReset(object sender, ElapsedEventArgs e)
        {
            if (_mouseMoveEventCounter > 0) 
                _mouseMoveEventCounter--; 
        }

        #endregion
        #endregion
        #endregion
    }
}
