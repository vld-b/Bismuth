using ABI.System.Numerics;
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
using Windows.UI.Xaml;
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

        private ObservableCollection<Color> _highlightColors;
        public ObservableCollection<Color> highlightColors
        {
            get => _highlightColors;
            set
            {
                if (_highlightColors != value)
                {
                    _highlightColors = value;
                    _highlightColors.CollectionChanged += (s, e) => RequestSave();
                    if (configHasLoaded)
                        RequestSave();
                }
            }
        }

        private ObservableCollection<Color> _pencilColors;
        public ObservableCollection<Color> pencilColors
        {
            get => _pencilColors;
            set
            {
                if (_pencilColors != value)
                {
                    _pencilColors = value;
                    _pencilColors.CollectionChanged += (s, e) => RequestSave();
                    if (configHasLoaded)
                        RequestSave();
                }
            }
        }

        private ObservableCollection<Color> _calligraphyColors;
        public ObservableCollection<Color> calligraphyColors
        {
            get => _calligraphyColors;
            set
            {
                if (_calligraphyColors != value)
                {
                    _calligraphyColors = value;
                    _calligraphyColors.CollectionChanged += (s, e) => RequestSave();
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

        private double _highlighterTipSize;
        public double highlightTipSize
        {
            get => _highlighterTipSize;
            set
            {
                if (_highlighterTipSize != value)
                {
                    _highlighterTipSize = value;
                    if (configHasLoaded)
                        RequestSave();
                }
            }
        }

        private double _pencilTipSize;
        public double pencilTipSize
        {
            get => _pencilTipSize;
            set
            {
                if (_pencilTipSize != value)
                {
                    _pencilTipSize = value;
                    if (configHasLoaded)
                        RequestSave();
                }
            }
        }

        private double _calligraphyTipSize;
        public double calligraphyTipSize
        {
            get => _calligraphyTipSize;
            set
            {
                if (_calligraphyTipSize != value)
                {
                    _calligraphyTipSize = value;
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

        private UndoRedoButtonsPlacement _undoRedoButtonsPlacement;
        public UndoRedoButtonsPlacement undoRedoButtonsPlacement
        {
            get => _undoRedoButtonsPlacement;
            set
            {
                if (_undoRedoButtonsPlacement != value)
                {
                    _undoRedoButtonsPlacement = value;
                    if (configHasLoaded)
                        RequestSave();
                }
            }
        }

        private StorageFile? configFile;

        [JsonIgnore]
        public bool configHasLoaded { get; private set; } = false;

        private DispatcherTimer? saveTimer;
        private CancellationTokenSource ctsLoadingColors = new CancellationTokenSource();
        private Task? loadingColorsTask;

        public Settings()
        {
            configVersion = 0;
            inputDevices = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen;
            _drawingColors = new ObservableCollection<Color>();
            _highlightColors = new ObservableCollection<Color>();
            _pencilColors = new ObservableCollection<Color>();
            _calligraphyColors = new ObservableCollection<Color>();
        }

        public void RequestSave()
        {
            if (saveTimer is null)
            {
                saveTimer = new DispatcherTimer();
                saveTimer.Interval = TimeSpan.FromSeconds(1);
                saveTimer.Tick += async (s, e) =>
                {
                    saveTimer.Stop();
                    saveTimer = null;
                    await SaveSettings();
                };
            }
            saveTimer.Stop();
            saveTimer.Start();
        }

        public async Task Flush()
        {
            if (saveTimer is null)
                await SaveSettings();
            else
            {
                saveTimer.Stop();
                saveTimer = null;
                await SaveSettings();
            }
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

        public async Task LoadColorsIntoStackPanel(
            SimpleColorPicker panel,
            ChangeColorEvent onChangeColor,
            SimpleColorPicker parent,
            ColorPalette palette,
            CurrentlySelectedColors currentColors
            )
        {
            ctsLoadingColors.Cancel();
            if (loadingColorsTask is not null)
                await loadingColorsTask;
            ctsLoadingColors = new CancellationTokenSource();
            loadingColorsTask = LoadColorsIntoStackPanelCancellable(panel, onChangeColor, parent, palette, currentColors, ctsLoadingColors.Token);
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

        private async Task LoadColorsIntoStackPanelCancellable(
            SimpleColorPicker panel,
            ChangeColorEvent onChangeColor,
            SimpleColorPicker parent,
            ColorPalette palette,
            CurrentlySelectedColors currentColors,
            CancellationToken ct
            )
        {
            for (int i = panel.Children.Count - 1; i >= 0; --i)
            {
                ColorPickerButton btn = (ColorPickerButton)panel.Children[i];
                btn.AnimateScale(0f);
                await Task.Delay(40);
                panel.Children.RemoveAt(i);
            }

            int selectedButton;
            ObservableCollection<Color> colorsToLoad;
            switch (palette)
            {
                case ColorPalette.Drawing:
                    colorsToLoad = drawingColors;
                    selectedButton = currentColors.drawing;
                    break;
                case ColorPalette.Highlight:
                    colorsToLoad = highlightColors;
                    selectedButton = currentColors.highlight;
                    break;
                case ColorPalette.Pencil:
                    colorsToLoad = pencilColors;
                    selectedButton = currentColors.pencil;
                    break;
                default:
                    colorsToLoad = calligraphyColors;
                    selectedButton = currentColors.calligraphy;
                    break;
            }
            int j = 0;
            foreach (Color color in colorsToLoad)
            {
                ColorPickerButton button = new ColorPickerButton(new Windows.UI.Xaml.Media.SolidColorBrush(color), parent);
                button.RemoveColor += (s, e) =>
                {
                    colorsToLoad.Remove(s.Fill.Color);
                    panel.Children.Remove(s);
                    panel.UpdateButtonIndices();
                };
                button.ChangeColor += onChangeColor;
                if (selectedButton == j)
                    button.isSelected = true;
                panel.Children.Add(button);
                ++j;
                button.AnimateScale(1f);
                await Task.Delay(40);
                if (ct.IsCancellationRequested)
                    return;
            }
            panel.UpdateButtonIndices();
            loadingColorsTask = null;
        }

        private async Task SaveSettings()
        {
            configFile = await ApplicationData.Current.RoamingFolder.CreateFileAsync("config.json", CreationCollisionOption.ReplaceExisting);
            using (Stream opStream = await configFile!.OpenStreamForWriteAsync())
                JsonSerializer.Serialize(opStream, this, SettingsJsonContext.Default.Settings);
        }
    }

    [JsonSerializable(typeof(Settings))]
    internal partial class SettingsJsonContext : JsonSerializerContext { }

    public class CurrentlySelectedColors
    {
        public int drawing, highlight, pencil, calligraphy;

        public CurrentlySelectedColors()
        {
            drawing = highlight = pencil = calligraphy = 0;
        }
    }

    public enum HomeScreenThumbnailSize
    {
        Small,
        Medium,
        Large,
    }

    public enum ColorPalette
    {
        Drawing,
        Highlight,
        Pencil,
        Calligraphy,
    }

    public enum UndoRedoButtonsPlacement
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }
}
