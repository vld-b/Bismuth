using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

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
    }
}
