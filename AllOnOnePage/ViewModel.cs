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
        private OnSaveConfiguration_Handler _SaveConfiguration;
        #endregion



        #region ------------- Fields --------------------------------------------------------------
		private Configuration               _configuration;
		private HelpTexts                   _texts;
		private PluginLoader                _pluginManager;
		private List<Processor>             _processors;
        private List<RuntimeModule>         _runtimeModules;
        private PropertyChangedEventHandler _propertyChanged;
		private MainWindow                  _parentWindow;
		private ApplicationDirectories      _applicationDirectories;

		#region Visual editor
		private Grid                 _ParentGrid;
        private Canvas               _Canvas;
        private Rectangle            _DragRect;
        private const int            _DragRectStroke = 4;
        private const int            _BorderSnapPixels = 20;
        private bool                 _MousePointerOnRightBorder;
        private bool                 _MousePointerOnBottomBorder;
		private bool                 _EditMode;
        private bool                 _ChangeModulePosition;
        private bool                 _ChangeModuleWidth;
        private bool                 _ChangeModuleHeight;
        private Thickness            _InitialModulePositionAndSize;
        private Point                _InitialMouse;
        private IPlugin              _CurrentModule;
        private int                  _Delta_to_Subtract_from_Window_width = 16;
		private bool                 _MouseIsOverWastebasket;
		#endregion
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public ViewModel(MainWindow parentWindow, Configuration configuration, HelpTexts texts, ApplicationDirectories applicationDirectories)
        {
            _parentWindow           = parentWindow           ?? throw new ArgumentNullException(nameof(parentWindow)); 
            _configuration          = configuration          ?? throw new ArgumentNullException(nameof(configuration)); 
            _texts                  = texts                  ?? throw new ArgumentNullException(nameof(texts)); 
            _applicationDirectories = applicationDirectories ?? throw new ArgumentNullException(nameof(applicationDirectories)); 

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
                config.ApplicationDirectories = _applicationDirectories;
                var runtime = new RuntimeModule(config);
                Init_one_module(runtime);
                _runtimeModules.Add(runtime);
            }

            CreateDragRectInvisible();
        }

		internal void Init_module(RuntimeModule moduleDef)
		{
            Init_one_module(moduleDef);
		}

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

		public void Time()
        {
            foreach (var module in _runtimeModules)
                module.Plugin.Time();
        }

        public void Update_all_modules()
        {
			foreach (var module in _runtimeModules)
				Update_one_module(module);
		}

		private void Update_one_module(RuntimeModule module)
		{
            try
			{
    			module.Plugin.UpdateContent();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
                ((ModBase)module.Plugin).Value = "???";
			}
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

		/// <summary>
		/// A module informs us that is has become visible or invisible.
		/// We inform all other modules who overlap with this and can hide.
		/// </summary>
		private void VisibilityStateChange(IPlugin plugin, Visibility visibility)
        {
            var ourPos = plugin.GetPositionAndSize();

            foreach(var otherModule in _runtimeModules)
            {
                if (ModulesAreNotTheSame(otherModule.Plugin, plugin))
                {
                    var otherPos = otherModule.Plugin.GetPositionAndSize();
                    if (ModulesOverlap(ourPos, otherPos))
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

        private bool ModulesOverlap(Thickness a, Thickness b)
        {
            return PointIsInsideRectangle(a.Left        , a.Top         , b) ||
                   PointIsInsideRectangle(a.Left+a.Right, a.Top         , b) ||
                   PointIsInsideRectangle(a.Left        , a.Top+a.Bottom, b) ||
                   PointIsInsideRectangle(a.Left+a.Right, a.Top+a.Bottom, b) ||
                   PointIsInsideRectangle(b.Left        , b.Top         , a) ||
                   PointIsInsideRectangle(b.Left+a.Right, b.Top         , a) ||
                   PointIsInsideRectangle(b.Left        , b.Top+a.Bottom, a) ||
                   PointIsInsideRectangle(b.Left+a.Right, b.Top+a.Bottom, a);
        }

        private bool PointIsInsideRectangle(double x, double y, Thickness rect)
        {
            return rect.Left <= x && x <= rect.Right && 
                   rect.Top  <= y && y <= rect.Bottom;
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        #region ------------- Module editor -----------------------------------
        #region ------------- Edit modules ------------------------------------

        public bool Button_Edit_Click()
		{
			return EnterOrleaveEditMode();
		}

		private bool EnterOrleaveEditMode()
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
				ResetBackgroundForAllModules();
				HideDragRectangle();
				Update_all_modules();
				VisibilityStateChange_CompleteUpdate();
                SaveConfiguration();
			}

			return _EditMode;
		}

		public void Window_MouseLeftButtonDown(Window sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_EditMode) 
                return;
            if (_runtimeModules == null) 
                return;
            var module = FindModuleUnderMouse(sender, e);
            if (module == null)
                return;

            _InitialModulePositionAndSize = module.Plugin.GetPositionAndSize();
            _InitialMouse = e.GetPosition(sender);

            UpdateDragRectangle(module.Plugin.GetPositionAndSize());

            if (_MousePointerOnRightBorder)
                _ChangeModuleWidth = true;
            else if (_MousePointerOnBottomBorder)
                _ChangeModuleHeight = true;
            else
                _ChangeModulePosition = true;
        }

        public void Window_MouseLeftButtonUp(Window sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_EditMode) 
                return;

            var module = FindModuleUnderMouse(sender, e);
            if (module != null)
            {
                if (_ChangeModuleHeight || _ChangeModuleWidth || _ChangeModulePosition)
                {
                    SaveConfiguration();
                }
            }

            _ChangeModulePosition = false;
            _ChangeModuleWidth = false;
            _ChangeModuleHeight = false;

            if (_MouseIsOverWastebasket)
            {
                if (Ask_if_user_wants_to_delete())
				{
                    DeleteModule(module);
				}
            }
        }

		private bool Ask_if_user_wants_to_delete()
		{
			var result = MessageBox.Show(_texts[HelpTexts.ID.DELETE_QUESTION], 
                _texts[HelpTexts.ID.DELETE_TITLE], MessageBoxButton.YesNo);
            return result == MessageBoxResult.Yes;
		}

		private void DeleteModule(RuntimeModule module)
		{
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

		public void Window_MouseMove(Window sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_EditMode)
                return;
            if (_runtimeModules == null)
                return;

            Highlight_Wastebasket_if_mouse_pointer_is_over(e.GetPosition(sender));

            if (_CurrentModule != null && (_ChangeModuleHeight || _ChangeModuleWidth || _ChangeModulePosition))
            {
                ElementIsUnderMouse(sender, e, _CurrentModule);
            }
            else
            {
                var module = FindModuleUnderMouse(sender, e);
                if (module == null)
                    NoElementIsUnderMouse();
                else
                    ElementIsUnderMouse(sender, e, module.Plugin);
            }
        }

        public void MouseDoubleClick(Window sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var module = FindModuleUnderMouse(sender, e);
            if (module != null)
            {
                if (!_EditMode)
                    EnterOrleaveEditMode();
                OpenEditDialog(e, module);
			}
            else
			{
                if (_EditMode)
                    EnterOrleaveEditMode();
                else
                    OpenBackgroundEditDialog();
			}
        }

		private void OpenEditDialog(MouseButtonEventArgs e, RuntimeModule module)
		{
			var wnd = new EditModule(module.Plugin, _parentWindow, _texts);
			wnd.Owner = _parentWindow;
			wnd.Left = e.GetPosition(_parentWindow).X + _parentWindow.Left;
			wnd.Top = e.GetPosition(_parentWindow).Y;
			var result = wnd.ShowDialog();
			if (result == true)
			{
                if (wnd.DeleteModule)
				{
                    if (Ask_if_user_wants_to_delete())
				    {
                        DeleteModule(module);
				    }
				}
                else
				{
                    module.Plugin.Recreate();
				    module.Plugin.UpdateLayout();
				    SaveConfiguration();
				}
			}
		}

		private void OpenBackgroundEditDialog()
		{
            var wnd = new EditBackground(_parentWindow, _configuration);
            wnd.Owner = _parentWindow;
            wnd.ShowDialog();
            if (wnd.DialogResult == true)
                SaveConfiguration();
		}
		#endregion
		#region ------------- Implementation ----------------------------------
		private void NoElementIsUnderMouse()
        {
            ResetMousePointerOnBorderOfDragRect();
            ResetModuleUnderMouse();
        }

        private void ElementIsUnderMouse(Window sender, MouseEventArgs e, IPlugin plugin)
        {
            var mouse = e.GetPosition(sender);
            var pos = plugin.GetPositionAndSize();
            UpdateDragRectangle(pos);
            Thickness Deltas = CalculateMouseDeltas(mouse, pos);
            DisplayEditorStats(plugin, pos, Deltas);

            if (plugin != _CurrentModule)
                SwitchToNextModule(plugin);

            if (_ChangeModulePosition)
                PerformMove(mouse);
            else if (_ChangeModuleWidth)
                PerformWidthAdjustment(mouse);
            else if (_ChangeModuleHeight)
                PerformHeightAdjustment(mouse);
            else
                DetectMousePointerOnBorder(Deltas);
        }

        private void SwitchToNextModule(IPlugin module)
        {
            if (_CurrentModule != null)
                ResetModuleUnderMouse();
            else
                SetModuleUnderMouse(module);
        }

        private void PerformMove(Point mouse)
        {
            if (_CurrentModule != null)
            {
                var x = _InitialModulePositionAndSize.Left + mouse.X - _InitialMouse.X;
                var y = _InitialModulePositionAndSize.Top  + mouse.Y - _InitialMouse.Y;
                _CurrentModule.SetPosition(x, y);
            }
            ResetMousePointerOnBorderOfDragRect();
        }

        private void PerformWidthAdjustment(Point mouse)
        {
            if (_CurrentModule != null)
            {
                _CurrentModule.SetSize(_InitialModulePositionAndSize.Right  + mouse.X - _InitialMouse.X,
                                       _InitialModulePositionAndSize.Bottom);
            }
        }

        private void PerformHeightAdjustment(Point mouse)
        {
            var width = _InitialModulePositionAndSize.Right;
            var height = _InitialModulePositionAndSize.Bottom + mouse.Y - _InitialMouse.Y;
            if (width > 0 && height > 0)
                _CurrentModule.SetSize(width, height);
        }

        private void DisplayEditorStats(IPlugin plugin, Thickness pos, Thickness Deltas)
        {
            EditControlName = $"{plugin.GetName()}  {pos.Left}, {pos.Top}, {pos.Right}, {pos.Bottom}";
            //EditControlName += $"   {Deltas.Left},{Deltas.Top},{Deltas.Right},{Deltas.Bottom}";
            NotifyPropertyChanged(nameof(EditControlName));
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
            if (_CurrentModule != null)
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
            foreach (var module in _runtimeModules)
            {
                module.Plugin.SwitchEditMode(true);
            }
        }

        private void ResetBackgroundForAllModules()
        {
            foreach (var module in _runtimeModules)
                module.Plugin.SwitchEditMode(false);
        }

        private void DetectMousePointerOnBorder(Thickness deltas)
        {
            if (deltas.Top    < _BorderSnapPixels ||
                deltas.Bottom < _BorderSnapPixels ||
                deltas.Left   < _BorderSnapPixels ||
                deltas.Right  < _BorderSnapPixels)
                SetMousePointerOnBorderOfDragRect();
            else
                ResetMousePointerOnBorderOfDragRect();

            _MousePointerOnRightBorder  = (deltas.Right  < _BorderSnapPixels);
            _MousePointerOnBottomBorder = (deltas.Bottom < _BorderSnapPixels);
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

        private RuntimeModule FindModuleUnderMouse(Window sender, System.Windows.Input.MouseEventArgs e)
        {
            var pos = e.GetPosition(sender);

            IInputElement element = sender.InputHitTest(pos);
            if (element != null && _runtimeModules != null)
            {
                string name = (element is TextBlock) ? ((element as TextBlock).Name + (element as TextBlock).Text) : "";
                //System.Diagnostics.Debug.WriteLine($"FindModuleUnderMouse: {module.ToString()}  Name: {name}");
                foreach (var module in _runtimeModules)
                {
                    if (module.Plugin.HitTest(element))
                        return module;
                }
            }

            return null;
        }

		private void Highlight_Wastebasket_if_mouse_pointer_is_over(Point pos)
		{
            var basket = _parentWindow.Button_Wastebasket;
            var L = _parentWindow.Width 
                    -_Delta_to_Subtract_from_Window_width 
                    - basket.Margin.Right 
                    - basket.ActualWidth;
            var R = L+basket.ActualWidth;
            var T = basket.Margin.Top;
            var B = T+basket.ActualHeight;
			
            //System.Diagnostics.Debug.WriteLine($"basket: {L} {R} {T} {B}  Mouse: {pos.X} {pos.Y}");

            if (L <= pos.X && pos.X <= R && T <= pos.Y && pos.Y <= B)
			{
                _parentWindow.Button_Wastebasket.Opacity = 1.0;
                _MouseIsOverWastebasket = true;
			}
            else
			{
                _parentWindow.Button_Wastebasket.Opacity = 0.5;
                _MouseIsOverWastebasket = false;
			}
		}

		internal void Button_Editmode_Click()
		{
			EnterOrleaveEditMode();
		}

		internal void Button_Wastebasket_Click()
		{
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
			var wnd = new NewModule(_applicationDirectories);
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
                        EnterOrleaveEditMode();
				}
			}
		}

		private int GenerateUniqueID()
		{
            int maxID = _runtimeModules.Select(x => x.Config.ID).Max();
            return maxID+1;
		}
		#endregion
		#endregion

		#endregion
	}
}
