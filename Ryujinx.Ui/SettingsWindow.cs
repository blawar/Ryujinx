using Gtk;
using Ryujinx.Audio;
using Ryujinx.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Configuration.System;
using Ryujinx.HLE.HOS.Services.Time.TimeZone;
using Ryujinx.HLE.FileSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ryujinx.Ui
{
    public class SettingsWindow : Window
    {
        private readonly GtkUserInterface _gtkUserInterface;
        private readonly ListStore        _gameDirsBoxStore;

        private long _systemTimeOffset;

#pragma warning disable CS0649, IDE0044
        [Builder.Object] CheckButton  _errorLogToggle;
        [Builder.Object] CheckButton  _warningLogToggle;
        [Builder.Object] CheckButton  _infoLogToggle;
        [Builder.Object] CheckButton  _stubLogToggle;
        [Builder.Object] CheckButton  _debugLogToggle;
        [Builder.Object] CheckButton  _fileLogToggle;
        [Builder.Object] CheckButton  _guestLogToggle;
        [Builder.Object] CheckButton  _fsAccessLogToggle;
        [Builder.Object] Adjustment   _fsLogSpinAdjustment;
        [Builder.Object] CheckButton  _dockedModeToggle;
        [Builder.Object] CheckButton  _discordToggle;
        [Builder.Object] CheckButton  _vSyncToggle;
        [Builder.Object] CheckButton  _multiSchedToggle;
        [Builder.Object] CheckButton  _ptcToggle;
        [Builder.Object] CheckButton  _fsicToggle;
        [Builder.Object] CheckButton  _ignoreToggle;
        [Builder.Object] CheckButton  _directKeyboardAccess;
        [Builder.Object] ComboBoxText _systemLanguageSelect;
        [Builder.Object] ComboBoxText _systemRegionSelect;
        [Builder.Object] ComboBoxText _systemTimeZoneSelect;
        [Builder.Object] ComboBoxText _audioBackendSelect;
        [Builder.Object] SpinButton   _systemTimeYearSpin;
        [Builder.Object] SpinButton   _systemTimeMonthSpin;
        [Builder.Object] SpinButton   _systemTimeDaySpin;
        [Builder.Object] SpinButton   _systemTimeHourSpin;
        [Builder.Object] SpinButton   _systemTimeMinuteSpin;
        [Builder.Object] Adjustment   _systemTimeYearSpinAdjustment;
        [Builder.Object] Adjustment   _systemTimeMonthSpinAdjustment;
        [Builder.Object] Adjustment   _systemTimeDaySpinAdjustment;
        [Builder.Object] Adjustment   _systemTimeHourSpinAdjustment;
        [Builder.Object] Adjustment   _systemTimeMinuteSpinAdjustment;
        [Builder.Object] CheckButton  _customThemeToggle;
        [Builder.Object] Entry        _custThemePath;
        [Builder.Object] Button       _browseThemePath;
        [Builder.Object] Label        _custThemePathLabel;
        [Builder.Object] TreeView     _gameDirsBox;
        [Builder.Object] Entry        _addGameDirBox;
        [Builder.Object] Button       _addDirectory;
        [Builder.Object] Button       _removeDirectory;
        [Builder.Object] Entry        _graphicsShadersDumpPath;
        [Builder.Object] ComboBoxText _anisotropy;
        [Builder.Object] ComboBoxText _resScaleCombo;
        [Builder.Object] Entry        _resScaleText;
        [Builder.Object] Button       _openLogsFolderButton;
        [Builder.Object] Button       _configureController1;
        [Builder.Object] Button       _configureController2;
        [Builder.Object] Button       _configureController3;
        [Builder.Object] Button       _configureController4;
        [Builder.Object] Button       _configureController5;
        [Builder.Object] Button       _configureController6;
        [Builder.Object] Button       _configureController7;
        [Builder.Object] Button       _configureController8;
        [Builder.Object] Button       _configureControllerH;
        [Builder.Object] Button       _saveButton;
        [Builder.Object] Button       _closeButton;
#pragma warning restore CS0649, IDE0044

        public SettingsWindow(GtkUserInterface gtkUserInterface, VirtualFileSystem virtualFileSystem, HLE.FileSystem.Content.ContentManager contentManager) 
            : this(new Builder("Ryujinx.Ui.SettingsWindow.glade"), gtkUserInterface, virtualFileSystem, contentManager) { }

        private SettingsWindow(Builder builder, GtkUserInterface gtkUserInterface, VirtualFileSystem virtualFileSystem, HLE.FileSystem.Content.ContentManager contentManager) 
            : base(builder.GetObject("SettingsWindow").Handle)
        {
            builder.Autoconnect(this);
            
            _gtkUserInterface  = gtkUserInterface;
            this.Icon          = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");

            _addDirectory.Clicked         += AddDirectory_Clicked;
            _removeDirectory.Clicked      += RemoveDirectory_Clicked;
            _customThemeToggle.Toggled    += CustomThemeToggle_Activated;
            _browseThemePath.Clicked      += BrowseThemePath_Clicked;
            _openLogsFolderButton.Clicked += OpenLogsFolder_Clicked;
            _saveButton.Clicked           += SaveButton_Clicked;
            _closeButton.Clicked          += (sender, args) => this.Dispose();
            _configureController1.Clicked += (sender, args) => ConfigureController_Clicked(PlayerIndex.Player1);
            _configureController2.Clicked += (sender, args) => ConfigureController_Clicked(PlayerIndex.Player2);
            _configureController3.Clicked += (sender, args) => ConfigureController_Clicked(PlayerIndex.Player3);
            _configureController4.Clicked += (sender, args) => ConfigureController_Clicked(PlayerIndex.Player4);
            _configureController5.Clicked += (sender, args) => ConfigureController_Clicked(PlayerIndex.Player5);
            _configureController6.Clicked += (sender, args) => ConfigureController_Clicked(PlayerIndex.Player6);
            _configureController7.Clicked += (sender, args) => ConfigureController_Clicked(PlayerIndex.Player7);
            _configureController8.Clicked += (sender, args) => ConfigureController_Clicked(PlayerIndex.Player8);
            _configureControllerH.Clicked += (sender, args) => ConfigureController_Clicked(PlayerIndex.Handheld);
            _resScaleCombo.Changed        += (sender, args) => _resScaleText.Visible = _resScaleCombo.ActiveId == "-1";

            if (ConfigurationState.Instance.Logger.EnableFileLog)
            {
                _fileLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableError)
            {
                _errorLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableWarn)
            {
                _warningLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableInfo)
            {
                _infoLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableStub)
            {
                _stubLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableDebug)
            {
                _debugLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableGuest)
            {
                _guestLogToggle.Click();
            }

            if (ConfigurationState.Instance.Logger.EnableFsAccessLog)
            {
                _fsAccessLogToggle.Click();
            }

            if (ConfigurationState.Instance.System.EnableDockedMode)
            {
                _dockedModeToggle.Click();
            }

            if (ConfigurationState.Instance.EnableDiscordIntegration)
            {
                _discordToggle.Click();
            }

            if (ConfigurationState.Instance.Graphics.EnableVsync)
            {
                _vSyncToggle.Click();
            }

            if (ConfigurationState.Instance.System.EnableMulticoreScheduling)
            {
                _multiSchedToggle.Click();
            }

            if (ConfigurationState.Instance.System.EnablePtc)
            {
                _ptcToggle.Click();
            }

            if (ConfigurationState.Instance.System.EnableFsIntegrityChecks)
            {
                _fsicToggle.Click();
            }

            if (ConfigurationState.Instance.System.IgnoreMissingServices)
            {
                _ignoreToggle.Click();
            }

            if (ConfigurationState.Instance.Hid.EnableKeyboard)
            {
                _directKeyboardAccess.Click();
            }

            if (ConfigurationState.Instance.Ui.EnableCustomTheme)
            {
                _customThemeToggle.Click();
            }

            TimeZoneContentManager timeZoneContentManager = new TimeZoneContentManager();

            timeZoneContentManager.InitializeInstance(virtualFileSystem, contentManager, LibHac.FsSystem.IntegrityCheckLevel.None);

            List<string> locationNames = timeZoneContentManager.LocationNameCache.ToList();

            locationNames.Sort();

            foreach (string locationName in locationNames)
            {
                _systemTimeZoneSelect.Append(locationName, locationName);
            }

            _audioBackendSelect.Append(AudioBackend.Dummy.ToString(), AudioBackend.Dummy.ToString());
            if (SoundIoAudioOut.IsSupported)
                _audioBackendSelect.Append(AudioBackend.SoundIo.ToString(), "SoundIO");
            if (OpenALAudioOut.IsSupported)
                _audioBackendSelect.Append(AudioBackend.OpenAl.ToString(), "OpenAL");

            _systemLanguageSelect.SetActiveId(ConfigurationState.Instance.System.Language.Value.ToString());
            _systemRegionSelect.SetActiveId(ConfigurationState.Instance.System.Region.Value.ToString());
            _audioBackendSelect.SetActiveId(ConfigurationState.Instance.System.AudioBackend.Value.ToString());
            _systemTimeZoneSelect.SetActiveId(timeZoneContentManager.SanityCheckDeviceLocationName());
            _resScaleCombo.SetActiveId(ConfigurationState.Instance.Graphics.ResScale.Value.ToString());
            _anisotropy.SetActiveId(ConfigurationState.Instance.Graphics.MaxAnisotropy.Value.ToString());

            _custThemePath.Buffer.Text           = ConfigurationState.Instance.Ui.CustomThemePath;
            _resScaleText.Buffer.Text            = ConfigurationState.Instance.Graphics.ResScaleCustom.Value.ToString();
            _resScaleText.Visible                = _resScaleCombo.ActiveId == "-1";
            _graphicsShadersDumpPath.Buffer.Text = ConfigurationState.Instance.Graphics.ShadersDumpPath;
            _fsLogSpinAdjustment.Value           = ConfigurationState.Instance.System.FsGlobalAccessLogMode;
            _systemTimeOffset                    = ConfigurationState.Instance.System.SystemTimeOffset;

            _gameDirsBox.AppendColumn("", new CellRendererText(), "text", 0);
            _gameDirsBoxStore  = new ListStore(typeof(string));
            _gameDirsBox.Model = _gameDirsBoxStore;

            foreach (string gameDir in ConfigurationState.Instance.Ui.GameDirs.Value)
            {
                _gameDirsBoxStore.AppendValues(gameDir);
            }

            if (_customThemeToggle.Active == false)
            {
                _custThemePath.Sensitive      = false;
                _custThemePathLabel.Sensitive = false;
                _browseThemePath.Sensitive    = false;
            }

            //Setup system time spinners
            UpdateSystemTimeSpinners();
        }

        private void UpdateSystemTimeSpinners()
        {
            //Bind system time events
            _systemTimeYearSpin.ValueChanged   -= SystemTimeSpin_ValueChanged;
            _systemTimeMonthSpin.ValueChanged  -= SystemTimeSpin_ValueChanged;
            _systemTimeDaySpin.ValueChanged    -= SystemTimeSpin_ValueChanged;
            _systemTimeHourSpin.ValueChanged   -= SystemTimeSpin_ValueChanged;
            _systemTimeMinuteSpin.ValueChanged -= SystemTimeSpin_ValueChanged;

            //Apply actual system time + SystemTimeOffset to system time spin buttons
            DateTime systemTime = DateTime.Now.AddSeconds(_systemTimeOffset);

            _systemTimeYearSpinAdjustment.Value   = systemTime.Year;
            _systemTimeMonthSpinAdjustment.Value  = systemTime.Month;
            _systemTimeDaySpinAdjustment.Value    = systemTime.Day;
            _systemTimeHourSpinAdjustment.Value   = systemTime.Hour;
            _systemTimeMinuteSpinAdjustment.Value = systemTime.Minute;

            //Format spin buttons text to include leading zeros
            _systemTimeYearSpin.Text   = systemTime.Year.ToString("0000");
            _systemTimeMonthSpin.Text  = systemTime.Month.ToString("00");
            _systemTimeDaySpin.Text    = systemTime.Day.ToString("00");
            _systemTimeHourSpin.Text   = systemTime.Hour.ToString("00");
            _systemTimeMinuteSpin.Text = systemTime.Minute.ToString("00");

            //Bind system time events
            _systemTimeYearSpin.ValueChanged   += SystemTimeSpin_ValueChanged;
            _systemTimeMonthSpin.ValueChanged  += SystemTimeSpin_ValueChanged;
            _systemTimeDaySpin.ValueChanged    += SystemTimeSpin_ValueChanged;
            _systemTimeHourSpin.ValueChanged   += SystemTimeSpin_ValueChanged;
            _systemTimeMinuteSpin.ValueChanged += SystemTimeSpin_ValueChanged;
        }

        //Events
        private void SystemTimeSpin_ValueChanged(Object sender, EventArgs e)
        {
            int year   = _systemTimeYearSpin.ValueAsInt;
            int month  = _systemTimeMonthSpin.ValueAsInt;
            int day    = _systemTimeDaySpin.ValueAsInt;
            int hour   = _systemTimeHourSpin.ValueAsInt;
            int minute = _systemTimeMinuteSpin.ValueAsInt;

            if (!DateTime.TryParse(year + "-" + month + "-" + day + " " + hour + ":" + minute, out DateTime newTime))
            {
                UpdateSystemTimeSpinners();

                return;
            }

            newTime = newTime.AddSeconds(DateTime.Now.Second).AddMilliseconds(DateTime.Now.Millisecond);

            long systemTimeOffset = (long)Math.Ceiling((newTime - DateTime.Now).TotalMinutes) * 60L;

            if (_systemTimeOffset != systemTimeOffset)
            {
                _systemTimeOffset = systemTimeOffset;
                UpdateSystemTimeSpinners();
            }
        }

        private void AddDirectory_Clicked(object sender, EventArgs args)
        {
            if (Directory.Exists(_addGameDirBox.Buffer.Text))
            {
                _gameDirsBoxStore.AppendValues(_addGameDirBox.Buffer.Text);
            }
            else
            {
                FileChooserDialog fileChooser = new FileChooserDialog("Choose the game directory to add to the list", this, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Add", ResponseType.Accept)
                {
                    SelectMultiple = true
                };

                if (fileChooser.Run() == (int)ResponseType.Accept)
                {
                    foreach (string directory in fileChooser.Filenames)
                    {
                        bool directoryAdded = false;
                        
                        if (_gameDirsBoxStore.GetIterFirst(out TreeIter treeIter))
                        {
                            do
                            {
                                if (directory.Equals((string)_gameDirsBoxStore.GetValue(treeIter, 0)))
                                {
                                    directoryAdded = true;
                                    break;
                                }
                            }
                            while(_gameDirsBoxStore.IterNext(ref treeIter));
                        }

                        if (!directoryAdded)
                        {
                            _gameDirsBoxStore.AppendValues(directory);
                        }
                    }
                }

                fileChooser.Dispose();
            }

            _addGameDirBox.Buffer.Text = "";
        }

        private void RemoveDirectory_Clicked(object sender, EventArgs args)
        {
            TreeSelection selection = _gameDirsBox.Selection;

            if (selection.GetSelected(out TreeIter treeIter))
            {
                _gameDirsBoxStore.Remove(ref treeIter);
            }
        }

        private void CustomThemeToggle_Activated(object sender, EventArgs args)
        {
            _custThemePath.Sensitive      = _customThemeToggle.Active;
            _custThemePathLabel.Sensitive = _customThemeToggle.Active;
            _browseThemePath.Sensitive    = _customThemeToggle.Active;
        }

        private void BrowseThemePath_Clicked(object sender, EventArgs args)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Choose the theme to load", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Select", ResponseType.Accept);

            fileChooser.Filter = new FileFilter();
            fileChooser.Filter.AddPattern("*.css");

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                _custThemePath.Buffer.Text = fileChooser.Filename;
            }

            fileChooser.Dispose();
        }

        private static void OpenLogsFolder_Clicked(object sender, EventArgs args)
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            
            DirectoryInfo directory = new DirectoryInfo(logPath);
            directory.Create();
            
            Process.Start(new ProcessStartInfo
            {
                FileName        = logPath,
                UseShellExecute = true,
                Verb            = "open"
            });
        }

        private void ConfigureController_Clicked(PlayerIndex playerIndex)
        {
            ControllerWindow controllerWin = new ControllerWindow(_gtkUserInterface, playerIndex);
            controllerWin.Show();
        }

        private void SaveButton_Clicked(object sender, EventArgs args)
        {
            List<string> gameDirs = new List<string>();

            _gameDirsBoxStore.GetIterFirst(out TreeIter treeIter);
            for (int i = 0; i < _gameDirsBoxStore.IterNChildren(); i++)
            {
                gameDirs.Add((string)_gameDirsBoxStore.GetValue(treeIter, 0));

                _gameDirsBoxStore.IterNext(ref treeIter);
            }

            if (!float.TryParse(_resScaleText.Buffer.Text, out float resScaleCustom) || resScaleCustom <= 0.0f)
            {
                resScaleCustom = 1.0f;
            }

            ConfigurationState.Instance.Logger.EnableError.Value               = _errorLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableWarn.Value                = _warningLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableInfo.Value                = _infoLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableStub.Value                = _stubLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableDebug.Value               = _debugLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableGuest.Value               = _guestLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableFsAccessLog.Value         = _fsAccessLogToggle.Active;
            ConfigurationState.Instance.Logger.EnableFileLog.Value             = _fileLogToggle.Active;
            ConfigurationState.Instance.System.EnableDockedMode.Value          = _dockedModeToggle.Active;
            ConfigurationState.Instance.EnableDiscordIntegration.Value         = _discordToggle.Active;
            ConfigurationState.Instance.Graphics.EnableVsync.Value             = _vSyncToggle.Active;
            ConfigurationState.Instance.System.EnableMulticoreScheduling.Value = _multiSchedToggle.Active;
            ConfigurationState.Instance.System.EnablePtc.Value                 = _ptcToggle.Active;
            ConfigurationState.Instance.System.EnableFsIntegrityChecks.Value   = _fsicToggle.Active;
            ConfigurationState.Instance.System.IgnoreMissingServices.Value     = _ignoreToggle.Active;
            ConfigurationState.Instance.Hid.EnableKeyboard.Value               = _directKeyboardAccess.Active;
            ConfigurationState.Instance.Ui.EnableCustomTheme.Value             = _customThemeToggle.Active;
            ConfigurationState.Instance.System.Language.Value                  = Enum.Parse<Language>(_systemLanguageSelect.ActiveId);
            ConfigurationState.Instance.System.Region.Value                    = Enum.Parse<Configuration.System.Region>(_systemRegionSelect.ActiveId);
            ConfigurationState.Instance.System.AudioBackend.Value              = Enum.Parse<AudioBackend>(_audioBackendSelect.ActiveId);
            ConfigurationState.Instance.System.TimeZone.Value                  = _systemTimeZoneSelect.ActiveId;
            ConfigurationState.Instance.System.SystemTimeOffset.Value          = _systemTimeOffset;
            ConfigurationState.Instance.Ui.CustomThemePath.Value               = _custThemePath.Buffer.Text;
            ConfigurationState.Instance.Graphics.ShadersDumpPath.Value         = _graphicsShadersDumpPath.Buffer.Text;
            ConfigurationState.Instance.Ui.GameDirs.Value                      = gameDirs;
            ConfigurationState.Instance.System.FsGlobalAccessLogMode.Value     = (int)_fsLogSpinAdjustment.Value;
            ConfigurationState.Instance.Graphics.MaxAnisotropy.Value           = float.Parse(_anisotropy.ActiveId);
            ConfigurationState.Instance.Graphics.ResScale.Value                = int.Parse(_resScaleCombo.ActiveId);
            ConfigurationState.Instance.Graphics.ResScaleCustom.Value          = resScaleCustom;

            MainWindow.SaveConfig();
            _gtkUserInterface.InvokeGraphicsConfigUpdated();
            MainWindow.ApplyTheme();
            Dispose();
        }
    }
}
