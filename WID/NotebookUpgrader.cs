using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WID
{
    internal static class NotebookUpgrader
    {
        public static NotebookConfig UpgradeToLastVersion(NotebookConfig config)
        {
            return Upgrade0To1(config);
        }

        private static NotebookConfig Upgrade0To1(NotebookConfig config)
        {
            config.configVersion = 1L;
            if (config.defaultTemplate == null)
                config.defaultTemplate = new DefaultTemplate(null);
            return config;
        }
    }
}
