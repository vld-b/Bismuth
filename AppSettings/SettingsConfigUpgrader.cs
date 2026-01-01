using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppSettings
{
    internal static class SettingsConfigUpgrader
    {
        public static Settings UpgradeToLatest(Settings current)
        {
            return Upgrade0To1(current);
        }

        private static Settings Upgrade0To1(Settings current)
        {
            current.configVersion = 1;
            return current;
        }
    }
}
