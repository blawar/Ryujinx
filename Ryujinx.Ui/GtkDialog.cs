using Gtk;
using System.Reflection;

namespace Ryujinx.Ui
{
    internal class GtkDialog : MessageDialog
    {
        internal GtkDialog(string title, string mainText, string secondaryText,
            MessageType messageType = MessageType.Other, ButtonsType buttonsType = ButtonsType.Ok) : base(null, DialogFlags.Modal, messageType, buttonsType, null)
        {
            Title          = title;
            Icon           = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");
            Text           = mainText;
            SecondaryText  = secondaryText;
            WindowPosition = WindowPosition.Center;
            Response      += (sender, args) => this.Dispose();

            SetSizeRequest(100, 20);
        }
    }
}