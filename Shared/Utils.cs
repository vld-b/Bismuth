using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.UserDataTasks.DataProvider;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace Shared
{
    public static class Utils
    {
        private static ContentDialog? currentlyShowingContentDialog;

        public static void Add<T>(this List<T> origin, List<T> source)
        {
            foreach (T item in source)
                origin.Add(item);
        }

        public async static Task CreatePending(List<string> items, StorageFolder folder)
        {
            foreach (string item in items)
            {
                string itemPath = folder.Path + "\\" + item;
                if (File.Exists(itemPath))
                    File.Delete(itemPath);
                await folder.CreateFileAsync(item, CreationCollisionOption.ReplaceExisting);
            }
            items.Clear();
        }

        public async static Task DeletePending(List<string> items, StorageFolder folder)
        {
            foreach (string item in items)
            {
                try
                {
                    await (await folder.GetFileAsync(item)).DeleteAsync();
                } catch { }
            }
            items.Clear();
        }

        public static async Task<BitmapImage> GetBMPFromFile(StorageFile bgFile)
        {
            BitmapImage bmp = new BitmapImage();
            using (IRandomAccessStream stream = await bgFile.OpenAsync(FileAccessMode.Read))
                await bmp.SetSourceAsync(stream);
            return bmp;
        }

        public static async Task<BitmapImage> GetBMPFromFileWithWidth(StorageFile bgFile, int desiredWidth)
        {
            BitmapImage bmp = new BitmapImage();
            bmp.DecodePixelWidth = desiredWidth;
            using (IRandomAccessStream stream = await bgFile.OpenAsync(FileAccessMode.Read))
                await bmp.SetSourceAsync(stream);
            return bmp;
        }

        public static string GetNotebookNameFromFolder(StorageFolder folder)
        {
            return folder.DisplayName[..(folder.DisplayName.Length - 9)];
        }

        public static string GetNotebookPathFromFolder(StorageFolder folder)
        {
            return folder.Path[(ApplicationData.Current.LocalFolder.Path.Length + 1)..(folder.Path.Length - 9)];
        }

        public static Color HsvToColor(byte alpha, float h, float s, float v)
        {
            int i = (int)(h * 6.0f);
            float f = h * 6.0f - i;
            float p = v * (1.0f - s);
            float q = v * (1.0f - f * s);
            float t = v * (1.0f - (1.0f - f) * s);

            float r, g, b;
            switch (i % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }

            return Color.FromArgb(
                alpha,
                (byte)Math.Round(r * 255.0f),
                (byte)Math.Round(g * 255.0f),
                (byte)Math.Round(b * 255.0f)
            );
        }

        public static Color AdjustSaturation(this Color color)
        {
            // 1. Convert RGB (0-255) to normalized floats (0.0 - 1.0)
            float r = color.R / 255.0f;
            float g = color.G / 255.0f;
            float b = color.B / 255.0f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            // If delta is 0, the color is grayscale (white, gray, black) with no hue
            if (delta == 0.0f)
            {
                return color;
            }

            float s = delta / max;

            // 2. Calculate Hue (0 to 360 degrees)
            float hue;
            if (max == r)
                hue = (g - b) / delta + (g < b ? 6.0f : 0.0f);
            else if (max == g)
                hue = (b - r) / delta + 2.0f;
            else
                hue = (r - g) / delta + 4.0f;

            hue /= 6.0f; // Normalize hue to 0.0 - 1.0

            // 3. Reconstruct RGB with Saturation forced to 1.0f (100%)
            // Keep original Value/Brightness (max) and Alpha
            return HsvToColor(color.A, hue, MathF.Min(1.0f, 1.2f * s), max);
        }

        public async static Task MovePending(List<StorageFile> items, StorageFolder folder)
        {
            foreach (StorageFile item in items)
            {
                if (File.Exists(folder.Path + "\\" + item.Name))
                {
                    await (await folder.GetFileAsync(item.Name)).DeleteAsync();
                }
                await item.MoveAsync(folder);
            }
            items.Clear();
        }

        public static Windows.UI.Color MultiplyColorWithScalar(Windows.UI.Color color, float scalar)
        {
            return Windows.UI.Color.FromArgb((byte)((float)color.A * scalar), color.R, color.G, color.B);
        }

        public static T Pop<T>(this List<T> list, int index)
        {
            T val = list[index];
            list.RemoveAt(index);
            return val;
        }

        public async static Task RenamePending(List<RenameItem> items)
        {
            foreach (RenameItem item in items)
            {
                string targetPath = (await item.file.GetParentAsync()).Path + "\\" + item.to;
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                await item.file.RenameAsync(item.to);
            }
            items.Clear();
        }
        
        // Raises the maximum value between r, g and b to 255 and adjusts others accordingly
        public static Windows.UI.Color SaturateColor(Windows.UI.Color color)
        {
            float r = (float)color.R;
            float g = (float)color.G;
            float b = (float)color.B;

            float max = MathF.Max(r, MathF.Max(g, b));

            r *= max;
            g *= max;
            b *= max;

            return Windows.UI.Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
        }

        public static ContentDialog ShowLoadingPopup(string title)
        {
            if (currentlyShowingContentDialog is not null)
            {
                currentlyShowingContentDialog.Hide();
                currentlyShowingContentDialog = null;
            }
            currentlyShowingContentDialog = new ContentDialog { Title = title, IsPrimaryButtonEnabled = false, IsSecondaryButtonEnabled = false };
            currentlyShowingContentDialog.Content = new Microsoft.UI.Xaml.Controls.ProgressBar { IsIndeterminate = true, HorizontalAlignment=HorizontalAlignment.Stretch, ShowPaused = false, ShowError = false };
            _ = currentlyShowingContentDialog.ShowAsync();
            return currentlyShowingContentDialog;
        }

        public async static Task ShowTeachingTip(TeachingTip tt, string title, string subtitle, int msDelay)
        {
            tt.Title = title;
            tt.Subtitle = subtitle;
            tt.IsOpen = true;
            await Task.Delay(msDelay);
            tt.IsOpen = false;
        }
    }

    // Class for renaming files when saving notebooks
    public class RenameItem
    {
        public StorageFile file { get; private set; }
        public string to { get; private set; }

        public RenameItem(StorageFile from, string to)
        {
            this.file = from;
            this.to = to;
        }
    }
}
