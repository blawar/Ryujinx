using Gtk;
using System;
using System.Reflection;

namespace Ryujinx.Ui
{
    public class ProfileDialog : Dialog
    {
        public string FileName { get; private set; }

#pragma warning disable CS0649, IDE0044
        [Builder.Object] Entry  _profileEntry;
        [Builder.Object] Label  _errorMessage;
        [Builder.Object] Button _okButton;
        [Builder.Object] Button _cancelButton;
#pragma warning restore CS0649, IDE0044

        public ProfileDialog() : this(new Builder("Ryujinx.Ui.ProfileDialog.glade")) { }

        private ProfileDialog(Builder builder) : base(builder.GetObject("ProfileDialog").Handle)
        {
            builder.Autoconnect(this);

            Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");

            _okButton.Clicked     += OkButton_Clicked;
            _cancelButton.Clicked += (sender, args) => Respond(ResponseType.Cancel);
        }

        private void OkButton_Clicked(object sender, EventArgs args)
        {
            bool validFileName = true;

            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                if (_profileEntry.Text.Contains(invalidChar))
                {
                    validFileName = false;
                }
            }

            if (validFileName && !string.IsNullOrEmpty(_profileEntry.Text))
            {
                FileName = $"{_profileEntry.Text}.json";

                Respond(ResponseType.Ok);
            }
            else
            {
                _errorMessage.Text = "The file name contains invalid characters. Please try again.";
            }
        }
    }
}