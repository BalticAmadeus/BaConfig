using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        public string BlobContent { get; set; }
        private List<object> _blobList;

        public NewBlobDialog(List<object> blobList)
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
            if (!IsBlobValid(BlobName, BlobContent))
                args.Cancel = true;
        }

        private bool IsBlobValid(string blobName, string blobContent)
        {
            if (blobName.Count().Equals(0) || blobContent.Count().Equals(0))
            {
                ShowDialogToUser("Blob name or blob content can not be empty.");
                return false;
            }

            if (!IsBlobNameValid(blobName))
            {
                ShowDialogToUser($"Blob name {blobName} already exists.");
                return false;
            }

            if (!IsJsonValid(blobContent))
            {
                ShowDialogToUser("Json format is invalid.");
                return false;
            }
            return true;
        }

        private bool IsBlobNameValid(string blobName)
        {
            foreach (PivotItem pivotItem in _blobList)
            {
                var blob = (PivotItemTag)pivotItem.Tag;
                if (blob != null)
                {
                    if (blob.Blob.Name.Equals(blobName))
                        return false;
                }
            }
            return true;
        }

        private static bool IsJsonValid(string jsonString)
        {
            try
            {
                JObject.Parse(jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private async void ShowMessageToUser(string message)
        {
            Message.Text = message;
            await Task.Delay(3500);
            Message.Text = "";
        }

        private async void ShowDialogToUser(string message)
        {
            var messageDialog = new MessageDialog(message);
            await messageDialog.ShowAsync();
        }
    }
}
