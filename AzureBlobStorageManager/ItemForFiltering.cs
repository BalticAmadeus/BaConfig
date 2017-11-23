using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace ConfigurationStorageManager
{
    public class FilteredItem
    {
        public CloudBlockBlob Blob { get; set; }
        public PivotItem PivotItem { get; set; }

        public override string ToString()
        {
            return Blob.Name;
        }
    }
}
