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
        public string fileName { get; private set; }
        public bool IsFolder { get; private set; }

        public NotebookItem(string Name, string fileName, bool IsFolder)
        {
            this.Name = Name;
            this.fileName = fileName;
            this.IsFolder = IsFolder;
        }
    }
}
