using ARMeilleure.Translation.PTC;
using OpenTK;
using Ryujinx.Audio;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Common.SystemInfo;
using Ryujinx.Common.Ui;
using Ryujinx.Configuration;
using Ryujinx.Debugger.Profiler;
using Ryujinx.HLE;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.FileSystem.Content;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu;
using Ryujinx.Ui;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ryujinx
{
    class Program
    {
        private static VirtualFileSystem _virtualFileSystem;
        private static ContentManager    _contentManager;

        static void Main(string[] cmdArgs)
        {
            Toolkit.Init(new ToolkitOptions
            {
                Backend = PlatformBackend.PreferNative,
                EnableHighResolution = true
            });

            string version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            Console.Title = $"Ryujinx Console {version}";

            string systemPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")};{systemPath}");

            // Initialize the configuration
            ConfigurationState.Initialize();

            // Initialize the logger system
            LoggerModule.Initialize();

            // Initialize Discord integration
            DiscordIntegrationModule.Initialize();

            string localConfigurationPath  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");
            string globalBasePath          = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ryujinx");
            string globalConfigurationPath = Path.Combine(globalBasePath, "Config.json");

            // Now load the configuration as the other subsystems are now registered
            if (File.Exists(localConfigurationPath))
            {
                ConfigurationState.ConfigurationPath = localConfigurationPath;
            }
            else if (File.Exists(globalConfigurationPath))
            {
                ConfigurationState.ConfigurationPath = globalConfigurationPath;
            }

            if (ConfigurationState.ConfigurationPath != null)
            {
                ConfigurationFileFormat configurationFileFormat = ConfigurationFileFormat.Load(ConfigurationState.ConfigurationPath);

                ConfigurationState.Instance.Load(configurationFileFormat);
            }
            else
            {
                // No configuration, we load the default values and save it on disk
                ConfigurationState.ConfigurationPath = globalConfigurationPath;

                // Make sure to create the Ryujinx directory if needed.
                Directory.CreateDirectory(globalBasePath);

                ConfigurationState.Instance.LoadDefault();
                ConfigurationState.Instance.ToFileFormat().SaveConfig(globalConfigurationPath);
            }

            Logger.PrintInfo(LogClass.Application, $"Ryujinx Version: {version}");
            Logger.PrintInfo(LogClass.Application, $"Operating System: {SystemInfo.Instance.OsDescription}");
            Logger.PrintInfo(LogClass.Application, $"CPU: {SystemInfo.Instance.CpuName}");
            Logger.PrintInfo(LogClass.Application, $"Total RAM: {SystemInfo.Instance.RamSizeInMB}");

            Profile.Initialize();

            _virtualFileSystem = VirtualFileSystem.CreateInstance();
            _contentManager    = new ContentManager(_virtualFileSystem);
            _virtualFileSystem.Reload();

            IUserInterface userInterface = new GtkUserInterface(_virtualFileSystem, _contentManager);

            userInterface.EndEvent              += (sender, args) => End();
            userInterface.LoadTitleEvent        += (sender, args) => LoadTitle(args.Path, args.CreateRenderWindow);
            userInterface.GraphicsConfigUpdated += (sender, args) => UpdateGraphicsConfig();
            userInterface.Run(cmdArgs);
        }

        internal static void LoadTitle(string path, Action<dynamic> createRenderWindow)
        {
            _virtualFileSystem.Reload();

            Switch device = new Switch(_virtualFileSystem, _contentManager, InitializeRenderer(), InitializeAudioEngine());
            device.Initialize();

            UpdateGraphicsConfig();

            Logger.PrintInfo(LogClass.Application, $"Using Firmware Version: {_contentManager.GetCurrentFirmwareVersion()?.VersionString}");
            Logger.RestartTime();

            if (Directory.Exists(path))
            {
                string[] romFsFiles = Directory.GetFiles(path, "*.istorage");

                if (romFsFiles.Length == 0)
                {
                    romFsFiles = Directory.GetFiles(path, "*.romfs");
                }

                if (romFsFiles.Length > 0)
                {
                    Logger.PrintInfo(LogClass.Application, "Loading as cart with RomFS.");
                    device.LoadCart(path, romFsFiles[0]);
                }
                else
                {
                    Logger.PrintInfo(LogClass.Application, "Loading as cart WITHOUT RomFS.");
                    device.LoadCart(path);
                }
            }
            else if (File.Exists(path))
            {
                switch (Path.GetExtension(path).ToLowerInvariant())
                {
                    case ".xci":
                        Logger.PrintInfo(LogClass.Application, "Loading as XCI.");
                        device.LoadXci(path);
                        break;
                    case ".nca":
                        Logger.PrintInfo(LogClass.Application, "Loading as NCA.");
                        device.LoadNca(path);
                        break;
                    case ".nsp":
                    case ".pfs0":
                        Logger.PrintInfo(LogClass.Application, "Loading as NSP.");
                        device.LoadNsp(path);
                        break;
                    default:
                        Logger.PrintInfo(LogClass.Application, "Loading as homebrew.");
                        try
                        {
                            device.LoadProgram(path);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Logger.PrintError(LogClass.Application, "The file which you have specified is unsupported by Ryujinx.");
                        }
                        break;
                }
            }
            else
            {
                Logger.PrintWarning(LogClass.Application, "Please specify a valid XCI/NCA/NSP/PFS0/NRO file.");
                device.Dispose();

                return;
            }

            device.Hid.Npads.AddControllers(ConfigurationState.Instance.Hid.InputConfig.Value.Select(inputConfig =>
                new ControllerConfig
                {
                    Player = (PlayerIndex)inputConfig.PlayerIndex,
                    Type   = (ControllerType)inputConfig.ControllerType
                }
            ).ToArray());

            DiscordIntegrationModule.SwitchToPlayingState(device.Application.TitleIdText, device.Application.TitleName);

            createRenderWindow(device);
        }

        private static IRenderer InitializeRenderer()
        {
            return new Graphics.OpenGL.Renderer();
        }

        /// <summary>
        /// Picks an <see cref="IAalOutput"/> audio output renderer supported on this machine
        /// </summary>
        /// <returns>An <see cref="IAalOutput"/> supported by this machine</returns>
        private static IAalOutput InitializeAudioEngine()
        {
            if (OpenALAudioOut.IsSupported)
            {
                return new OpenALAudioOut();
            }
            else if (SoundIoAudioOut.IsSupported)
            {
                return new SoundIoAudioOut();
            }
            else
            {
                return new DummyAudioOut();
            }
        }

        private static void UpdateGraphicsConfig()
        {
            int   resScale       = ConfigurationState.Instance.Graphics.ResScale;
            float resScaleCustom = ConfigurationState.Instance.Graphics.ResScaleCustom;

            GraphicsConfig.ResScale        = (resScale == -1) ? resScaleCustom : resScale;
            GraphicsConfig.MaxAnisotropy   = ConfigurationState.Instance.Graphics.MaxAnisotropy;
            GraphicsConfig.ShadersDumpPath = ConfigurationState.Instance.Graphics.ShadersDumpPath;
        }

        private static void End()
        {
            Profile.FinishProfiling();
            DiscordIntegrationModule.Exit();
            Logger.Shutdown();

            Ptc.Dispose();
            PtcProfiler.Dispose();
        }
    }
}