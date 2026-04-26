using Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace AppSettings
{
    public class Settings
    {
        [JsonInclude]
        public long configVersion;

        private CoreInputDeviceTypes _inpDev;
        public CoreInputDeviceTypes inputDevices
        {
            get => _inpDev;
            set
            {
                if (_inpDev != value)
                {
                    _inpDev = value;
                    if (configHasLoaded)
                        RequestSave();
                }
            }
        }

        private ObservableCollection<Color> _drawingColors;
        public ObservableCollection<Color> drawingColors
        {
            get => _drawingColors;
            set
            {
                if (_drawingColors != value)
                {
                    _drawingColors = value;
                    _drawingColors.CollectionChanged += (s, e) => RequestSave();
                    if (configHasLoaded)
                        RequestSave();
                }
            }
        }

        private double _tipSize;
        public double tipSize
        {
            get => _tipSize;
            set
            {
                if (_tipSize != value)
                {
                    _tipSize = value;
                    if (configHasLoaded)
                        RequestSave();
                }
            }
        }

        private HomeScreenThumbnailSize _homescreenThumbnailSize;
        public HomeScreenThumbnailSize homescreenThumbnailSize
        {
            get => _homescreenThumbnailSize;
            set
            {
                if (_homescreenThumbnailSize != value)
                {
                    _homescreenThumbnailSize = value;
                    if (configHasLoaded)
                        RequestSave();
                }
            }
        }

        private StorageFile? configFile;

        [JsonIgnore]
        public bool configHasLoaded { get; private set; } = false;

        private CancellationTokenSource? _cts;

        public Settings()
        {
            configVersion = 0;
            inputDevices = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen;
            _drawingColors = new ObservableCollection<Color>();
        }

        public void RequestSave()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000, token);
                    if (!token.IsCancellationRequested)
                        await SaveSettings();
                } catch (TaskCanceledException) { }
            });
        }

        public async Task Flush()
        {
            _cts?.Cancel();
            _cts = null;
            await SaveSettings();
        }

        private async Task SaveSettings()
        {
            configFile = await ApplicationData.Current.RoamingFolder.CreateFileAsync("config.json", CreationCollisionOption.ReplaceExisting);
            using (Stream opStream = await configFile!.OpenStreamForWriteAsync())
                JsonSerializer.Serialize(opStream, this, SettingsJsonContext.Default.Settings);
        }

        public static async Task<Settings> LoadSettingsFromFile()
        {
            Settings settings;

            StorageFile configFile = await ApplicationData.Current.RoamingFolder.CreateFileAsync("config.json", CreationCollisionOption.OpenIfExists);
            bool configWasPresent = true;

            if ((new FileInfo(configFile.Path).Length) > 0)
                using (Stream ipStream = await configFile.OpenStreamForReadAsync())
                    settings = SettingsConfigUpgrader.UpgradeToLatest(
                        JsonSerializer.Deserialize(ipStream, SettingsJsonContext.Default.Settings)!
                        );
            else
            {
                settings = SettingsConfigUpgrader.UpgradeToLatest(new Settings());
                configWasPresent = false;
            }

            settings.drawingColors.CollectionChanged += (s, e) => settings.RequestSave();
            
            settings.configFile = configFile;
            settings.configHasLoaded = true;
            if (!configWasPresent)
                settings.RequestSave();

            return settings;
        }

        public void LoadColorsIntoStackPanel(SimpleColorPicker panel, ChangeColorEvent method, SimpleColorPicker parent)
        {
            panel.Children.Clear();
            foreach (Color color in this.drawingColors)
            {
                ColorPickerButton button = new ColorPickerButton(new Windows.UI.Xaml.Media.SolidColorBrush(color), parent);
                button.RemoveColor += (s, e) =>
                {
                    this.drawingColors.Remove(s.Fill.Color);
                    panel.Children.Remove(s);
                    panel.UpdateButtonIndices();
                };
                button.ChangeColor += method;
                panel.Children.Add(button);
            }
            panel.UpdateButtonIndices();
        }

        public void GetHomescreenThumbnailSizeAllowedWidths(out double minWidth, out double maxWidth)
        {
            if (homescreenThumbnailSize == HomeScreenThumbnailSize.Small)
            {
                minWidth = 216d;
                maxWidth = 360d;
            } else if (homescreenThumbnailSize == HomeScreenThumbnailSize.Medium)
            {
                minWidth = 324d;
                maxWidth = 540d;
            } else
            {
                minWidth = 432d;
                maxWidth = 720d;
            }
        }
    }

    [JsonSerializable(typeof(Settings))]
    internal partial class SettingsJsonContext : JsonSerializerContext { }

    public enum HomeScreenThumbnailSize
    {
        Small,
        Medium,
        Large,
    }
}
