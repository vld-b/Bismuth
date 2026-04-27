using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WID
{
    public interface IOnPageItem
    {
        public double GetTop();
        public double GetLeft();
        public void SetPos(double top, double left);
        public string GetFileName();
        public void SetHasBeenModified(bool value);
    }
}
