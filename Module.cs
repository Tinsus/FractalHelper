using Blish_HUD;
using Blish_HUD.ArcDps.Common;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Blish_HUD.Settings.UI.Views;
using Gw2Sharp.ChatLinks;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using static Blish_HUD.GameService;

namespace ff.FractalHelper
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        internal static Module Instance { get; private set; }
        #endregion

        #region Controls
        private CornerIcon _fractalHelperIcon;
        public ContextMenuStrip _fractalHelperContextMenuStrip;
        #endregion

        #region Settings
        public SettingCollection _packSettings;

        private SettingEntry<bool> settingNonSmartWiki;
        private SettingEntry<bool> settingIgnoreClass;
        private SettingEntry<bool> settingIgnorePortal;
        private SettingEntry<bool> settingHideFailureWarnings;
        private SettingEntry<bool> settingIgnorePosition;
        #endregion

        private string[] _filesToCopy = {
            "ui.loca",
        };

        #region localization
        private DirectoryReader _directoryReader;
        private JsonSerializerOptions _jsonOptions;
        private string _fractalHelperDirectory;
        public Dictionary<string, string> _loca;
        #endregion

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            Instance = this;
        }

        public string GetLoca(string key)
        {
            try
            {
                return _loca[key];
            }
            catch (Exception)
            {
                return key;
            }            
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            _loca = new Dictionary<string, string>();
           
            foreach (string s in _filesToCopy)
            {
                ExtractFile(s);
            }

            _fractalHelperDirectory = DirectoriesManager.GetFullDirectoryPath("fractalhelper");

            _directoryReader = new DirectoryReader(_fractalHelperDirectory);

            _jsonOptions = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                IgnoreNullValues = true
            };

            _directoryReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                ReadLoca(fileStream);
            }, ".loca");

            _packSettings = settings.AddSubCollection("pack");

            settingNonSmartWiki         = _packSettings.DefineSetting(nameof(settingNonSmartWiki),          false, GetLoca("setting1_title"), GetLoca("setting1_desc"));
            settingIgnoreClass          = _packSettings.DefineSetting(nameof(settingIgnoreClass),           false, GetLoca("setting2_title"), GetLoca("setting2_desc"));
            settingIgnorePosition       = _packSettings.DefineSetting(nameof(settingIgnorePosition),        false, GetLoca("setting3_title"), GetLoca("setting3_desc"));
            settingIgnorePortal         = _packSettings.DefineSetting(nameof(settingIgnorePortal),          false, GetLoca("setting4_title"), GetLoca("setting4_desc"));
            settingHideFailureWarnings  = _packSettings.DefineSetting(nameof(settingHideFailureWarnings),   false, GetLoca("setting5_title"), GetLoca("setting5_desc"));
        }

        protected override void Initialize()
        {
            _fractalHelperIcon = new CornerIcon()
            {
                IconName = GetLoca("title"),
                Icon = ContentsManager.GetTexture(@"logo_32.png"),
                Priority = 0
            };

            _fractalHelperContextMenuStrip = new ContextMenuStrip();

            var newWindow = new TabbedSettingWindow()
            {
                Title = GetLoca("title"),
                Parent = GameService.Graphics.SpriteScreen,
                Location = new Point(100, 100),
                Emblem = this.ContentsManager.GetTexture(@"logo_64.png")
            };

            newWindow.AddTab(new WindowTab(GetLoca("wiki"), ContentsManager.GetTexture(@"102497.png"), 1), () => new Blish_HUD.Settings.UI.Views.SettingsView(_packSettings));


            newWindow.AddTab(new WindowTab(GetLoca("level1"), ContentsManager.GetTexture(@"102497.png"), 1), () => new Blish_HUD.Settings.UI.Views.SettingsView(_packSettings));
            newWindow.AddTab(new WindowTab(GetLoca("level2"), ContentsManager.GetTexture(@"156027.png"), 2), () => new Blish_HUD.Settings.UI.Views.SettingsView(_packSettings));
            newWindow.AddTab(new WindowTab(GetLoca("level3"), ContentsManager.GetTexture(@"102497.png"), 1), () => new Blish_HUD.Settings.UI.Views.SettingsView(_packSettings));
            newWindow.AddTab(new WindowTab(GetLoca("level4"), ContentsManager.GetTexture(@"156027.png"), 2), () => new Blish_HUD.Settings.UI.Views.SettingsView(_packSettings));

            newWindow.AddTab(new WindowTab(GetLoca("settings"), ContentsManager.GetTexture(@"156027.png"), 2), () => new Blish_HUD.Settings.UI.Views.SettingsView(_packSettings));

            _fractalHelperIcon.Menu = _fractalHelperContextMenuStrip;

            _fractalHelperIcon.Click += delegate {
                newWindow.ToggleWindow();
            };
        }

        protected override async Task LoadAsync()
        {
            _directoryReader = new DirectoryReader(_fractalHelperDirectory);

            _directoryReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                ReadJson(fileStream);
            }, ".json");
        }

        private class JsonLoca
        {
            #pragma warning disable IDE1006 // ignore style, cause it is lowercase in the json-files
            public string key { get; set; }
            public string en { get; set; }
            public string de { get; set; }
            public string es { get; set; }
            public string fr { get; set; }
            public string kp { get; set; }
            #pragma warning restore IDE1006 // ignore style, cause it is lowercase in the json-files
        }

        private void ReadLoca(Stream fileStream)
        {
            string lang = GameService.Overlay.UserLocale.Value.ToString();

            string jsonContent;

            using (var jsonReader = new StreamReader(fileStream))
            {
                jsonContent = jsonReader.ReadToEnd();
            }

            List<JsonLoca> userDetails = System.Text.Json.JsonSerializer.Deserialize<List<JsonLoca>>(jsonContent, _jsonOptions);

            foreach(var v in userDetails)
            {
                string value;
                
                switch (lang)
                {
                    case "German":
                        value = v.de;
                        break;
                    case "Spanish":
                        value = v.es;
                        break;
                    case "French":
                        value = v.fr;
                        break;
                    case "Korean":
                        value = v.kp;
                        break;
                    default:
                        value = v.en;
                        break;
                }

                if (value == null || value.Length == 0)
                {
                    value = v.en;
                }

                _loca.Add(v.key, value);
            }
        }

        private void ReadJson(Stream fileStream) //TODO: this one needs to be edited to load specific markers data
        {
            /*
            string jsonContent;
            using (var jsonReader = new StreamReader(fileStream))
            {
                jsonContent = jsonReader.ReadToEnd();
            }

            try
            {
                string g = System.Text.Json.JsonSerializer.Deserialize<string>(jsonContent, _jsonOptions);
                Logger.Info(g);
            }
            catch (Exception ex)
            {
                Logger.Error("FractalHelper deserialization failure: " + ex.Message);
            }
            //*/
        }

        private void ExtractFile(string filePath)
        {
            var fullPath = Path.Combine(DirectoriesManager.GetFullDirectoryPath("fractalhelper"), filePath);

            using (var fs = Module.Instance.ContentsManager.GetFileStream(filePath))
            {
                fs.Position = 0;
                byte[] buffer = new byte[fs.Length];
                var content = fs.Read(buffer, 0, (int)fs.Length);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                System.IO.File.WriteAllBytes(fullPath, buffer);
            }
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime)
        {

        }

        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload here

            // All static members must be manually unset
        }

        public class TabbedSettingWindow : WindowBase
        {
            private const int TAB_HEIGHT = 52;
            private const int TAB_WIDTH = 94;
            private const int TAB_ICON_SIZE = 48;
            private const int TAB_SECTION_WIDTH = 39;
            private const int WINDOWCONTENT_WIDTH = 825;
            private const int WINDOWCONTENT_HEIGHT = 680;

            private static readonly Microsoft.Xna.Framework.Rectangle StandardTabBounds = new Microsoft.Xna.Framework.Rectangle(TAB_SECTION_WIDTH, TAB_ICON_SIZE/3, TAB_WIDTH, TAB_HEIGHT);

            #region tabs
            public event EventHandler<EventArgs> TabChanged;

            protected int _selectedTabIndex = -1;
            public int SelectedTabIndex
            {
                get => _selectedTabIndex;
                set
                {
                    if (SetProperty(ref _selectedTabIndex, value, true))
                    {
                        OnTabChanged(EventArgs.Empty);
                    }
                }
            }

            public WindowTab SelectedTab => _tabs.Count > _selectedTabIndex ? _tabs[_selectedTabIndex] : null;

            private int _hoveredTabIndex = 0;
            private int HoveredTabIndex
            {
                get => _hoveredTabIndex;
                set => SetProperty(ref _hoveredTabIndex, value);
            }
            #endregion

            private readonly LinkedList<IView> _currentNav = new LinkedList<IView>();

            private readonly Dictionary<WindowTab, Microsoft.Xna.Framework.Rectangle> _tabRegions = new Dictionary<WindowTab, Microsoft.Xna.Framework.Rectangle>();
            private readonly Dictionary<WindowTab, Panel> _panels = new Dictionary<WindowTab, Panel>();
            private readonly Dictionary<WindowTab, Func<IView>> _views = new Dictionary<WindowTab, Func<IView>>();
            private List<WindowTab> _tabs = new List<WindowTab>();

            private readonly ViewContainer _activeViewContainer;

            // TODO: Remove public access to _panels - only kept currently as it is used by KillProof.me module (need more robust "Navigate()" call for panel history)
            public Dictionary<WindowTab, Panel> Panels => _panels;

            #region initiation
            private Texture2D _textureDefaultBackround;
            private Texture2D _textureSplitLine;
            private Texture2D _textureBlackFade;
            private Texture2D _textureTabActive;

            public TabbedSettingWindow()
            {
                InitTextures();

                var tabWindowTexture = _textureDefaultBackround;
                tabWindowTexture = tabWindowTexture.Duplicate().SetRegion(0, 0, 64, _textureDefaultBackround.Height, Microsoft.Xna.Framework.Color.Transparent);

                this.ConstructWindow(tabWindowTexture, new Vector2(25, 40), new Microsoft.Xna.Framework.Rectangle(0, 0, 908, 660), new Thickness(60, 75, 445, 25), 40);

                _contentRegion = new Microsoft.Xna.Framework.Rectangle(TAB_WIDTH / 2, 48, WINDOWCONTENT_WIDTH, WINDOWCONTENT_HEIGHT);

                _activeViewContainer = new ViewContainer()
                {
                    HeightSizingMode = SizingMode.Fill,
                    WidthSizingMode = SizingMode.Fill,
                    Size = _contentRegion.Value.Size,
                    Parent = this
                };
            }

            private void InitTextures()
            {
                _textureDefaultBackround = Instance.ContentsManager.GetTexture("156006.png");
                _textureSplitLine = Content.GetTexture("605024");
                _textureBlackFade = Content.GetTexture("fade-down-46");
                _textureTabActive = Content.GetTexture("window-tab-active");
            }
            #endregion

            #region event-/ mouse input-handling
            protected virtual void OnTabChanged(EventArgs e)
            {
                this.Subtitle = SelectedTab.Name;

                Navigate(_views[this.SelectedTab](), false);

                this.TabChanged?.Invoke(this, e);
            }

            protected override CaptureType CapturesInput()
            {
                return CaptureType.Mouse | CaptureType.MouseWheel | CaptureType.Filter;
            }

            protected override void OnMouseLeft(MouseEventArgs e)
            {
                this.HoveredTabIndex = -1;

                base.OnMouseLeft(e);
            }

            protected override void OnMouseMoved(MouseEventArgs e)
            {
                bool newSet = false;

                if (RelativeMousePosition.X < StandardTabBounds.Right && RelativeMousePosition.Y > StandardTabBounds.Y)
                {
                    var tabList = _tabRegions.ToList();

                    for (int tabIndex = 0; tabIndex < _tabs.Count; tabIndex++)
                    {
                        var tab = _tabs[tabIndex];

                        if (_tabRegions[tab].Contains(RelativeMousePosition))
                        {
                            HoveredTabIndex = tabIndex;
                            newSet = true;
                            this.BasicTooltipText = tab.Name;

                            break;
                        }
                    }
                    tabList.Clear();
                }

                if (!newSet)
                {
                    this.HoveredTabIndex = -1;
                    this.BasicTooltipText = null;
                }

                base.OnMouseMoved(e);
            }

            protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
            {
                if (RelativeMousePosition.X < StandardTabBounds.Right && RelativeMousePosition.Y > StandardTabBounds.Y)
                {
                    var tabList = _tabs.ToList();

                    for (int tabIndex = 0; tabIndex < _tabs.Count; tabIndex++)
                    {
                        var tab = tabList[tabIndex];

                        if (_tabRegions[tab].Contains(RelativeMousePosition))
                        {
                            SelectedTabIndex = tabIndex;

                            break;
                        }
                    }
                    tabList.Clear();
                }

                base.OnLeftMouseButtonPressed(e);
            }
            #endregion

            #region Navigation
            public void Navigate(IView newView, bool keepHistory = true)
            {
                if (!keepHistory)
                {
                    _currentNav.Clear();
                }

                _currentNav.AddLast(newView);

                _activeViewContainer.Show(newView);
            }

            public override void NavigateBack()
            {
                if (_currentNav.Count > 1)
                {
                    _currentNav.RemoveLast();
                }

                _activeViewContainer.Show(_currentNav.Last.Value);
            }

            public override void NavigateHome()
            {
                _activeViewContainer.Show(_currentNav.First.Value);

                _currentNav.Clear();

                _currentNav.AddFirst(_activeViewContainer.CurrentView);
            }

            #endregion

            #region Tab Handling

            public WindowTab AddTab(string name, AsyncTexture2D icon, Func<IView> viewFunc, int priority = 0)
            {
                var tab = new WindowTab(name, icon, priority);
                AddTab(tab, viewFunc);

                return tab;
            }

            public void AddTab(WindowTab tab, Func<IView> viewFunc)
            {
                if (!_tabs.Contains(tab))
                {
                    var prevTab = _tabs.Count > 0 ? _tabs[this.SelectedTabIndex] : tab;

                    _tabs.Add(tab);
                    _tabRegions.Add(tab, TabBoundsFromIndex(_tabRegions.Count));
                    _views.Add(tab, viewFunc);

                    _tabs = _tabs.OrderBy(t => t.Priority).ToList();

                    for (int i = 0; i < _tabs.Count; i++)
                    {
                        _tabRegions[_tabs[i]] = TabBoundsFromIndex(i);
                    }

                    // Update tab index without making tab switch noise
                    if (_selectedTabIndex == -1)
                    {
                        _subtitle = prevTab.Name;
                        _selectedTabIndex = _tabs.IndexOf(prevTab);

                        Navigate(viewFunc(), false);
                    }
                    else
                    {
                        _selectedTabIndex = _tabs.IndexOf(prevTab);
                    }

                    Invalidate();
                }
            }

            public void RemoveTab(WindowTab tab)
            {
                // TODO: If the last tab is for some reason removed, this will crash the application
                var prevTab = _tabs.Count > 0 ? _tabs[this.SelectedTabIndex] : _tabs[0];

                if (_tabs.Contains(tab))
                {
                    _tabs.Remove(tab);
                    _tabRegions.Remove(tab);
                    _panels.Remove(tab);
                    _views.Remove(tab);
                }

                _tabs = _tabs.OrderBy(t => t.Priority).ToList();

                for (var tabIndex = 0; tabIndex < _tabRegions.Count; tabIndex++)
                {
                    var curTab = _tabs[tabIndex];
                    _tabRegions[curTab] = TabBoundsFromIndex(tabIndex);
                }

                if (_tabs.Contains(prevTab))
                {
                    _selectedTabIndex = _tabs.IndexOf(prevTab);
                }

                Invalidate();
            }

            private Microsoft.Xna.Framework.Rectangle TabBoundsFromIndex(int index)
            {
                return StandardTabBounds.OffsetBy(-TAB_WIDTH, ContentRegion.Y + index * TAB_HEIGHT);
            }
            #endregion

            #region Calculated Layout
            private Microsoft.Xna.Framework.Rectangle _layoutTopTabBarBounds;
            private Microsoft.Xna.Framework.Rectangle _layoutBottomTabBarBounds;

            private Microsoft.Xna.Framework.Rectangle _layoutTopSplitLineBounds;
            private Microsoft.Xna.Framework.Rectangle _layoutBottomSplitLineBounds;

            private Microsoft.Xna.Framework.Rectangle _layoutTopSplitLineSourceBounds;
            private Microsoft.Xna.Framework.Rectangle _layoutBottomSplitLineSourceBounds;

            public override void RecalculateLayout()
            {
                base.RecalculateLayout();

                if (_tabs.Count == 0) return;

                var firstTabBounds = TabBoundsFromIndex(0);
                var selectedTabBounds = _tabRegions[this.SelectedTab];
                var lastTabBounds = TabBoundsFromIndex(_tabRegions.Count - 1);

                _layoutTopTabBarBounds = new Microsoft.Xna.Framework.Rectangle(0, 0, TAB_SECTION_WIDTH, firstTabBounds.Top);
                _layoutBottomTabBarBounds = new Microsoft.Xna.Framework.Rectangle(0, lastTabBounds.Bottom, TAB_SECTION_WIDTH, _size.Y - lastTabBounds.Bottom);

                int topSplitHeight = selectedTabBounds.Top - ContentRegion.Top;
                int bottomSplitHeight = ContentRegion.Bottom - selectedTabBounds.Bottom;

                _layoutTopSplitLineBounds = new Microsoft.Xna.Framework.Rectangle(ContentRegion.X - _textureSplitLine.Width + 1, ContentRegion.Y, _textureSplitLine.Width, topSplitHeight);
                _layoutTopSplitLineSourceBounds = new Microsoft.Xna.Framework.Rectangle(0, 0, _textureSplitLine.Width, topSplitHeight);
                _layoutBottomSplitLineBounds = new Microsoft.Xna.Framework.Rectangle(ContentRegion.X - _textureSplitLine.Width + 1, selectedTabBounds.Bottom, _textureSplitLine.Width, bottomSplitHeight);
                _layoutBottomSplitLineSourceBounds = new Microsoft.Xna.Framework.Rectangle(0, _textureSplitLine.Height - bottomSplitHeight, _textureSplitLine.Width, bottomSplitHeight);
            }

            public override void PaintBeforeChildren(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Rectangle bounds)
            {
                base.PaintBeforeChildren(spriteBatch, bounds);

                // Draw black block for tab bar
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, _layoutTopTabBarBounds, Microsoft.Xna.Framework.Color.Black);

                // Draw black fade for tab bar
                spriteBatch.DrawOnCtrl(this, _textureBlackFade, _layoutBottomTabBarBounds);

                // Draw tabs
                int i = 0;

                foreach (var tab in _tabs)
                {
                    bool active = (i == this.SelectedTabIndex);
                    bool hovered = (i == this.HoveredTabIndex);

                    var tabBounds = _tabRegions[tab];
                    var subBounds = new Microsoft.Xna.Framework.Rectangle(tabBounds.X + tabBounds.Width / 2, tabBounds.Y, TAB_WIDTH / 2, tabBounds.Height);

                    if (active)
                    {
                        spriteBatch.DrawOnCtrl(this, _textureDefaultBackround, tabBounds, tabBounds.OffsetBy(_windowBackgroundOrigin.ToPoint()).OffsetBy(-5, -13).Add(0, -35, 0, 0).Add(tabBounds.Width / 3, 0, -tabBounds.Width / 3, 0), Microsoft.Xna.Framework.Color.White);

                        spriteBatch.DrawOnCtrl(this, _textureTabActive, tabBounds);
                    }
                    else
                    {
                        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Microsoft.Xna.Framework.Rectangle(0, tabBounds.Y, TAB_SECTION_WIDTH, tabBounds.Height), Microsoft.Xna.Framework.Color.Black);
                    }

                    spriteBatch.DrawOnCtrl(this, tab.Icon, new Microsoft.Xna.Framework.Rectangle(TAB_WIDTH / 4 - TAB_ICON_SIZE / 2 + 2, TAB_HEIGHT / 2 - TAB_ICON_SIZE / 2, TAB_ICON_SIZE, TAB_ICON_SIZE).OffsetBy(subBounds.Location), active || hovered ? Microsoft.Xna.Framework.Color.White : ContentService.Colors.DullColor);

                    i++;
                }

                // Draw top of split
                spriteBatch.DrawOnCtrl(this, _textureSplitLine, _layoutTopSplitLineBounds, _layoutTopSplitLineSourceBounds);

                // Draw bottom of split
                spriteBatch.DrawOnCtrl(this, _textureSplitLine, _layoutBottomSplitLineBounds, _layoutBottomSplitLineSourceBounds);
            }
        }
    }
    #endregion
}
