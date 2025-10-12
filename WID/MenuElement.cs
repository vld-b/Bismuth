using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WID
{
    class MenuElement
    {
        public string itemName { get; set; }
        public bool isFolder { get; private set; }

        public MenuElement(string itemName, bool isFolder)
        {
            this.itemName = itemName;
            this.isFolder = isFolder;
        }
    }
}
