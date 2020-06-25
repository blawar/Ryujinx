using ARMeilleure.Translation.PTC;
using Gtk;
using LibHac.Common;
using LibHac.Ns;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Configuration;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.FileSystem.Content;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ui
{
    public class MainWindow : Window
    {
        private readonly GtkUserInterface  _gtkUserInterface;
        private readonly VirtualFileSystem _virtualFileSystem;
        private readonly ContentManager    _contentManager;
        private readonly AutoResetEvent    _deviceExitStatus;
        private readonly ListStore         _tableStore;

        private GlRenderer _glWidget;
        private bool _updatingGameTable;
        private bool _gameLoaded;
        private bool _ending;

#pragma warning disable CS0169
        private readonly Debugger.Debugger _debugger;
        private bool _debuggerOpened;

#pragma warning disable CS0649, IDE0044
        [Builder.Object] MenuBar        _menuBar;
        [Builder.Object] MenuItem       _loadFileMenuItem;
        [Builder.Object] MenuItem       _loadFolderMenuItem;
        [Builder.Object] MenuItem       _openRyujinxFolderMenuItem;
        [Builder.Object] MenuItem       _exitMenuItem;
        [Builder.Object] MenuItem       _settingsMenuItem;
        [Builder.Object] MenuItem       _aboutMenuItem;
        [Builder.Object] Box            _footerBox;
        [Builder.Object] Box            _statusBar;
        [Builder.Object] MenuItem       _stopEmulation;
        [Builder.Object] MenuItem       _fullScreen;
        [Builder.Object] CheckMenuItem  _favToggle;
        [Builder.Object] MenuItem       _firmwareInstallDirectory;
        [Builder.Object] MenuItem       _firmwareInstallFile;
        [Builder.Object] Label          _hostStatus;
        [Builder.Object] MenuItem       _openDebugger;
        [Builder.Object] CheckMenuItem  _iconToggle;
        [Builder.Object] CheckMenuItem  _publisherToggle;
        [Builder.Object] CheckMenuItem  _titleToggle;
        [Builder.Object] CheckMenuItem  _timePlayedToggle;
        [Builder.Object] CheckMenuItem  _versionToggle;
        [Builder.Object] CheckMenuItem  _lastPlayedToggle;
        [Builder.Object] CheckMenuItem  _fileExtToggle;
        [Builder.Object] CheckMenuItem  _pathToggle;
        [Builder.Object] CheckMenuItem  _fileSizeToggle;
        [Builder.Object] Label          _dockedMode;
        [Builder.Object] Label          _gameStatus;
        [Builder.Object] TreeView       _gameTable;
        [Builder.Object] TreeSelection  _gameTableSelection;
        [Builder.Object] ScrolledWindow _gameTableWindow;
        [Builder.Object] Label          _gpuName;
        [Builder.Object] Label          _progressLabel;
        [Builder.Object] Label          _firmwareVersionLabel;
        [Builder.Object] LevelBar       _progressBar;
        [Builder.Object] Box            _viewBox;
        [Builder.Object] Label          _vSyncStatus;
        [Builder.Object] Box            _listStatusBox;
        [Builder.Object] EventBox       _refreshListEventBox;
#pragma warning restore CS0169, CS0649, IDE0044

        public MainWindow(GtkUserInterface gtkUserInterface, VirtualFileSystem virtualFileSystem, ContentManager contentManager) 
            : this(new Builder("Ryujinx.Ui.MainWindow.glade"), gtkUserInterface, virtualFileSystem, contentManager) { }

        private MainWindow(Builder builder, GtkUserInterface gtkUserInterface, VirtualFileSystem virtualFileSystem, ContentManager contentManager)
            : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            _gtkUserInterface  = gtkUserInterface;
            _virtualFileSystem = virtualFileSystem;
            _contentManager    = contentManager;
            _deviceExitStatus  = new AutoResetEvent(false);

            int monitorWidth  = Display.PrimaryMonitor.Geometry.Width  * Display.PrimaryMonitor.ScaleFactor;
            int monitorHeight = Display.PrimaryMonitor.Geometry.Height * Display.PrimaryMonitor.ScaleFactor;

            this.DefaultWidth  = monitorWidth < 1280 ? monitorWidth : 1280;
            this.DefaultHeight = monitorHeight < 760 ? monitorHeight : 760;

            this.Icon  = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");
            this.Title = $"Ryujinx {Constants.Version}";

            this.DeleteEvent        += (sender, args) => End();
            _exitMenuItem.Activated += (sender, args) => End();

            _loadFileMenuItem.Activated                += Load_File;
            _loadFolderMenuItem.Activated              += Load_Folder;
            _openRyujinxFolderMenuItem.Activated       += Open_Ryujinx_Folder;
            _settingsMenuItem.Activated                += Settings_Pressed;
            _aboutMenuItem.Activated                   += About_Pressed;
            _gameTable.RowActivated                    += Row_Activated;
            _gameTable.ButtonReleaseEvent              += Row_Clicked;
            _fullScreen.Activated                      += FullScreen_Toggled;
            _stopEmulation.Activated                   += StopEmulation_Pressed;
            _firmwareInstallFile.Activated             += Installer_File_Pressed;
            _firmwareInstallDirectory.Activated        += Installer_Directory_Pressed;
            _favToggle.Toggled                         += Fav_Toggled;
            _iconToggle.Toggled                        += Icon_Toggled;
            _titleToggle.Toggled                       += Title_Toggled;
            _versionToggle.Toggled                     += Version_Toggled;
            _publisherToggle.Toggled                   += Publisher_Toggled;
            _timePlayedToggle.Toggled                  += TimePlayed_Toggled;
            _lastPlayedToggle.Toggled                  += LastPlayed_Toggled;
            _fileExtToggle.Toggled                     += FileExt_Toggled;
            _fileSizeToggle.Toggled                    += FileSize_Toggled;
            _pathToggle.Toggled                        += Path_Toggled;
            _refreshListEventBox.ButtonReleaseEvent    += RefreshList_Pressed;
            ApplicationLibrary.ApplicationAdded        += Application_Added;
            ApplicationLibrary.ApplicationCountUpdated += ApplicationCount_Updated;
            GlRenderer.StatusUpdatedEvent              += Update_StatusBar;

            // First we check that a migration isn't needed. (because VirtualFileSystem will create the new directory otherwise)
            bool continueWithStartup = Migration.PromptIfMigrationNeededForStartup(this, out bool migrationNeeded);
            if (!continueWithStartup)
            {
                End();
            }

            if (migrationNeeded)
            {
                bool migrationSuccessful = Migration.DoMigrationForStartup(_virtualFileSystem, _gtkUserInterface.ShowErrorDialog);

                if (!migrationSuccessful)
                {
                    End();
                }
            }

            ApplyTheme();

            _stopEmulation.Sensitive = false;

            if (ConfigurationState.Instance.Ui.GuiColumns.FavColumn)        _favToggle.Active        = true;
            if (ConfigurationState.Instance.Ui.GuiColumns.IconColumn)       _iconToggle.Active       = true;
            if (ConfigurationState.Instance.Ui.GuiColumns.AppColumn)        _titleToggle.Active      = true;
            if (ConfigurationState.Instance.Ui.GuiColumns.DevColumn)        _publisherToggle.Active  = true;
            if (ConfigurationState.Instance.Ui.GuiColumns.VersionColumn)    _versionToggle.Active    = true;
            if (ConfigurationState.Instance.Ui.GuiColumns.TimePlayedColumn) _timePlayedToggle.Active = true;
            if (ConfigurationState.Instance.Ui.GuiColumns.LastPlayedColumn) _lastPlayedToggle.Active = true;
            if (ConfigurationState.Instance.Ui.GuiColumns.FileExtColumn)    _fileExtToggle.Active    = true;
            if (ConfigurationState.Instance.Ui.GuiColumns.FileSizeColumn)   _fileSizeToggle.Active   = true;
            if (ConfigurationState.Instance.Ui.GuiColumns.PathColumn)       _pathToggle.Active       = true;

#if USE_DEBUGGING
            _debugger = new Debugger.Debugger();
            _openDebugger.Activated += OpenDebugger_Pressed;
#else
            _openDebugger.Hide();
#endif

            _gameTable.Model = _tableStore = new ListStore(
                typeof(bool),
                typeof(Gdk.Pixbuf),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(BlitStruct<ApplicationControlProperty>));

            _tableStore.SetSortFunc(5, TimePlayedSort);
            _tableStore.SetSortFunc(6, LastPlayedSort);
            _tableStore.SetSortFunc(8, FileSizeSort);

            int  columnId  = ConfigurationState.Instance.Ui.ColumnSort.SortColumnId;
            bool ascending = ConfigurationState.Instance.Ui.ColumnSort.SortAscending;

            _tableStore.SetSortColumnId(columnId, ascending ? SortType.Ascending : SortType.Descending);

            _gameTable.EnableSearch = true;
            _gameTable.SearchColumn = 2;

            UpdateColumns();
            UpdateGameTable();

            ConfigurationState.Instance.Ui.GameDirs.Event += (sender, args) =>
            {
                if (args.OldValue != args.NewValue)
                {
                    UpdateGameTable();
                }
            };

            Task.Run(RefreshFirmwareLabel);

            _statusBar.Hide();
        }

#if USE_DEBUGGING
        private void OpenDebugger_Pressed(object sender, EventArgs args)
        {
            if (_debuggerOpened)
            {
                return;
            }

            Window debugWindow = new Window("Debugger");
            
            debugWindow.SetSizeRequest(1280, 640);
            debugWindow.Child = _debugger.Widget;
            debugWindow.DeleteEvent += DebugWindow_DeleteEvent;
            debugWindow.ShowAll();

            _debugger.Enable();

            _debuggerOpened = true;
        }

        private void DebugWindow_DeleteEvent(object sender, DeleteEventArgs args)
        {
            _debuggerOpened = false;

            _debugger.Disable();

            (_debugger.Widget.Parent as Window)?.Remove(_debugger.Widget);
        }

#endif
        internal static void ApplyTheme()
        {
            if (!ConfigurationState.Instance.Ui.EnableCustomTheme)
            {
                return;
            }

            if (File.Exists(ConfigurationState.Instance.Ui.CustomThemePath) && (System.IO.Path.GetExtension(ConfigurationState.Instance.Ui.CustomThemePath) == ".css"))
            {
                CssProvider cssProvider = new CssProvider();

                cssProvider.LoadFromPath(ConfigurationState.Instance.Ui.CustomThemePath);

                StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);
            }
            else
            {
                Logger.PrintWarning(LogClass.Application, $"The \"custom_theme_path\" section in \"Config.json\" contains an invalid path: \"{ConfigurationState.Instance.Ui.CustomThemePath}\".");
            }
        }

        private void UpdateColumns()
        {
            foreach (TreeViewColumn column in _gameTable.Columns)
            {
                _gameTable.RemoveColumn(column);
            }

            CellRendererToggle favToggle = new CellRendererToggle();
            favToggle.Toggled += FavToggle_Toggled;

            if (ConfigurationState.Instance.Ui.GuiColumns.FavColumn)        _gameTable.AppendColumn("Fav",         favToggle,                "active", 0);
            if (ConfigurationState.Instance.Ui.GuiColumns.IconColumn)       _gameTable.AppendColumn("Icon",        new CellRendererPixbuf(), "pixbuf", 1);
            if (ConfigurationState.Instance.Ui.GuiColumns.AppColumn)        _gameTable.AppendColumn("Application", new CellRendererText(),   "text",   2);
            if (ConfigurationState.Instance.Ui.GuiColumns.DevColumn)        _gameTable.AppendColumn("Publisher",   new CellRendererText(),   "text",   3);
            if (ConfigurationState.Instance.Ui.GuiColumns.VersionColumn)    _gameTable.AppendColumn("Version",     new CellRendererText(),   "text",   4);
            if (ConfigurationState.Instance.Ui.GuiColumns.TimePlayedColumn) _gameTable.AppendColumn("Time Played", new CellRendererText(),   "text",   5);
            if (ConfigurationState.Instance.Ui.GuiColumns.LastPlayedColumn) _gameTable.AppendColumn("Last Played", new CellRendererText(),   "text",   6);
            if (ConfigurationState.Instance.Ui.GuiColumns.FileExtColumn)    _gameTable.AppendColumn("File Ext",    new CellRendererText(),   "text",   7);
            if (ConfigurationState.Instance.Ui.GuiColumns.FileSizeColumn)   _gameTable.AppendColumn("File Size",   new CellRendererText(),   "text",   8);
            if (ConfigurationState.Instance.Ui.GuiColumns.PathColumn)       _gameTable.AppendColumn("Path",        new CellRendererText(),   "text",   9);

            foreach (TreeViewColumn column in _gameTable.Columns)
            {
                switch (column.Title)
                {
                    case "Fav":
                        column.SortColumnId = 0;
                        column.Clicked += Column_Clicked;
                        break;
                    case "Application":
                        column.SortColumnId = 2;
                        column.Clicked += Column_Clicked;
                        break;
                    case "Publisher":
                        column.SortColumnId = 3;
                        column.Clicked += Column_Clicked;
                        break;
                    case "Version":
                        column.SortColumnId = 4;
                        column.Clicked += Column_Clicked;
                        break;
                    case "Time Played":
                        column.SortColumnId = 5;
                        column.Clicked += Column_Clicked;
                        break;
                    case "Last Played":
                        column.SortColumnId = 6;
                        column.Clicked += Column_Clicked;
                        break;
                    case "File Ext":
                        column.SortColumnId = 7;
                        column.Clicked += Column_Clicked;
                        break;
                    case "File Size":
                        column.SortColumnId = 8;
                        column.Clicked += Column_Clicked;
                        break;
                    case "Path":
                        column.SortColumnId = 9;
                        column.Clicked += Column_Clicked;
                        break;
                }
            }
        }

        internal void UpdateGameTable()
        {
            if (_updatingGameTable || _gameLoaded)
            {
                return;
            }

            _updatingGameTable = true;

            _tableStore.Clear();

            Thread applicationLibraryThread = new Thread(() =>
            {
                ApplicationLibrary.LoadApplications(ConfigurationState.Instance.Ui.GameDirs,
                    _virtualFileSystem, ConfigurationState.Instance.System.Language, _gtkUserInterface.ShowErrorDialog);

                _updatingGameTable = false;
            });
            applicationLibraryThread.Name = "GUI.ApplicationLibraryThread";
            applicationLibraryThread.IsBackground = true;
            applicationLibraryThread.Start();
        }

        internal void LoadApplication(string path)
        {
            if (_gameLoaded)
            {
                _gtkUserInterface.ShowInfoDialog("A game has already been loaded", "Please close it first and try again.");
            }
            else
            {
                if (ConfigurationState.Instance.Logger.EnableDebug.Value)
                {
                    MessageDialog debugWarningDialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Warning, ButtonsType.YesNo, null)
                    {
                        Title         = "Ryujinx - Warning",
                        Text          = "You have debug logging enabled, which is designed to be used by developers only.",
                        SecondaryText = "For optimal performance, it's recommended to disable debug logging. Would you like to disable debug logging now?"
                    };

                    if (debugWarningDialog.Run() == (int)ResponseType.Yes)
                    {
                        ConfigurationState.Instance.Logger.EnableDebug.Value = false;
                        SaveConfig();
                    }

                    debugWarningDialog.Dispose();
                }

                if (!string.IsNullOrWhiteSpace(ConfigurationState.Instance.Graphics.ShadersDumpPath.Value))
                {
                    MessageDialog shadersDumpWarningDialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Warning, ButtonsType.YesNo, null)
                    {
                        Title         = "Ryujinx - Warning",
                        Text          = "You have shader dumping enabled, which is designed to be used by developers only.",
                        SecondaryText = "For optimal performance, it's recommended to disable shader dumping. Would you like to disable shader dumping now?"
                    };

                    if (shadersDumpWarningDialog.Run() == (int)ResponseType.Yes)
                    {
                        ConfigurationState.Instance.Graphics.ShadersDumpPath.Value = "";
                        SaveConfig();
                    }

                    shadersDumpWarningDialog.Dispose();
                }

                _gtkUserInterface.InvokeLoadTitleEvent(path, d =>
                {
                    HLE.Switch device = d;
                    
                    _deviceExitStatus.Reset();

                    _gameLoaded = true;
                    _stopEmulation.Sensitive = true;

                    _firmwareInstallFile.Sensitive = false;
                    _firmwareInstallDirectory.Sensitive = false;

                    ApplicationLibrary.LoadAndSaveMetaData(device.Application.TitleIdText, appMetadata =>
                    {
                        appMetadata.LastPlayed = DateTime.UtcNow.ToString();
                    });

#if MACOS_BUILD
                    CreateGameWindow(device);
#else
                    Thread windowThread = new Thread(() =>
                    {
                        CreateGameWindow(device);
                    })
                    {
                        Name = "GUI.WindowThread"
                    };

                    windowThread.Start();
#endif
                });
            }
        }

        private void CreateGameWindow(HLE.Switch device)
        {
            _glWidget = new GlRenderer(device, _gtkUserInterface);

            Application.Invoke(delegate
            {
                _viewBox.Remove(_gameTableWindow);
                _glWidget.Expand = true;
                _viewBox.Child = _glWidget;

                _glWidget.ShowAll();
                EditFooterForGameRender();
            });

            _glWidget.WaitEvent.WaitOne();

            _glWidget.Start();

            ApplicationLibrary.LoadAndSaveMetaData(device.Application.TitleIdText, appMetadata =>
            {
                DateTime lastPlayedDateTime = DateTime.Parse(appMetadata.LastPlayed);
                double sessionTimePlayed = DateTime.UtcNow.Subtract(lastPlayedDateTime).TotalSeconds;

                appMetadata.TimePlayed += Math.Round(sessionTimePlayed, MidpointRounding.AwayFromZero);
            });

            Ptc.Close();
            PtcProfiler.Stop();

            device.Dispose();
            _deviceExitStatus.Set();

            // NOTE: Everything that is here will not be executed when you close the UI.
            Application.Invoke(delegate
            {
                _viewBox.Remove(_glWidget);
                _glWidget.Exit();

                if(_glWidget.Window != this.Window && _glWidget.Window != null)
                {
                    _glWidget.Window.Dispose();
                }

                _glWidget.Dispose();

                _viewBox.Add(_gameTableWindow);

                _gameTableWindow.Expand = true;

                this.Window.Title = $"Ryujinx {Constants.Version}";

                _gameLoaded = false;
                _glWidget   = null;

                DiscordIntegrationModule.SwitchToMainMenu();

                RecreateFooterForMenu();

                UpdateColumns();
                UpdateGameTable();

                Task.Run(RefreshFirmwareLabel);

                _stopEmulation.Sensitive            = false;
                _firmwareInstallFile.Sensitive      = true;
                _firmwareInstallDirectory.Sensitive = true;
            });
        }

        private void RecreateFooterForMenu()
        {
            _listStatusBox.Show();
            _statusBar.Hide();
        }

        private void EditFooterForGameRender()
        {
            _listStatusBox.Hide();
            _statusBar.Show();
        }

        public void ToggleExtraWidgets(bool show)
        {
            if (_glWidget != null)
            {
                if (show)
                {
                    _menuBar.ShowAll();
                    _footerBox.Show();
                    _statusBar.Show();
                }
                else
                {
                    _menuBar.Hide();
                    _footerBox.Hide();
                }
            }

            bool fullScreenToggled = this.Window.State.HasFlag(Gdk.WindowState.Fullscreen);

            _fullScreen.Label = fullScreenToggled ? "Exit Fullscreen" : "Enter Fullscreen";
        }

        public static void SaveConfig()
        {
            ConfigurationState.Instance.ToFileFormat().SaveConfig(ConfigurationState.ConfigurationPath);
        }

        private void End()
        {

#if USE_DEBUGGING
            _debugger.Dispose();
#endif

            if (_ending)
            {
                return;
            }

            _ending = true;

            if (_glWidget != null)
            {
                // We tell the widget that we are exiting
                _glWidget.Exit();

                // Wait for the other thread to dispose the HLE context before exiting.
                _deviceExitStatus.WaitOne();
            }

            this.Dispose();
        }
        //Events
        private void Application_Added(object sender, ApplicationAddedEventArgs args)
        {
            Application.Invoke(delegate
            {
                _tableStore.AppendValues(
                    args.AppData.Favorite,
                    new Gdk.Pixbuf(args.AppData.Icon, 75, 75),
                    $"{args.AppData.TitleName}\n{args.AppData.TitleId.ToUpper()}",
                    args.AppData.Publisher,
                    args.AppData.Version,
                    args.AppData.TimePlayed,
                    args.AppData.LastPlayed,
                    args.AppData.FileExtension,
                    args.AppData.FileSize,
                    args.AppData.Path,
                    args.AppData.ControlHolder);
            });
        }

        private void ApplicationCount_Updated(object sender, ApplicationCountUpdatedEventArgs args)
        {
            Application.Invoke(delegate
            {
                _progressLabel.Text = $"{args.NumAppsLoaded}/{args.NumAppsFound} Games Loaded";
                float barValue      = 0;

                if (args.NumAppsFound != 0)
                {
                    barValue = (float)args.NumAppsLoaded / args.NumAppsFound;
                }

                _progressBar.Value = barValue;

                if (args.NumAppsLoaded == args.NumAppsFound) // Reset the vertical scrollbar to the top when titles finish loading
                {
                    _gameTableWindow.Vadjustment.Value = 0;
                }
            });
        }

        private void Update_StatusBar(object sender, StatusUpdatedEventArgs args)
        {
            Application.Invoke(delegate
            {
                _hostStatus.Text = args.HostStatus;
                _gameStatus.Text = args.GameStatus;
                _gpuName.Text    = args.GpuName;
                _dockedMode.Text = args.DockedMode;

                if (args.VSyncEnabled)
                {
                    _vSyncStatus.Attributes = new Pango.AttrList();
                    _vSyncStatus.Attributes.Insert(new Pango.AttrForeground(11822, 60138, 51657));
                }
                else
                {
                    _vSyncStatus.Attributes = new Pango.AttrList();
                    _vSyncStatus.Attributes.Insert(new Pango.AttrForeground(ushort.MaxValue, 17733, 21588));
                }
            });
        }

        private void FavToggle_Toggled(object sender, ToggledArgs args)
        {
            _tableStore.GetIter(out TreeIter treeIter, new TreePath(args.Path));

            string titleId = _tableStore.GetValue(treeIter, 2).ToString().Split("\n")[1].ToLower();

            bool newToggleValue = !(bool)_tableStore.GetValue(treeIter, 0);

            _tableStore.SetValue(treeIter, 0, newToggleValue);

            ApplicationLibrary.LoadAndSaveMetaData(titleId, appMetadata =>
            {
                appMetadata.Favorite = newToggleValue;
            });
        }

        private void Column_Clicked(object sender, EventArgs args)
        {
            TreeViewColumn column = (TreeViewColumn)sender;

            ConfigurationState.Instance.Ui.ColumnSort.SortColumnId.Value  = column.SortColumnId;
            ConfigurationState.Instance.Ui.ColumnSort.SortAscending.Value = column.SortOrder == SortType.Ascending;

            SaveConfig();
        }

        private void Row_Activated(object sender, RowActivatedArgs args)
        {
            _gameTableSelection.GetSelected(out TreeIter treeIter);
            string path = (string)_tableStore.GetValue(treeIter, 9);

            LoadApplication(path);
        }

        private void Row_Clicked(object sender, ButtonReleaseEventArgs args)
        {
            if (args.Event.Button != 3) return;

            _gameTableSelection.GetSelected(out TreeIter treeIter);

            if (treeIter.UserData == IntPtr.Zero) return;

            BlitStruct<ApplicationControlProperty> controlData = (BlitStruct<ApplicationControlProperty>)_tableStore.GetValue(treeIter, 10);

            GameTableContextMenu contextMenu = new GameTableContextMenu(_gtkUserInterface, _tableStore, controlData, treeIter, _virtualFileSystem);
            contextMenu.ShowAll();
            contextMenu.PopupAtPointer(null);
        }

        private void Load_File(object sender, EventArgs args)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Choose the file to open", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            fileChooser.Filter = new FileFilter();
            fileChooser.Filter.AddPattern("*.nsp" );
            fileChooser.Filter.AddPattern("*.pfs0");
            fileChooser.Filter.AddPattern("*.xci" );
            fileChooser.Filter.AddPattern("*.nca" );
            fileChooser.Filter.AddPattern("*.nro" );
            fileChooser.Filter.AddPattern("*.nso" );

            RunLoadApplicationFileChooser(fileChooser);
        }

        private void Load_Folder(object sender, EventArgs args)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Choose the folder to open", this, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            RunLoadApplicationFileChooser(fileChooser);
        }

        private void RunLoadApplicationFileChooser(FileChooserDialog fileChooser)
        {
            int response = fileChooser.Run();
            string path = fileChooser.Filename;

            fileChooser.Dispose();

            if (response == (int)ResponseType.Accept)
            {
                LoadApplication(path);
            }
        }

        private void Open_Ryujinx_Folder(object sender, EventArgs args)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = _virtualFileSystem.GetBasePath(),
                UseShellExecute = true,
                Verb            = "open"
            });
        }

        private void StopEmulation_Pressed(object sender, EventArgs args)
        {
            _glWidget?.Exit();
        }

        private void Installer_File_Pressed(object sender, EventArgs args)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Choose the firmware file to open",
                                                                  this,
                                                                  FileChooserAction.Open,
                                                                  "Cancel",
                                                                  ResponseType.Cancel,
                                                                  "Open",
                                                                  ResponseType.Accept);

            fileChooser.Filter = new FileFilter();
            fileChooser.Filter.AddPattern("*.zip");
            fileChooser.Filter.AddPattern("*.xci");

            HandleInstallerDialog(fileChooser);
        }

        private void Installer_Directory_Pressed(object sender, EventArgs args)
        {
            FileChooserDialog directoryChooser = new FileChooserDialog("Choose the firmware directory to open",
                                                                       this,
                                                                       FileChooserAction.SelectFolder,
                                                                       "Cancel",
                                                                       ResponseType.Cancel,
                                                                       "Open",
                                                                       ResponseType.Accept);

            HandleInstallerDialog(directoryChooser);
        }

        private void RefreshFirmwareLabel()
        {
            SystemVersion currentFirmware = _contentManager.GetCurrentFirmwareVersion();

            GLib.Idle.Add(() =>
            {
                _firmwareVersionLabel.Text = currentFirmware != null ? currentFirmware.VersionString : "0.0.0";

                return false;
            });
        }

        private void HandleInstallerDialog(FileChooserDialog fileChooser)
        {
            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                MessageDialog dialog = null;

                try
                {
                    string filename = fileChooser.Filename;

                    fileChooser.Dispose();

                    SystemVersion firmwareVersion = _contentManager.VerifyFirmwarePackage(filename);

                    if (firmwareVersion == null)
                    {
                        dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, false, "")
                        {
                            Text          = "Firmware not found.",
                            SecondaryText = $"A valid system firmware was not found in {filename}."
                        };

                        Logger.PrintError(LogClass.Application, $"A valid system firmware was not found in {filename}.");

                        dialog.Run();
                        dialog.Hide();
                        dialog.Dispose();

                        return;
                    }

                    SystemVersion currentVersion = _contentManager.GetCurrentFirmwareVersion();

                    string dialogMessage = $"System version {firmwareVersion.VersionString} will be installed.";

                    if (currentVersion != null)
                    {
                        dialogMessage += $"This will replace the current system version {currentVersion.VersionString}. ";
                    }

                    dialogMessage += "Do you want to continue?";

                    dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, false, "")
                    {
                        Text          = $"Install Firmware {firmwareVersion.VersionString}",
                        SecondaryText = dialogMessage
                    };

                    int response = dialog.Run();

                    dialog.Dispose();

                    dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.None, false, "")
                    {
                        Text          = $"Install Firmware {firmwareVersion.VersionString}",
                        SecondaryText = "Installing firmware..."
                    };

                    if (response == (int)ResponseType.Yes)
                    {
                        Logger.PrintInfo(LogClass.Application, $"Installing firmware {firmwareVersion.VersionString}");
                        
                        Thread thread = new Thread(() =>
                        {
                            GLib.Idle.Add(() =>
                            {
                                dialog.Run();
                                return false;
                            });

                            try
                            {
                                _contentManager.InstallFirmware(filename);

                                GLib.Idle.Add(() =>
                                {
                                    dialog.Dispose();

                                    dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, false, "")
                                    {
                                        Text          = $"Install Firmware {firmwareVersion.VersionString}",
                                        SecondaryText = $"System version {firmwareVersion.VersionString} successfully installed."
                                    };



                                    Logger.PrintInfo(LogClass.Application, $"System version {firmwareVersion.VersionString} successfully installed.");

                                    dialog.Run();
                                    dialog.Dispose();

                                    return false;
                                });
                            }
                            catch (Exception ex)
                            {
                                GLib.Idle.Add(() =>
                                {
                                    dialog.Dispose();

                                    dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, false, "")
                                    {
                                        Text          = $"Install Firmware {firmwareVersion.VersionString} Failed.",
                                        SecondaryText = $"An error occured while installing system version {firmwareVersion.VersionString}." +
                                                        " Please check logs for more info."
                                    };

                                    Logger.PrintError(LogClass.Application, ex.Message);

                                    dialog.Run();
                                    dialog.Dispose();

                                    return false;
                                });
                            }
                            finally
                            {
                                RefreshFirmwareLabel();
                            }
                        });

                        thread.Name = "GUI.FirmwareInstallerThread";
                        thread.Start();
                    }
                    else
                    {
                        dialog.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    dialog?.Dispose();

                    dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, false, "")
                    {
                        Text          = "Parsing Firmware Failed.",
                        SecondaryText = "An error occured while parsing firmware. Please check the logs for more info."
                    };

                    Logger.PrintError(LogClass.Application, ex.Message);

                    dialog.Run();
                    dialog.Dispose();
                }
            }
            else
            {
                fileChooser.Dispose();
            }
        }

        private void FullScreen_Toggled(object sender, EventArgs args)
        {
            bool fullScreenToggled = this.Window.State.HasFlag(Gdk.WindowState.Fullscreen);

            if (!fullScreenToggled)
            {
                Fullscreen();

                ToggleExtraWidgets(false);
            }
            else
            {
                Unfullscreen();

                ToggleExtraWidgets(true);
            }
        }

        private void Settings_Pressed(object sender, EventArgs args)
        {
            SettingsWindow settingsWin = new SettingsWindow(_gtkUserInterface, _virtualFileSystem, _contentManager);
            settingsWin.Show();
        }

        private static void About_Pressed(object sender, EventArgs args)
        {
            AboutWindow aboutWin = new AboutWindow();
            aboutWin.Show();
        }

        private void Fav_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.FavColumn.Value = _favToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void Icon_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.IconColumn.Value = _iconToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void Title_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.AppColumn.Value = _titleToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void Publisher_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.DevColumn.Value = _publisherToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void Version_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.VersionColumn.Value = _versionToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void TimePlayed_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.TimePlayedColumn.Value = _timePlayedToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void LastPlayed_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.LastPlayedColumn.Value = _lastPlayedToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void FileExt_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.FileExtColumn.Value = _fileExtToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void FileSize_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.FileSizeColumn.Value = _fileSizeToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void Path_Toggled(object sender, EventArgs args)
        {
            ConfigurationState.Instance.Ui.GuiColumns.PathColumn.Value = _pathToggle.Active;

            SaveConfig();
            UpdateColumns();
        }

        private void RefreshList_Pressed(object sender, ButtonReleaseEventArgs args)
        {
            UpdateGameTable();
        }

        private static int TimePlayedSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            string aValue = model.GetValue(a, 5).ToString();
            string bValue = model.GetValue(b, 5).ToString();

            if (aValue.Length > 4 && aValue.Substring(aValue.Length - 4) == "mins")
            {
                aValue = (float.Parse(aValue[..^5]) * 60).ToString();
            }
            else if (aValue.Length > 3 && aValue.Substring(aValue.Length - 3) == "hrs")
            {
                aValue = (float.Parse(aValue[..^4]) * 3600).ToString();
            }
            else if (aValue.Length > 4 && aValue.Substring(aValue.Length - 4) == "days")
            {
                aValue = (float.Parse(aValue[..^5]) * 86400).ToString();
            }
            else
            {
                aValue = aValue[..^1];
            }

            if (bValue.Length > 4 && bValue.Substring(bValue.Length - 4) == "mins")
            {
                bValue = (float.Parse(bValue[..^5]) * 60).ToString();
            }
            else if (bValue.Length > 3 && bValue.Substring(bValue.Length - 3) == "hrs")
            {
                bValue = (float.Parse(bValue[..^4]) * 3600).ToString();
            }
            else if (bValue.Length > 4 && bValue.Substring(bValue.Length - 4) == "days")
            {
                bValue = (float.Parse(bValue[..^5]) * 86400).ToString();
            }
            else
            {
                bValue = bValue[..^1];
            }

            if (float.Parse(aValue) > float.Parse(bValue))
            {
                return -1;
            }

            return float.Parse(bValue) > float.Parse(aValue) ? 1 : 0;
        }

        private static int LastPlayedSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            string aValue = model.GetValue(a, 6).ToString();
            string bValue = model.GetValue(b, 6).ToString();

            if (aValue == "Never")
            {
                aValue = DateTime.UnixEpoch.ToString();
            }

            if (bValue == "Never")
            {
                bValue = DateTime.UnixEpoch.ToString();
            }

            return DateTime.Compare(DateTime.Parse(bValue), DateTime.Parse(aValue));
        }

        private static int FileSizeSort(ITreeModel model, TreeIter a, TreeIter b)
        {
            string aValue = model.GetValue(a, 8).ToString();
            string bValue = model.GetValue(b, 8).ToString();

            aValue = aValue.Substring(aValue.Length - 2) == "GB" ? (float.Parse(aValue[..^2]) * 1024).ToString() : aValue[..^2];
            bValue = bValue.Substring(bValue.Length - 2) == "GB" ? (float.Parse(bValue[..^2]) * 1024).ToString() : bValue[..^2];

            if (float.Parse(aValue) > float.Parse(bValue))
            {
                return -1;
            }
            
            return float.Parse(bValue) > float.Parse(aValue) ? 1 : 0;
        }
    }
}
