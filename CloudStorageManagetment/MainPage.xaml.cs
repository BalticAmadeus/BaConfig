using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace ConfigurationStorageManager
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private const string VAULT_NAME = "ConnectionStrings";

        private ObservableCollection<CloudBlobContainer> _containerDropBoxItems = new ObservableCollection<CloudBlobContainer>();
        private ObservableCollection<ConnectionModel> _connectionDropBoxItems = new ObservableCollection<ConnectionModel>();
        private ObservableCollection<CloudBlockBlob> _blobListViewItems = new ObservableCollection<CloudBlockBlob>();
        private ObservableCollection<string> _searchSuggestions = new ObservableCollection<string>();

        private CloudStorageService _storageClient;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MainPage()
        {
            this.InitializeComponent();
            GetConnectionsFromStorage();

            HideBlobControls();
            BlobListView.Visibility = Visibility.Collapsed;
            ReconnectButton.Visibility = Visibility.Collapsed;
            AddBlobButton.Visibility = Visibility.Collapsed;
        }

        #region Buttons_Click
        private void EditConnectionsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(StorageSelectionPage), _connectionDropBoxItems);
        }

        private async void SaveBlobButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBlob = (CloudBlockBlob)BlobListView.SelectedItem;
            if (selectedBlob == null) return;

            var contentText = BlobContentTxt.Text;

            if (!IsJsonValid(contentText))
            {
                await ShowDialogToUser("Json format is invalid.");
            }

            try
            {
                InfoMessageText.Text = "Working ...";
                await _storageClient.UploadDataToBlobAsync(selectedBlob, contentText);
                await ShowMessageToUser($"Blob {selectedBlob.Name} content have been saved.");
            }
            catch (Exception ex)
            {
                InfoMessageText.Text = "";
                await ShowDialogToUser(ex.Message);
            }
        }

        private async void DeleteBlobButton_CLick(object sender, RoutedEventArgs e)
        {
            var selectedBlob = (CloudBlockBlob)BlobListView.SelectedItem;
            if (selectedBlob == null) return;

            var deleteConfirmDialog = new MessageDialog($"Do you want to delete \"{selectedBlob.Name}\" blob ?");
            deleteConfirmDialog.Commands.Add(new UICommand("Yes", cmd => DeleteBlob(selectedBlob)));
            deleteConfirmDialog.Commands.Add(new UICommand("No"));
            await deleteConfirmDialog.ShowAsync();
        }

        private async void DeleteBlob(CloudBlockBlob blob)
        {
            try
            {
                InfoMessageText.Text = "Working ...";
                await _storageClient.RemoveBlobAsync(blob);
                _blobListViewItems.Remove(blob);

                HideBlobControls();

                await ShowMessageToUser($"Blob \"{blob.Name}\" have been deleted.");
            }
            catch (Exception ex)
            {
                await ShowDialogToUser(ex.Message);
            }
        }

        private void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectToStorage();
        }

        private async void AddBlobButton_Click(object sender, RoutedEventArgs e)
        {
            DisableSelection();
            try
            {
                var newBlobDialog = new NewBlobDialog(_blobListViewItems);
                var dialogResults = await newBlobDialog.ShowAsync();
                if (dialogResults == ContentDialogResult.Secondary)
                {
                    InfoMessageText.Text = "Working ...";

                    var newBlob = await _storageClient.AddBlobAsync((CloudBlobContainer)ContainerDropBox.SelectedItem, newBlobDialog.BlobName, "");
                    _blobListViewItems.Add(newBlob);
                    OnPropertyChanged(nameof(_blobListViewItems));

                    EnableSelection();

                    BlobListView.SelectedItem = newBlob;
                    await ShowMessageToUser($"Blob \"{newBlobDialog.BlobName}\" have been created.");
                }
                EnableSelection();
            }
            catch (Exception ex)
            {
                InfoMessageText.Text = "";
                await ShowDialogToUser(ex.Message);
                EnableSelection();
            }
        }

        #endregion

        #region Lists_SelectionChanged
        private void ConnectionDropBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConnectToStorage();
        }

        private async void BlobListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedBlob = (CloudBlockBlob)BlobListView.SelectedItem;
            if (selectedBlob == null) return;
            await PopulateBlob(selectedBlob);
        }

        private async void ContainerDropBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisableSelection();
            HideBlobControls();
            BlobListView.Visibility = Visibility.Collapsed;

            var selectedContainer = (CloudBlobContainer)ContainerDropBox.SelectedItem;
            if (selectedContainer == null) return;

            try
            {
                InfoMessageText.Text = "Working ...";
                var containerBlobSegment = await _storageClient.GetBlobsFromCloudAsync(selectedContainer);
                 _blobListViewItems = new ObservableCollection<CloudBlockBlob>(containerBlobSegment.Results.ToList().Cast<CloudBlockBlob>().ToList());
                OnPropertyChanged(nameof(_blobListViewItems));
                BlobListView.Visibility = Visibility.Visible;
                AddBlobButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                await ShowDialogToUser(ex.Message);
            }

            InfoMessageText.Text = "";
            EnableSelection();
        }

        #endregion

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

        private async void ConnectToStorage()
        {
            DisableSelection();
            HideBlobControls();
            BlobListView.Visibility = Visibility.Collapsed;
            AddBlobButton.Visibility = Visibility.Collapsed;

            var connection = (ConnectionModel)ConnectionDropBox.SelectedItem;
            if (connection == null) return;

            _containerDropBoxItems.Clear();
            _blobListViewItems.Clear();

            _storageClient = new CloudStorageService(connection.ConnectionString);
            if (!_storageClient.IsConnected)
            {
                await ShowDialogToUser("Error: Failed to connect to cloud storage !");
                ReconnectButton.Visibility = Visibility.Visible;
                EnableSelection();
                return;
            }

            try
            {
                InfoMessageText.Text = "Working ...";
                await PopulateContainerDropBox();
                EnableSelection();
                await ShowMessageToUser("Successfully connected to cloud storage.");
            }
            catch (Exception ex)
            {
                InfoMessageText.Text = "";
                await ShowDialogToUser(ex.InnerException.Message);

                ReconnectButton.Visibility = Visibility.Visible;
                EnableSelection();
            }
        }

        private void GetConnectionsFromStorage()
        {
            try
            {
                var vault = new PasswordVault();
                var connectionListFromStorage = vault.FindAllByResource(VAULT_NAME);
                foreach (var connection in connectionListFromStorage)
                {
                    connection.RetrievePassword();
                    _connectionDropBoxItems.Add(new ConnectionModel
                    {
                        ConnectionName = connection.UserName,
                        NewConnectionName = connection.UserName,
                        ConnectionString = connection.Password,
                        NewConnectionString = connection.Password
                    });
                }
            }
            catch{}
        }

        private async Task PopulateContainerDropBox()
        {
            var containerSegment = await _storageClient.GetContainersFromCloudAsync();
            _containerDropBoxItems = new ObservableCollection<CloudBlobContainer>(containerSegment.Results.ToList());
            OnPropertyChanged(nameof(_containerDropBoxItems));
        }

        private async Task PopulateBlob(CloudBlockBlob blob)
        {
            DisableSelection();

            try
            {
                InfoMessageText.Text = "Working ...";
                var blobData = await _storageClient.GetDataFromBlobAsync(blob);
                BlobNameTxt.Text = blob.Name;
                BlobContentTxt.Text = blobData;
                ShowBlobControls();
            }
            catch(Exception ex)
            {
                await ShowDialogToUser(ex.Message);
            }

            InfoMessageText.Text = "";
            EnableSelection();
        }

        private  async Task ShowMessageToUser(string message)
        {
            InfoMessageText.Text = message;
            await Task.Delay(3500);
            InfoMessageText.Text = "";
        }

        private async Task ShowDialogToUser(string message)
        {
            var messageDialog = new MessageDialog(message);
            await messageDialog.ShowAsync();
        }

        private void SearchBlobTxt_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text.Count() > 0)
                {
                    _searchSuggestions = new ObservableCollection<string>
                        (_blobListViewItems.Select(x => x.Name).Cast<string>()
                        .Where(x=>x.IndexOf(sender.Text, StringComparison.CurrentCultureIgnoreCase)>= 0));
                    OnPropertyChanged(nameof(_searchSuggestions));
                }
                else
                {
                    _searchSuggestions = new ObservableCollection<string>(_blobListViewItems.Select(x=>x.Name).Cast<string>());
                    OnPropertyChanged(nameof(_searchSuggestions));
                }
            }
        }

        private void SearchBlobTxt_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if(args.ChosenSuggestion != null)
                BlobListView.SelectedItem = _blobListViewItems.Single(x => x.Name.Equals((string)args.ChosenSuggestion));
        }

        private void HideBlobControls()
        {
            SaveBlobButton.Visibility = Visibility.Collapsed;
            DeleteBlobButton.Visibility = Visibility.Collapsed;
            BlobNameTxt.Visibility = Visibility.Collapsed;
            BlobContentTxt.Visibility = Visibility.Collapsed;
        }

        private void ShowBlobControls()
        {
            BlobContentTxt.Visibility = Visibility.Visible;
            BlobNameTxt.Visibility = Visibility.Visible;
            SaveBlobButton.Visibility = Visibility.Visible;
            DeleteBlobButton.Visibility = Visibility.Visible;
        }

        private void EnableSelection()
        {
            ContainerDropBox.IsEnabled = true;
            ConnectionDropBox.IsEnabled = true;
        }

        private void DisableSelection()
        {
            ContainerDropBox.IsEnabled = false;
            ConnectionDropBox.IsEnabled = false;
        }
    }
}
