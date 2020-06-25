using Gtk;
using Ryujinx.Common;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ryujinx.Ui
{
    public class AboutWindow : Window
    {
#pragma warning disable CS0649, IDE0044
        [Builder.Object] Label    _versionText;
        [Builder.Object] Image    _ryujinxLogo;
        [Builder.Object] EventBox _ryujinxEventBox;
        [Builder.Object] Image    _patreonLogo;
        [Builder.Object] EventBox _patreonEventBox;
        [Builder.Object] Image    _gitHubLogo;
        [Builder.Object] EventBox _gitHubEventBox;
        [Builder.Object] Image    _discordLogo;
        [Builder.Object] EventBox _discordEventBox;
        [Builder.Object] Image    _twitterLogo;
        [Builder.Object] EventBox _twitterEventBox;
        [Builder.Object] EventBox _contributorsEventBox;
#pragma warning restore CS0649, IDE0044

        public AboutWindow() : this(new Builder("Ryujinx.Ui.AboutWindow.glade")) { }

        private AboutWindow(Builder builder) : base(builder.GetObject("AboutWindow").Handle)
        {
            builder.Autoconnect(this);

            this.Icon           = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");
            _ryujinxLogo.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png", 100, 100);
            _patreonLogo.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.PatreonLogo.png", 30, 30);
            _gitHubLogo.Pixbuf  = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.GitHubLogo.png",  30, 30);
            _discordLogo.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.DiscordLogo.png", 30, 30);
            _twitterLogo.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.TwitterLogo.png", 30, 30);

            _versionText.Text = Constants.Version;

            _ryujinxEventBox.ButtonPressEvent      += RyujinxButton_Pressed;
            _patreonEventBox.ButtonPressEvent      += PatreonButton_Pressed;
            _gitHubEventBox.ButtonPressEvent       += GitHubButton_Pressed;
            _discordEventBox.ButtonPressEvent      += DiscordButton_Pressed;
            _twitterEventBox.ButtonPressEvent      += TwitterButton_Pressed;
            _contributorsEventBox.ButtonPressEvent += ContributorsButton_Pressed;
        }

        private static void OpenUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }

        private static void RyujinxButton_Pressed(object sender, ButtonPressEventArgs args)
        {
            OpenUrl("https://ryujinx.org");
        }

        private static void PatreonButton_Pressed(object sender, ButtonPressEventArgs args)
        {
            OpenUrl("https://www.patreon.com/ryujinx");
        }

        private static void GitHubButton_Pressed(object sender, ButtonPressEventArgs args)
        {
            OpenUrl("https://github.com/Ryujinx/Ryujinx");
        }

        private static void DiscordButton_Pressed(object sender, ButtonPressEventArgs args)
        {
            OpenUrl("https://discordapp.com/invite/N2FmfVc");
        }

        private static void TwitterButton_Pressed(object sender, ButtonPressEventArgs args)
        {
            OpenUrl("https://twitter.com/RyujinxEmu");
        }

        private static void ContributorsButton_Pressed(object sender, ButtonPressEventArgs args)
        {
            OpenUrl("https://github.com/Ryujinx/Ryujinx/graphs/contributors?type=a");
        }
    }
}