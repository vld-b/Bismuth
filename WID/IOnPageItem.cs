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
        public double GetTop();
        public double GetLeft();
        public void SetPos(double left, double top);
        public string GetFileName();
        public void SetHasBeenModified(bool value);
        public void SetIsSelectable(bool isSelectable);
    }
}
