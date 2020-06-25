using System;

namespace Ryujinx.Common.Ui
{
    public interface IUserInterface
    {
        public event EventHandler<LoadTitleEventArgs> LoadTitleEvent;
        public event EventHandler EndEvent;
        public event EventHandler GraphicsConfigUpdated;

        public void Run(string[] args);
        public void ShowInfoDialog(string mainText, string secondaryText);
        public void ShowWarningDialog(string mainText, string secondaryText);
        public void ShowErrorDialog(string errorMessage);
        public bool ShowConfirmationDialog(string mainText, string secondaryText);
    }

    public class LoadTitleEventArgs : EventArgs
    {
        public string Path;
        public Action<dynamic> CreateRenderWindow;
    }
}