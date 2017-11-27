using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ConfigurationStorageManager
{
    public sealed partial class NewBlobDialog : ContentDialog
    {
        public string BlobName { get; set; }
        private ObservableCollection<CloudBlockBlob> _blobList;

        public NewBlobDialog(ObservableCollection<CloudBlockBlob> blobList)
        {
            _blobList = blobList;
            this.InitializeComponent();
        }
        

        private void CloseButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Hide();
        }

        private void SaveButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (!IsBlobValid(BlobName))
                args.Cancel = true;
        }

        private bool IsBlobValid(string blobName)
        {
            if (blobName.Count().Equals(0))
            {
                ShowDialogToUser("Blob name or blob content can not be empty.");
                return false;
            }

            if (!IsBlobNameValid(blobName))
            {
                ShowDialogToUser($"Blob name {blobName} already exists.");
                return false;
            }

            return true;
        }

        private bool IsBlobNameValid(string blobName)
        {
            foreach (var blob in _blobList)
            {
                if (blob.Name.Equals(blobName))
                    return false;
            }
            return true;
        }

        private async void ShowDialogToUser(string message)
        {
            var messageDialog = new MessageDialog(message);
            await messageDialog.ShowAsync();
        }
    }
}
