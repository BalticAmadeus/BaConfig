using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using ConfigurationStorageManager.Annotations;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ConfigurationStorageManager.Models
{
    public class BlobListItem: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public CloudBlockBlob Blob { get; set; }
        public string LocalBlobContent { get; set; }
        public string CloudBlobContent { get; set; }

        public string Visability { get; set; } = "Collapsed";

        private bool _saved = true;
        public bool Saved
        {
            get => _saved;
            set
            {
                _saved = value;
                Visability = _saved ? "Collapsed" : "Visible";
                OnPropertyChanged(nameof(Visability));
            }
        }
    }
}
