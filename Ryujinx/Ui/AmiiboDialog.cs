using Gtk;
using Ryujinx.Common;
using Ryujinx.HLE.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ryujinx.Ui
{
    public class AmiiboDialog : Dialog
    {
        public string AmiiboId { get; private set; }

        private readonly HttpClient _httpClient;
        private readonly string _amiiboJsonPath;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly byte[] _amiiboLogoBytes;
        private List<Amiibo> _amiiboList;

#pragma warning disable CS0649, IDE0044
        [Builder.Object] Image        _preview;
        [Builder.Object] ComboBoxText _seriesComboBox;
        [Builder.Object] ComboBoxText _characterComboBox;
#pragma warning restore CS0649, IDE0044

        public AmiiboDialog(VirtualFileSystem virtualFileSystem) : this(new Builder("Ryujinx.Ui.AmiiboDialog.glade"), virtualFileSystem) { }

        private AmiiboDialog(Builder builder, VirtualFileSystem virtualFileSystem) : base(builder.GetObject("_amiiboDialog").Handle)
        {
            builder.Autoconnect(this);

            this.Icon       = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");
            _httpClient     = new HttpClient();
            _amiiboJsonPath = System.IO.Path.Join(virtualFileSystem.GetBasePath(), "Amiibo.json");
            _amiiboList     = new List<Amiibo>();

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                DictionaryKeyPolicy  = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _amiiboLogoBytes = EmbeddedResources.Read("Ryujinx/Ui/assets/AmiiboLogo.png");
            _preview.Pixbuf  = new Gdk.Pixbuf(_amiiboLogoBytes);

            _ = LoadContentAsync();
        }

        private async Task LoadContentAsync()
        {
            string amiiboJsonString = await AmiiboJsonExists() ? File.ReadAllText(_amiiboJsonPath) : await DownloadAmiiboJson();
            _amiiboList = JsonSerializer.Deserialize<AmiiboJson>(amiiboJsonString, _jsonSerializerOptions).Amiibo;

            foreach (string series in _amiiboList.Select(amiibo => amiibo.AmiiboSeries).Distinct())
            {
                _seriesComboBox.Append(series, series);
            }

            _seriesComboBox.Changed    += SeriesComboBox_Changed;
            _characterComboBox.Changed += CharacterComboBox_Changed;

            _seriesComboBox.Active = 0;
        }

        private async Task<bool> AmiiboJsonExists()
        {
            if (!File.Exists(_amiiboJsonPath))
                return false;

            if (!NetworkInterface.GetIsNetworkAvailable())
                return true;

            string   lastUpdatedJson = await _httpClient.GetStringAsync("https://www.amiiboapi.com/api/lastupdated/");
            DateTime lastUpdated     = JsonSerializer.Deserialize<LastUpdatedJson>(lastUpdatedJson, _jsonSerializerOptions).LastUpdated;
            int      comparison      = lastUpdated.CompareTo(File.GetLastWriteTime(_amiiboJsonPath));

            return comparison <= 0;
        }

        private async Task<string> DownloadAmiiboJson()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return "{ \"amiibo\": [] }";

            string amiiboJsonString = await _httpClient.GetStringAsync("https://www.amiiboapi.com/api/amiibo/");

            using (FileStream dlcJsonStream = File.Create(_amiiboJsonPath, 4096, FileOptions.WriteThrough))
            {
                dlcJsonStream.Write(Encoding.UTF8.GetBytes(amiiboJsonString));
            }

            return amiiboJsonString;
        }

        private async Task UpdateAmiiboPreview(string imageUrl)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            byte[] previewBytes = await _httpClient.GetByteArrayAsync(imageUrl);
            _preview.Pixbuf     = new Gdk.Pixbuf(previewBytes);
        }

        //Events
        private void SeriesComboBox_Changed(object sender, EventArgs args)
        {
            _characterComboBox.Changed -= CharacterComboBox_Changed;

            _characterComboBox.RemoveAll();

            foreach (Amiibo amiibo in _amiiboList.Where(amiibo => amiibo.AmiiboSeries == _seriesComboBox.ActiveId))
            {
                _characterComboBox.Append(amiibo.Head + amiibo.Tail, amiibo.Character);
            }

            _characterComboBox.Changed += CharacterComboBox_Changed;

            _characterComboBox.Active = 0;
        }

        private void CharacterComboBox_Changed(object sender, EventArgs args)
        {
            AmiiboId = _characterComboBox.ActiveId;
            _preview.Pixbuf = new Gdk.Pixbuf(_amiiboLogoBytes);
            string imageUrl = _amiiboList.FirstOrDefault(amiibo => amiibo.Head + amiibo.Tail == _characterComboBox.ActiveId).Image;

            _ = UpdateAmiiboPreview(imageUrl);
        }

        private void ScanButton_Pressed(object sender, EventArgs args)
        {
            Respond(ResponseType.Accept);
        }

        private void CloseButton_Pressed(object sender, EventArgs args)
        {
            Respond(ResponseType.Cancel);
        }

        protected override void Dispose(bool disposing)
        {
            _httpClient.Dispose();

            base.Dispose(disposing);
        }
    }

    public struct LastUpdatedJson
    {
        public DateTime LastUpdated { get; set; }
    }

    public struct AmiiboJson
    {
        public List<Amiibo> Amiibo { get; set; }
    }

    public struct Amiibo
    {
        public string Character    { get; set; }
        public string Head         { get; set; }
        public string Tail         { get; set; }
        public string Image        { get; set; }
        public string AmiiboSeries { get; set; }

        // NOTE: These properties exist in the JSON but are never used.

        //public string GameSeries { get; set; }
        //public string Name       { get; set; }
        //public string Type       { get; set; }
        //public Dictionary<string, string> Release { get; set; }
    }
}