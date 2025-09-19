using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WID
{
    class NotebookItem
    {
        public string Name { get; private set; }

        public NotebookItem(string Name)
        {
            this.Name = Name;
        }
    }
}
