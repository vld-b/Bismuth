using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Markup;

namespace Shared
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class Loc : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;
        private static Windows.ApplicationModel.Resources.ResourceLoader loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("Shared/Resources");

        protected override object ProvideValue()
        {
            if (string.IsNullOrEmpty(Key)) return string.Empty;

            return loader.GetString(Key);
        }

        public static string GetLocalizedString(string key)
        {
            return loader.GetString(key);
        }
    }
}
