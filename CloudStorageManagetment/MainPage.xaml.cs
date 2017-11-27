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
        private ObservableCollection<CloudBlockBlob> _blobListItems = new ObservableCollection<CloudBlockBlob>();
        private ObservableCollection<string> _searchSuggestions = new ObservableCollection<string>();

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MainPage()
        {
            this.InitializeComponent();
            GetConnectionsFromStorage();

            HideBlobControls();
            BlobList.Visibility = Visibility.Collapsed;
            ReconnectButton.Visibility = Visibility.Collapsed;
            AddNewBlobButton.Visibility = Visibility.Collapsed;
        }

        #region Buttons_Click
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(StorageSelectionPage), _connectionDropBoxItems);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBlob = (CloudBlockBlob)BlobList.SelectedItem;
            if (selectedBlob == null) return;

            var contentText = BlobContentTxt.Text;

            if (!IsJsonValid(contentText))
            {
                await ShowDialogToUser("Json format is invalid.");
            }

            try
            {
                MessageText.Text = "Working ...";
                await CloudStorageManagetment.UploadDataToBlobAsync(selectedBlob, contentText);
                await ShowMessageToUser($"Blob {selectedBlob.Name} content have been saved.");
            }
            catch (Exception ex)
            {
                MessageText.Text = "";
                await ShowDialogToUser(ex.InnerException.Message);
            }
        }

        private async void DeleteButton_CLick(object sender, RoutedEventArgs e)
        {
            var selectedBlob = (CloudBlockBlob)BlobList.SelectedItem;
            if (selectedBlob == null) return;

            var dialogYes = new UICommand("Yes", async cmd =>
            {
                try
                {
                    MessageText.Text = "Working ...";
                    await CloudStorageManagetment.RemoveBlobAsync(selectedBlob);
                    _blobListItems.Remove(selectedBlob);

                    HideBlobControls();

                    await ShowMessageToUser($"Blob \"{selectedBlob.Name}\" have been deleted.");
                }
                catch (Exception ex)
                {
                    await ShowDialogToUser(ex.InnerException.Message);
                }
            });
            var dialogNo = new UICommand("No");

            var deleteConfirmDialog = new MessageDialog($"Do you want to delete \"{selectedBlob.Name}\" blob ?");
            deleteConfirmDialog.Commands.Add(dialogYes);
            deleteConfirmDialog.Commands.Add(dialogNo);
            await deleteConfirmDialog.ShowAsync();
        }

        private void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectToStorage();
        }

        private async void AddNewBlobButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newBlobDialog = new NewBlobDialog(_blobListItems);
                var dialogResults = await newBlobDialog.ShowAsync();
                if (dialogResults == ContentDialogResult.Secondary)
                {
                    ContainerDropBoxList.IsEnabled = false;
                    ConnectionList.IsEnabled = false;
                    MessageText.Text = "Working ...";

                    var newBlob = await CloudStorageManagetment.AddNewBlobAsync((CloudBlobContainer)ContainerDropBoxList.SelectedItem, newBlobDialog.BlobName, "");
                    _blobListItems.Add(newBlob);
                    OnPropertyChanged(nameof(_blobListItems));

                    ContainerDropBoxList.IsEnabled = true;
                    ConnectionList.IsEnabled = true;

                    BlobList.SelectedItem = newBlob;
                    await ShowMessageToUser($"Blob \"{newBlobDialog.BlobName}\" have been created.");
                }
            }
            catch (Exception ex)
            {
                MessageText.Text = "";
                await ShowDialogToUser(ex.InnerException.Message);
            }
        }

        #endregion

        #region Lists_SelectionChanged
        private void ConnectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConnectToStorage();
        }

        private async void BlobList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedBlob = (CloudBlockBlob)BlobList.SelectedItem;
            if (selectedBlob == null) return;
            await PopulateBlob(selectedBlob);
        }

        private async void ContainerDropBoxList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContainerDropBoxList.IsEnabled = false;
            ConnectionList.IsEnabled = false;

            HideBlobControls();

            BlobList.Visibility = Visibility.Collapsed;

            var selectedContainer = (CloudBlobContainer)ContainerDropBoxList.SelectedItem;
            if (selectedContainer == null) return;

            try
            {
                MessageText.Text = "Working ...";
                var containerBlobSegment = await CloudStorageManagetment.GetBlobsFromCloudAsync(selectedContainer);
                 _blobListItems = new ObservableCollection<CloudBlockBlob>(containerBlobSegment.Results.ToList().Cast<CloudBlockBlob>().ToList());
                OnPropertyChanged(nameof(_blobListItems));
                BlobList.Visibility = Visibility.Visible;
                AddNewBlobButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                await ShowDialogToUser(ex.InnerException.Message);
            }

            MessageText.Text = "";
            ContainerDropBoxList.IsEnabled = true;
            ConnectionList.IsEnabled = true;
        }

        #endregion

        #region Validations
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
        #endregion

        private async void ConnectToStorage()
        {
            ContainerDropBoxList.IsEnabled = false;
            ConnectionList.IsEnabled = false;

            HideBlobControls();

            BlobList.Visibility = Visibility.Collapsed;
            AddNewBlobButton.Visibility = Visibility.Collapsed;

            var connection = (ConnectionModel)ConnectionList.SelectedItem;
            if (connection == null) return;

            _containerDropBoxItems.Clear();
            _blobListItems.Clear();

            var isConnectedToStorage = CloudStorageManagetment.CreateConnectionWithCloud(connection.ConnectionString);
            if (!isConnectedToStorage)
            {
                await ShowDialogToUser("Error: Failed connect to cloud storage !");
                ReconnectButton.Visibility = Visibility.Visible;
                ContainerDropBoxList.IsEnabled = true;
                ConnectionList.IsEnabled = true;

                return;
            }

            try
            {
                MessageText.Text = "Working ...";
                await PopulateContainerDropBoxList();

                ContainerDropBoxList.IsEnabled = true;
                ConnectionList.IsEnabled = true;

                await ShowMessageToUser("Successfully connected to cloud storage.");
            }
            catch (Exception ex)
            {
                MessageText.Text = "";
                await ShowDialogToUser(ex.InnerException.Message);

                ReconnectButton.Visibility = Visibility.Visible;
                ContainerDropBoxList.IsEnabled = true;
                ConnectionList.IsEnabled = true;
            }
        }

        private async void GetConnectionsFromStorage()
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
            catch(Exception ex)
            {
                await ShowDialogToUser(ex.InnerException.Message);
            }
        }

        private async Task PopulateContainerDropBoxList()
        {
            var containerSegment = await CloudStorageManagetment.GetContainersFromCloudAsync();
            _containerDropBoxItems = new ObservableCollection<CloudBlobContainer>(containerSegment.Results.ToList());
            OnPropertyChanged(nameof(_containerDropBoxItems));
        }

        private async Task PopulateBlob(CloudBlockBlob blob)
        {
            try
            {
                ContainerDropBoxList.IsEnabled = false;
                ConnectionList.IsEnabled = false;
                MessageText.Text = "Working ...";

                var blobData = await CloudStorageManagetment.GetDataFromBlobAsync(blob);
                BlobNameTxt.Text = blob.Name;
                BlobContentTxt.Text = blobData;

                ShowBlobControls();
                
                ContainerDropBoxList.IsEnabled = true;
                ConnectionList.IsEnabled = true;
            }
            catch(Exception ex)
            {
                await ShowDialogToUser(ex.InnerException.Message);
            }
            MessageText.Text = "";
        }

        private  async Task ShowMessageToUser(string message)
        {
            MessageText.Text = message;
            await Task.Delay(3500);
            MessageText.Text = "";
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
                        (_blobListItems.Select(x => x.Name).Cast<string>()
                        .Where(x=>x.IndexOf(sender.Text, StringComparison.CurrentCultureIgnoreCase)>= 0));
                    OnPropertyChanged(nameof(_searchSuggestions));
                }
                else
                {
                    _searchSuggestions = new ObservableCollection<string>(_blobListItems.Select(x=>x.Name).Cast<string>());
                    OnPropertyChanged(nameof(_searchSuggestions));
                }
            }
        }

        private void SearchBlobTxt_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if(args.ChosenSuggestion != null)
                BlobList.SelectedItem = _blobListItems.Single(x => x.Name.Equals((string)args.ChosenSuggestion));
        }

        private void HideBlobControls()
        {
            SaveButton.Visibility = Visibility.Collapsed;
            DeletButton.Visibility = Visibility.Collapsed;
            BlobNameTxt.Visibility = Visibility.Collapsed;
            BlobContentTxt.Visibility = Visibility.Collapsed;
        }

        private void ShowBlobControls()
        {
            BlobContentTxt.Visibility = Visibility.Visible;
            BlobNameTxt.Visibility = Visibility.Visible;
            SaveButton.Visibility = Visibility.Visible;
            DeletButton.Visibility = Visibility.Visible;
        }
    }
}
