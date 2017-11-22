using Microsoft.WindowsAzure.Storage.Blob;
using Windows.UI.Xaml.Controls;

namespace ConfigurationStorageManager
{
    public class PivotItemTag
    {
        public CloudBlockBlob Blob { get; set; }
        public TextBox Content { get; set; }
    }
}
