using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WID
{
    class MenuElement : INotifyPropertyChanged
    {
        private string _itemName;
        public string itemName
        {
            get => _itemName;
            set
            {
                if (_itemName != value)
                {
                    _itemName = value;
                    OnPropertyChanged(nameof(itemName));
                }
            }
        }
        public bool isFolder { get; private set; }

        public MenuElement(string itemName, bool isFolder)
        {
            this.itemName = itemName;
            this.isFolder = isFolder;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
