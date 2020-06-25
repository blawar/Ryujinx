using ARMeilleure.Translation.PTC;
using Gtk;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Ui;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.FileSystem.Content;
using System;
using System.IO;

namespace Ryujinx.Ui
{
    public class GtkUserInterface : IUserInterface
    {
        public event EventHandler<LoadTitleEventArgs> LoadTitleEvent;
        public event EventHandler EndEvent;
        public event EventHandler RefreshTitleListEvent;
        public event EventHandler GraphicsConfigUpdated;

        private bool _isConfirmationDialogOpen;
        private readonly VirtualFileSystem _virtualFileSystem;
        private readonly ContentManager    _contentManager;

        public GtkUserInterface(VirtualFileSystem virtualFileSystem, ContentManager contentManager)
        {
            _virtualFileSystem = virtualFileSystem;
            _contentManager    = contentManager;

            GLib.ExceptionManager.UnhandledException += Glib_UnhandledException;
        }

        public void Run(string[] args)
        {
            Application.Init();

            MainWindow mainWindow = new MainWindow(this, _virtualFileSystem, _contentManager);
            mainWindow.Show();

            mainWindow.Destroyed += (sender, eventArgs) =>
            {
                Application.Quit();
                EndEvent?.Invoke(this, EventArgs.Empty);
            };

            RefreshTitleListEvent += (sender, eventArgs) => mainWindow.UpdateGameTable();

            string globalProdKeysPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ryujinx", "system", "prod.keys");
            string userProfilePath    = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch", "prod.keys");
            if (!File.Exists(globalProdKeysPath) && !File.Exists(userProfilePath) && !Migration.IsMigrationNeeded())
            {
                ShowWarningDialog("Key file was not found", "Please refer to `KEYS.md` for more info");
            }

            if (args.Length == 1)
            {
                mainWindow.LoadApplication(args[0]);
            }

            Application.Run();
        }

        public void ShowInfoDialog(string mainText, string secondaryText) =>
            new GtkDialog("Ryujinx - Info", mainText, secondaryText, MessageType.Info).Run();

        public void ShowWarningDialog(string mainText, string secondaryText) =>
            new GtkDialog("Ryujinx - Warning", mainText, secondaryText, MessageType.Warning).Run();

        public void ShowErrorDialog(string errorMessage) =>
            new GtkDialog("Ryujinx - Error", "Ryujinx has encountered an error", errorMessage, MessageType.Error).Run();

        public bool ShowConfirmationDialog(string mainText, string secondaryText)
        {
            if (_isConfirmationDialogOpen)
                return false;

            _isConfirmationDialogOpen = true;
            ResponseType response     = (ResponseType)new GtkDialog("Ryujinx - Confirmation", mainText, secondaryText, MessageType.Question, ButtonsType.YesNo).Run();
            _isConfirmationDialogOpen = false;

            return response == ResponseType.Yes;
        }

        internal void InvokeLoadTitleEvent(string path, Action<dynamic> createRenderWindow) => 
            LoadTitleEvent?.Invoke(this, new LoadTitleEventArgs { Path = path, CreateRenderWindow = createRenderWindow });

        internal void InvokeRefreshTitleListEvent() =>
            RefreshTitleListEvent?.Invoke(this, EventArgs.Empty);

        internal void InvokeGraphicsConfigUpdated() =>
            GraphicsConfigUpdated?.Invoke(this, EventArgs.Empty);

        private static void Glib_UnhandledException(GLib.UnhandledExceptionArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;

            Logger.PrintError(LogClass.Application, $"Unhandled exception caught: {exception}");

            Ptc.Close();
            PtcProfiler.Stop();

            if (e.IsTerminating)
            {
                Logger.Shutdown();

                Ptc.Dispose();
                PtcProfiler.Dispose();
            }
        }
    }
}