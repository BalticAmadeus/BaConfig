using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

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
