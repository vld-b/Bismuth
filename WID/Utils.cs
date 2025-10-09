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
using Windows.Foundation.Diagnostics;
using Windows.Storage;

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

        public async static Task DeletePending(Stack<string> items, StorageFolder folder)
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

        public async static Task MovePending(Stack<StorageFile> items, StorageFolder folder)
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

        public async static Task RenamePending(Stack<RenameItem> items)
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

        public async static Task ShowPopup(TeachingTip tt, string title, string subtitle, int msDelay)
        {
            tt.Title = title;
            tt.Subtitle = subtitle;
            tt.IsOpen = true;
            await Task.Delay(msDelay);
            tt.IsOpen = false;
        }
    }

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
