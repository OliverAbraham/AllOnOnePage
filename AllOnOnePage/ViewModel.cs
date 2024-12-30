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

            public HighLight()
            {
                Elements = new List<UIElement>();
            }
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
		private Grid                 _ParentGrid;
        private Canvas               _Canvas;
        private Rectangle            _DragRect;
        private const int            _DragRectStroke = 4;
        private const int            _BorderSnapPixels = 10;
        private bool                 _MouseOnTopEdge;
        private bool                 _MouseOnLeftEdge;
        private bool                 _MouseOnRightEdge;
        private bool                 _MouseOnBottomEdge;
        private bool                 _MouseOnCorner1;
        private bool                 _MouseOnCorner2;
        private bool                 _MouseOnCorner3;
        private bool                 _MouseOnCorner4;
		private bool                 _EditMode;
        private bool                 _ChangeModulePosition;
        private bool                 _ChangeModuleWidthLeft;
        private bool                 _ChangeModuleWidthRight;
        private bool                 _ChangeModuleHeightTop;
        private bool                 _ChangeModuleHeightBottom;
        private bool                 _ChangeModuleSizeTopLeft;
        private bool                 _ChangeModuleSizeTopRight;
        private bool                 _ChangeModuleSizeBottomRight;
        private bool                 _ChangeModuleSizeBottomLeft;
        private Thickness            _InitialPosAndSize;
        private Point                _InitialMouse;
        private IPlugin              _CurrentModule;
        private int                  _Delta_to_Subtract_from_Window_width = 16;
		private bool                 _MouseIsOverWastebasket;
        private HighLight?           _HoveredModule;
        private HighLight?           _SelectedModule;
        private HighLight?           _Ruler;
        private const int            _mouseMoveEventThreshold = 200;
        private Timer                _perimeterTimer;
        private const int            _removePerimeterAfter = 60;
        private int                  _mouseMoveEventCounter = 0;
        private Timer                _mouseMoveDetectorTimer;
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
            _ParentGrid    = parentGrid;
            _Canvas        = canvas;
            
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
			    moduleDef.Plugin.Init(moduleDef.Config, _ParentGrid, Dispatcher);
			    moduleDef.Plugin.UpdateLayout();
				return;
			}

			var newProcessor = _pluginManager.InstantiateProcessor(processor);
            if (newProcessor.Instance != null)
			{
                moduleDef.Plugin = (IPlugin)newProcessor.Instance;
			    moduleDef.Plugin.Init(moduleDef.Config, _ParentGrid, Dispatcher);
			    moduleDef.Plugin.UpdateLayout();
			}
            else
			{
                moduleDef.Plugin = new ModDummy();
			    moduleDef.Plugin.Init(moduleDef.Config, _ParentGrid, Dispatcher);
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
                ChangeModule(sender, e, _CurrentModule);
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

            _EditMode = true;
            _InitialPosAndSize = module.Plugin.GetPositionAndWidth();
            _InitialMouse = e.GetPosition(sender);

            UpdateDragRectangle(module.Plugin.GetPositionAndWidth());

            if      (_MouseOnCorner1)     _ChangeModuleSizeTopLeft     = true;
            else if (_MouseOnCorner2)     _ChangeModuleSizeTopRight    = true;
            else if (_MouseOnCorner3)     _ChangeModuleSizeBottomLeft  = true;
            else if (_MouseOnCorner4)     _ChangeModuleSizeBottomRight = true;
            else if (_MouseOnLeftEdge)    _ChangeModuleWidthLeft       = true;
            else if (_MouseOnRightEdge)   _ChangeModuleWidthRight      = true;
            else if (_MouseOnTopEdge)     _ChangeModuleHeightTop       = true;
            else if (_MouseOnBottomEdge)  _ChangeModuleHeightBottom    = true;

            SetMouseCursorShape();
            _ChangeModulePosition = true;
            SelectModuleUnderMouse();
        }

        public void Window_MouseLeftButtonUp(Window sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_EditMode)
                return;

            var module = FindModuleUnderMouse(sender, e);
            if (module != null)
            {
                if (AnySizeChangeIsInProgress())
                    SaveConfiguration();
            }

            UpdateModuleSelectionIndicator();

            ClearAllSizeChangers();

            if (_MouseIsOverWastebasket)
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
                if (!_EditMode)
                    EnterOrLeaveEditMode();
                OpenEditDialog(e, module);
			}
            else
			{
                if (_EditMode)
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
			_EditMode = !_EditMode;
			EditModeControlsVisibility = _EditMode ? Visibility.Visible : Visibility.Hidden;
			NotifyPropertyChanged(nameof(EditModeControlsVisibility));

			if (_EditMode)
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

			return _EditMode;
		}

        private void ClearAllSizeChangers()
        {
            _ChangeModulePosition        = false;
            _ChangeModuleWidthLeft       = false;
            _ChangeModuleWidthRight      = false;
            _ChangeModuleHeightTop       = false;
            _ChangeModuleHeightBottom    = false;
            _ChangeModuleSizeTopLeft     = false;
            _ChangeModuleSizeTopRight    = false;
            _ChangeModuleSizeBottomRight = false;
            _ChangeModuleSizeBottomLeft  = false;
        }

        private bool AnySizeChangeIsInProgress()
        {
            return 
                _ChangeModulePosition        ||
                _ChangeModuleWidthLeft       ||
                _ChangeModuleWidthRight      ||
                _ChangeModuleHeightTop       ||
                _ChangeModuleHeightBottom    ||
                _ChangeModuleSizeTopLeft     ||
                _ChangeModuleSizeTopRight    ||
                _ChangeModuleSizeBottomRight ||
                _ChangeModuleSizeBottomLeft;
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
                if (size.Left - _BorderSnapPixels <= pos.X && pos.X <= size.Right  + _BorderSnapPixels &&
                    size.Top  - _BorderSnapPixels <= pos.Y && pos.Y <= size.Bottom + _BorderSnapPixels)
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
            _CurrentModule = plugin;
            SendMouseMove();
        }

        private void ResetModuleUnderMouse()
        {
            if (AModuleIsSelected())
            {
                ResetEditModeMouseOver();
                _CurrentModule = null;
            }
        }

        private void SendMouseMove()
        {
            _CurrentModule.MouseMove(true);
        }

        private void ResetEditModeMouseOver()
        {
            _CurrentModule.MouseMove(false);
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
            _DragRect.Stroke = Brushes.LightGreen; //Brushes.Red;
        }

        private void ResetMousePointerOnBorderOfDragRect()
        {
            _DragRect.Stroke = Brushes.LightGreen;
            _DragRect.Visibility = Visibility.Hidden;
        }

        private void CreateDragRectInvisible()
        {
            _DragRect = new Rectangle
            {
                Stroke = Brushes.Transparent,
                StrokeThickness = _DragRectStroke
            };
            Canvas.SetLeft(_DragRect, 50);
            Canvas.SetTop(_DragRect, 50);
            _Canvas.Children.Add(_DragRect);
        }

        private void UpdateDragRectangle(Thickness pos)
        {
            Canvas.SetLeft(_DragRect, (int)pos.Left - _DragRectStroke);
            Canvas.SetTop (_DragRect, (int)pos.Top  - _DragRectStroke);
            _DragRect.Width  = (int)pos.Right  + 2*_DragRectStroke;
            _DragRect.Height = (int)pos.Bottom + 2*_DragRectStroke;
        }

        private void HideDragRectangle()
        {
            _DragRect.Stroke = Brushes.Transparent;
        }

		private void HighlightWastebasketIfMousePointerIsOver(Point pos)
		{
   //         var basket = _parentWindow.Button_Wastebasket;
   //         var L = _parentWindow.Width 
   //                 -_Delta_to_Subtract_from_Window_width 
   //                 - basket.Margin.Right 
   //                 - basket.ActualWidth;
   //         var R = L+basket.ActualWidth;
   //         var T = basket.Margin.Top;
   //         var B = T+basket.ActualHeight;

   //         if (L <= pos.X && pos.X <= R && T <= pos.Y && pos.Y <= B)
			//{
   //             _parentWindow.Button_Wastebasket.Opacity = 1.0;
   //             _MouseIsOverWastebasket = true;
			//}
   //         else
			//{
   //             _parentWindow.Button_Wastebasket.Opacity = 0.5;
   //             _MouseIsOverWastebasket = false;
			//}
		}

        private void ChangeModule(Window sender, MouseEventArgs e, IPlugin plugin)
        {
            var mouse = e.GetPosition(sender);
            var pos = plugin.GetPositionAndWidth();
            UpdateDragRectangle(pos);

            if (plugin != _CurrentModule)
                SwitchToNextModule(plugin);

            if (_CurrentModule is not null)
                DisplayModuleHoverIndicator(mouse);

            var updateSelectionIndicators = true;
            if      (_ChangeModuleSizeTopLeft    ) ChangeWidthGrabbedTopLeft(mouse);
            else if (_ChangeModuleSizeTopRight   ) ChangeWidthGrabbedTopRight(mouse);
            else if (_ChangeModuleSizeBottomRight) ChangeWidthGrabbedBottomRight(mouse);
            else if (_ChangeModuleSizeBottomLeft ) ChangeWidthGrabbedBottomLeft(mouse);
            else if (_ChangeModuleSizeTopLeft    ) ChangeWidthGrabbedLeft(mouse);
            else if (_ChangeModuleSizeTopRight   ) ChangeWidthGrabbedRight(mouse);
            else if (_ChangeModuleWidthLeft      ) ChangeWidthGrabbedLeft(mouse);
            else if (_ChangeModuleWidthRight     ) ChangeWidthGrabbedRight(mouse);
            else if (_ChangeModuleHeightTop      ) ChangeHeightGrabbedTop(mouse);
            else if (_ChangeModuleHeightBottom   ) ChangeHeightGrabbedBottom(mouse);
            else if (_ChangeModulePosition       ) MoveModule(mouse);
            else                                   updateSelectionIndicators = false;

            SetMouseCursorShape();

            if (updateSelectionIndicators)
                UpdateModuleSelectionIndicator();
        }

        private void SetMouseCursorShape()
        {
            if      (_MouseOnBottomEdge || _MouseOnTopEdge)   _parentWindow.Cursor = Cursors.SizeNS;
            else if (_MouseOnLeftEdge   || _MouseOnRightEdge) _parentWindow.Cursor = Cursors.SizeWE;
            else if (_MouseOnCorner1    || _MouseOnCorner4)   _parentWindow.Cursor = Cursors.SizeNWSE;
            else if (_MouseOnCorner2    || _MouseOnCorner3)   _parentWindow.Cursor = Cursors.SizeNESW;
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
                var x = _InitialPosAndSize.Left + mouse.X - _InitialMouse.X;
                var y = _InitialPosAndSize.Top  + mouse.Y - _InitialMouse.Y;
                _CurrentModule.SetPosition(x, y);
                //DisplayRulerIfWeAlignToAnotherModule(mouse);
            }
            ResetMousePointerOnBorderOfDragRect();
        }

        private bool AModuleIsSelected()
        {
            return _CurrentModule != null;
        }

        private void ChangeWidthGrabbedLeft(Point mouse)
        {
            var x = _InitialPosAndSize.Left + mouse.X - _InitialMouse.X;
            var y = _InitialPosAndSize.Top  + mouse.Y - _InitialMouse.Y;
            var dx = _InitialMouse.X - mouse.X;
            _CurrentModule.SetSize(_InitialPosAndSize.Right  + dx, _InitialPosAndSize.Bottom);
            _CurrentModule.SetPosition(x, y);
        }

        private void ChangeHeightGrabbedTop(Point mouse)
        {
            var width = _InitialPosAndSize.Right;
            var height = _InitialPosAndSize.Bottom + mouse.Y - _InitialMouse.Y;
            var x = _InitialPosAndSize.Left + mouse.X - _InitialMouse.X;
            var y = _InitialPosAndSize.Top + mouse.Y - _InitialMouse.Y;
            var dy = _InitialMouse.Y - mouse.Y;
            if (width > 0 && height > 0)
            {
                _CurrentModule.SetSize(width, _InitialPosAndSize.Bottom + dy);
                _CurrentModule.SetPosition(x, y);
            }
        }

        private void ChangeWidthGrabbedRight(Point mouse)
        {
            _CurrentModule.SetSize(_InitialPosAndSize.Right  + mouse.X - _InitialMouse.X,
                                    _InitialPosAndSize.Bottom);
        }

        private void ChangeHeightGrabbedBottom(Point mouse)
        {
            var width = _InitialPosAndSize.Right;
            var height = _InitialPosAndSize.Bottom + mouse.Y - _InitialMouse.Y;
            if (width > 0 && height > 0)
            {
                _CurrentModule.SetSize(width, height);
            }
        }

        private void ChangeWidthGrabbedTopLeft(Point mouse)
        {
            var x = _InitialPosAndSize.Left + mouse.X - _InitialMouse.X;
            var y = _InitialPosAndSize.Top  + mouse.Y - _InitialMouse.Y;
            var dx = _InitialMouse.X - mouse.X;
            var dy = _InitialMouse.Y - mouse.Y;
            _CurrentModule.SetSize(_InitialPosAndSize.Right  + dx, _InitialPosAndSize.Bottom + dy);
            _CurrentModule.SetPosition(x, y);
        }

        private void ChangeWidthGrabbedTopRight(Point mouse)
        {
            var x = _InitialPosAndSize.Left;
            var y = _InitialPosAndSize.Top  + mouse.Y - _InitialMouse.Y;
            var dx = mouse.X - _InitialMouse.X;
            var dy = _InitialMouse.Y - mouse.Y;
            _CurrentModule.SetSize(_InitialPosAndSize.Right  + dx, _InitialPosAndSize.Bottom + dy);
            _CurrentModule.SetPosition(x, y);
        }

        private void ChangeWidthGrabbedBottomLeft(Point mouse)
        {
            var x = _InitialPosAndSize.Left + mouse.X - _InitialMouse.X;
            var y = _InitialPosAndSize.Top;
            var width = _InitialPosAndSize.Right;
            var height = _InitialPosAndSize.Bottom + mouse.Y - _InitialMouse.Y;
            var dx = _InitialMouse.X - mouse.X;
            var dy = mouse.Y - _InitialMouse.Y;
            if (width > 0 && height > 0)
            {
                _CurrentModule.SetSize(width + dx, height + dy);
                _CurrentModule.SetPosition(x, y);
            }
        }

        private void ChangeWidthGrabbedBottomRight(Point mouse)
        {
            var width = _InitialPosAndSize.Right;
            var height = _InitialPosAndSize.Bottom + mouse.Y - _InitialMouse.Y;
            var dx = mouse.X - _InitialMouse.X;
            if (width > 0 && height > 0)
            {
                _CurrentModule.SetSize(width + dx, height);
            }
        }
        #endregion
        #region ------------- Automatic ruler ---------------------------------
        //private void DisplayRulerIfWeAlignToAnotherModule(Point ourPosition)
        //{
        //    var allOtherModulesExceptUs = _runtimeModules.Where(m => m.Plugin != _CurrentModule);
        //
        //    foreach (var module in allOtherModulesExceptUs)
        //    {
        //        var anotherModule = module.Plugin.GetPositionAndCorrectSize();
        //        //if (size.Left - _BorderSnapPixels <= pos.X && pos.X <= size.Right  + _BorderSnapPixels &&
        //        //    size.Top  - _BorderSnapPixels <= pos.Y && pos.Y <= size.Bottom + _BorderSnapPixels)
        //        //    return module;
        //
        //        var weAlignToThisModule = ourPosition.Y == anotherModule.Top;
        //        
        //        if (weAlignToThisModule)
        //        {
        //            if (_Ruler is null)
        //                CreateRuler(ourPosition, anotherModule);
        //            else
        //                UpdateRuler();
        //        }
        //
        //    }
        //}
        //
        //private void CreateRuler(Point ourPosition, Thickness anotherModule)
        //{
        //    var thickness = 1;
        //    var strokeColor = Brushes.Yellow;
        //    var dashed = true;
        //
        //    UIElement e = new Line
        //    { 
        //        Stroke          = strokeColor, 
        //        StrokeThickness = thickness, 
        //        X1              = 0, 
        //        X2              = ourPosition.X - anotherModule.Left, 
        //        Y1              = 0, 
        //        Y2              = 0 
        //    };
        //    Canvas.SetLeft(e, ourPosition.X);
        //    Canvas.SetTop(e, ourPosition.Y);
        //    if (dashed)
        //        (e as Line).StrokeDashArray = new DoubleCollection { 2 };
        //    
        //    _Canvas.Children.Add(e);
        //}
        //
        //private void UpdateRuler()
        //{
        //}
        #endregion
        #region ------------- Module highlight and module select --------------

        private void SelectModuleUnderMouse()
        {
            if (_CurrentModule is null)
                return;
            RemovePerimeterRectangle(ref _SelectedModule);
            CreatePerimeterRectangle(ref _SelectedModule, null, false);
        }

        private void RemovePerimeterRectangles()
        {
            RemovePerimeterRectangle(ref _SelectedModule);
            RemovePerimeterRectangle(ref _HoveredModule);
        }

        private void DisplayModuleHoverIndicator(Point mouse)
        {
            RemovePerimeterRectangle(ref _HoveredModule);
            CreatePerimeterRectangle(ref _HoveredModule, mouse, true);
        }

        private void RemoveModuleHoverIndicator()
        {
            RemovePerimeterRectangle(ref _HoveredModule);
        }

        private void RemoveModuleSelectionIndicator()
        {
            RemovePerimeterRectangle(ref _SelectedModule);
        }

        private void UpdateModuleSelectionIndicator()
        {
            RemovePerimeterRectangle(ref _SelectedModule);
            CreatePerimeterRectangle(ref _SelectedModule, null, false);
        }

        private void CreatePerimeterRectangle(ref HighLight shape, Point? mouse, bool dashed)
        {
            if (_CurrentModule is null)
                return;

            var thickness = 2;
            var strokeColor = Brushes.LightGreen;
            var hoverColor = Brushes.OrangeRed;
            var fillColor = Brushes.White;

            var p = _CurrentModule.GetPositionAndCorrectSize();
            shape = new HighLight();

            CreateCorder(shape, thickness, strokeColor, hoverColor, fillColor, p.Left  - 5, p.Left  + 7, p.Top    - 3, p.Top    + 7, mouse, ref _MouseOnCorner1);
            CreateCorder(shape, thickness, strokeColor, hoverColor, fillColor, p.Right - 7, p.Right + 5, p.Top    - 5, p.Top    + 7, mouse, ref _MouseOnCorner2);
            CreateCorder(shape, thickness, strokeColor, hoverColor, fillColor, p.Left  - 5, p.Left  + 7, p.Bottom - 7, p.Bottom + 5, mouse, ref _MouseOnCorner3);
            CreateCorder(shape, thickness, strokeColor, hoverColor, fillColor, p.Right - 7, p.Right + 5, p.Bottom - 7, p.Bottom + 5, mouse, ref _MouseOnCorner4);

            var mouseOnAnyCorner = _MouseOnCorner1 || _MouseOnCorner2 || _MouseOnCorner3 || _MouseOnCorner4;

            CreateEdge  (shape, dashed, thickness, strokeColor, hoverColor,    p.Left  + 7, p.Top       , p.Right  - 7, p.Top       , mouse, ref _MouseOnTopEdge   , mouseOnAnyCorner);
            CreateEdge  (shape, dashed, thickness, strokeColor, hoverColor,    p.Left     , p.Top    + 7, p.Left      , p.Bottom - 7, mouse, ref _MouseOnLeftEdge  , mouseOnAnyCorner);
            CreateEdge  (shape, dashed, thickness, strokeColor, hoverColor,    p.Right    , p.Top    + 7, p.Right     , p.Bottom - 7, mouse, ref _MouseOnRightEdge , mouseOnAnyCorner);
            CreateEdge  (shape, dashed, thickness, strokeColor, hoverColor,    p.Left  + 7, p.Bottom    , p.Right  - 7, p.Bottom    , mouse, ref _MouseOnBottomEdge, mouseOnAnyCorner);

            CreateGridTextBlock(shape, strokeColor, 12, _CurrentModule.GetName(), p.Left+ 7, p.Top + 7);

            foreach (var element in shape.Elements)
                _Canvas.Children.Add(element);

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
                    _Canvas.Children.Remove(element);
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
            System.Diagnostics.Debug.WriteLine("StartPerimeterTimer");
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
            System.Diagnostics.Debug.WriteLine("RemovePerimeterIndicator");
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
			        newModule.Plugin.Init(newConfig, _ParentGrid, Dispatcher);
                    newModule.Plugin.CreateSeedData();
                    _runtimeModules.Add(newModule);
					_configuration.Modules.Add(newModule.Config);
					SaveConfiguration();
                    if (!_EditMode)
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
                newModule.Plugin.Init(newConfig, _ParentGrid, Dispatcher);
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
