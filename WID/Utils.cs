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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace WID
{
    public static class Utils
    {
        public static T Pop<T>(this List<T> list, int index)
        {
            T val = list[index];
            list.RemoveAt(index);
            return val;
        }

        public async static Task CreatePending(List<string> items, StorageFolder folder)
        {
            foreach (string item in items)
            {
                try
                {
                    await folder.CreateFileAsync(item, CreationCollisionOption.FailIfExists);
                } catch
                {

                }
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

        public async static Task ShowTeachingTip(TeachingTip tt, string title, string subtitle, int msDelay)
        {
            tt.Title = title;
            tt.Subtitle = subtitle;
            tt.IsOpen = true;
            await Task.Delay(msDelay);
            tt.IsOpen = false;
        }

        public static ContentDialog ShowLoadingPopup(string title)
        {
            ContentDialog dialog = new ContentDialog { Title = title, IsPrimaryButtonEnabled = false, IsSecondaryButtonEnabled = false };
            dialog.Opened += (s, e) => ((Microsoft.UI.Xaml.Controls.ProgressBar)dialog.Content).IsIndeterminate = true;
            dialog.Content = new Microsoft.UI.Xaml.Controls.ProgressBar { IsIndeterminate = true, HorizontalAlignment=HorizontalAlignment.Stretch, ShowPaused = false, ShowError = false };
            dialog.ShowAsync();
            return dialog;
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
