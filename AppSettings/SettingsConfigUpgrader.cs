using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace AppSettings
{
    internal static class SettingsConfigUpgrader
    {
        public static Settings UpgradeToLatest(Settings current)
        {
            return Upgrade1To2(current);
        }

        private static Settings Upgrade1To2(Settings current)
        {
            if (current.configVersion < 1)
                current = Upgrade0To1(current);

            current.configVersion = 2;
            if (current.drawingColors is null)
            {
                current.drawingColors = new ObservableCollection<Color>
                    {
                        Colors.Black,
                        Colors.Blue,
                        Colors.Red,
                        Colors.Green,
                        Colors.Yellow,
                    };
            }
            return current;
        }

        private static Settings Upgrade0To1(Settings current)
        {
            current.configVersion = 1;
            return current;
        }
    }
}
