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
            if (!blobName.Any())
            {
                ShowDialogToUser("Blob name or blob content can not be empty.");
                return false;
            }

            if (IsBlobNameValid(blobName)) return true;

            ShowDialogToUser($"Blob name {blobName} already exists.");
            return false;
        }

        private bool IsBlobNameValid(string blobName)
        {
            return _blobList.All(blob => !blob.Name.Equals(blobName));
        }

        private async void ShowDialogToUser(string message)
        {
            var messageDialog = new MessageDialog(message);
            await messageDialog.ShowAsync();
        }
    }
}
