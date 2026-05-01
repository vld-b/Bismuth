using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WID
{
    public interface IOnPageItem
    {
        public double Top { get; }
        public double Left { get; }
        public void SetPos(double left, double top);
        public double Width { get; }
        public double Height { get; }
        public void SetDimensions(double width, double height);
        public string FileName { get; }
        public bool HasBeenModified { set; }
        public void SetIsSelectable(bool isSelectable);
    }
}
