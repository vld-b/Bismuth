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
            return Upgrade4To5(current);
        }

        private static Settings Upgrade4To5(Settings current)
        {
            if (current.configVersion < 4)
                current = Upgrade3To4(current);

            current.configVersion = 5;

            return current;
        }

        private static Settings Upgrade3To4(Settings current)
        {
            if (current.configVersion < 3)
                current = Upgrade2To3(current);

            current.configVersion = 4;
            current.homescreenThumbnailSize = HomeScreenThumbnailSize.Medium;

            return current;
        }

        private static Settings Upgrade2To3(Settings current)
        {
            if (current.configVersion < 2)
                current = Upgrade1To2(current);

            current.configVersion = 3;
            current.tipSize = 4d;

            return current;
        }

        private static Settings Upgrade1To2(Settings current)
        {
            if (current.configVersion < 1)
                current = Upgrade0To1(current);

            current.configVersion = 2;
            current.drawingColors = new ObservableCollection<Color>
            {
                Colors.Black,
                Colors.Blue,
                Colors.Red,
                Colors.Green,
                Colors.Yellow,
            };
            return current;
        }

        private static Settings Upgrade0To1(Settings current)
        {
            current.configVersion = 1;
            return current;
        }
    }
}
