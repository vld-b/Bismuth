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
            return Upgrade1To2(config);
        }

        private static NotebookConfig Upgrade1To2(NotebookConfig config)
        {
            if (config.configVersion < 1L)
            {
                config = Upgrade0To1(config);
            }

            if (config.configVersion != 2L)
            {
                config.maxImageID = -1;
                config.usableImageIDs = new List<int>();
            }
            foreach (PageConfig pageConfig in config.pageMapping)
            {
                if (pageConfig.images is null)
                {
                    pageConfig.images = new List<ImageData>();
                }
            }
            config.configVersion = 2L;
            return config;
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
