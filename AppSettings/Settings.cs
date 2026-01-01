using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;

namespace AppSettings
{
    public class Settings
    {
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
                }
            }
        }

        private StorageFile? configFile;

        [JsonIgnore]
        public bool configHasLoaded { get; private set; } = false;

        private Task? savingTask;

        public Settings() { }

        public void SaveSettingsSafe()
        {
            if (configHasLoaded && savingTask is null)
            {
                savingTask = SaveSettings();
                savingTask.ContinueWith(_ => savingTask = null);
            }
        }

        private async Task SaveSettings()
        {
            using (Stream opStream = await configFile!.OpenStreamForWriteAsync())
                JsonSerializer.Serialize(opStream, this, SettingsJsonContext.Default.Settings);
        }

        public static async Task<Settings> LoadSettingsFromFile()
        {
            Settings settings;

            StorageFile configFile = await ApplicationData.Current.RoamingFolder.CreateFileAsync("config.json", CreationCollisionOption.OpenIfExists);

            if ((new FileInfo(configFile.Path).Length) > 0)
                using (Stream ipStream = await configFile.OpenStreamForReadAsync())
                    settings = SettingsConfigUpgrader.UpgradeToLatest(
                        JsonSerializer.Deserialize(ipStream, SettingsJsonContext.Default.Settings)!
                        );
            else
                settings = new Settings
                {
                    inputDevices = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen,
                };
            
            settings.configFile = configFile;
            settings.configHasLoaded = true;

            return settings;
        }
    }

    [JsonSerializable(typeof(Settings))]
    internal partial class SettingsJsonContext : JsonSerializerContext { }

    public struct AppSettingsData
    {
        public CoreInputDeviceTypes inputDevices;
    }
}
