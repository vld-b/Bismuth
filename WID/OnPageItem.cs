using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WID
{
    internal interface IOnPageItem
    {
        public double GetTop();
        public double GetLeft();
        public void SetPos(double top, double left);
    }
}
