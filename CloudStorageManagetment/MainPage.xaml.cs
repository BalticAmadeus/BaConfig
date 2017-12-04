using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using ConfigurationStorageManager.Models;
using ConfigurationStorageManager.Services;

namespace ConfigurationStorageManager
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private ObservableCollection<CloudBlobContainer> _containerDropBoxItems = new ObservableCollection<CloudBlobContainer>();
        private ObservableCollection<ConnectionModel> _connectionDropBoxItems;
        private ObservableCollection<BlobListItem> _blobListViewItems = new ObservableCollection<BlobListItem>();
        private ObservableCollection<string> _searchSuggestions = new ObservableCollection<string>();

        private CloudStorageService _storageClient;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MainPage()
        {
            this.InitializeComponent();

            Window.Current.CoreWindow.KeyDown += CoreWindow_OnKeyDown;
            Application.Current.Suspending += new SuspendingEventHandler(Current_Suspending);
            

            HideBlobControls();
            SearchBlobTxt.Visibility = Visibility.Collapsed;
            ReconnectButton.Visibility = Visibility.Collapsed;
            HideBlobListControls();
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
           
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var connectionStorage = new ConnectionStorageService();
            _connectionDropBoxItems = connectionStorage.GetAllConnectionsFromStorage();
            OnPropertyChanged(nameof(_connectionDropBoxItems));
        }

        private async void CoreWindow_OnKeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (BlobListView.Visibility != Visibility.Visible) return;

            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && shift.HasFlag(CoreVirtualKeyStates.Down) &&
                args.VirtualKey == VirtualKey.S)
            {
                await SaveAllBlobs();
                await ShowMessageToUser("All blobs have been saved.");
            }
            else if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && args.VirtualKey == VirtualKey.S)
            {
                if (BlobContentTxt.FocusState == FocusState.Unfocused) return;


                var selectedBlob = (BlobListItem)BlobListView.SelectedItem;
                if (selectedBlob == null) return;
                await SaveSelectedBlobContent(selectedBlob);
            }
        }

        #region Buttons_Click

        private async void EditConnectionsButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowSaveConfirmDialog(
                () => this.Frame.Navigate(typeof(StorageSelectionPage), _connectionDropBoxItems));
        }

        private async void SaveBlobButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBlob = (BlobListItem)BlobListView.SelectedItem;
            if (selectedBlob == null) return;
            await SaveSelectedBlobContent(selectedBlob);
        }

        private async void DeleteBlobButton_CLick(object sender, RoutedEventArgs e)
        {
            var selectedBlob = (BlobListItem)BlobListView.SelectedItem;
            if (selectedBlob == null) return;

            var deleteConfirmDialog = new MessageDialog($"Do you want to delete \"{selectedBlob.Blob.Name}\" blob ?");
            deleteConfirmDialog.Commands.Add(new UICommand("Yes", cmd => DeleteBlob(selectedBlob)));
            deleteConfirmDialog.Commands.Add(new UICommand("No"));
            await deleteConfirmDialog.ShowAsync();
        }

        private void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectToCloud();
        }

        private async void AddBlobButton_Click(object sender, RoutedEventArgs e)
        {
            DisableSelection();
            try
            {
                var newBlobDialog =new NewBlobDialog(
                    new ObservableCollection<CloudBlockBlob>(_blobListViewItems.Select(x => x.Blob)));

                var dialogResults = await newBlobDialog.ShowAsync();
                if (dialogResults == ContentDialogResult.Secondary)
                {
                    InfoMessageText.Text = "Working ...";

                    var newBlob = new BlobListItem
                    {
                        Blob = await _storageClient.AddBlobAsync(
                            (CloudBlobContainer)ContainerDropBox.SelectedItem, newBlobDialog.BlobName, "")
                    };

                    _blobListViewItems.Add(newBlob);
                    OnPropertyChanged(nameof(_blobListViewItems));

                    EnableSelection();

                    BlobListView.SelectedItem = newBlob;
                    await ShowMessageToUser($"Blob \"{newBlob.Blob.Name}\" have been created.");
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

        private async void SaveContainerButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedContainer = (CloudBlobContainer)ContainerDropBox.SelectedItem;
            if(selectedContainer == null)
            {
                await ShowDialogToUser("You have not selected a container.");
                return;
            }

            var folderPicker = new FolderPicker { SuggestedStartLocation = PickerLocationId.Desktop };
            folderPicker.FileTypeFilter.Add("*");
            var selectedFolder = await folderPicker.PickSingleFolderAsync();
            if (selectedFolder == null) return;

            var localStorage = new LocalStorageService();
            InfoMessageText.Text = "Working ...";
            await localStorage.SaveContainerInSelectedFolder(selectedFolder, _storageClient, selectedContainer);
            await ShowMessageToUser($"Container have been saved in :\"{selectedFolder.Path}\"");
        }

        private async void LoadToContainerButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedContainer = (CloudBlobContainer)ContainerDropBox.SelectedItem;
            if (selectedContainer == null)
            {
                await ShowDialogToUser("You have not selected a container.");
                return;
            }

            var folderPicker = new FolderPicker { SuggestedStartLocation = PickerLocationId.Desktop };
            folderPicker.FileTypeFilter.Add("*");
            var selectedFolder = await folderPicker.PickSingleFolderAsync();
            if (selectedFolder == null) return;

            InfoMessageText.Text = "Working ...";
            var newBlobList = await _storageClient.SaveFilesToSelectedContainer(await selectedFolder.GetFilesAsync(), selectedContainer);
            newBlobList.ForEach(x => _blobListViewItems.Add(new BlobListItem{Blob = x}));
            await ShowMessageToUser("All data from files have been loaded.");
        }

        private async void DeleteMBlobButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBlobs = BlobListView.SelectedItems.Cast<BlobListItem>().ToList();
            if (!selectedBlobs.Any()) return;

            BlobListView.SelectedIndex = -1;
            var deleteConfirmDialog = new MessageDialog("Do you want to delete selected blobs ?");
            deleteConfirmDialog.Commands.Add(new UICommand("Yes", cmd => DeleteBlobList(selectedBlobs)));
            deleteConfirmDialog.Commands.Add(new UICommand("No"));
            await deleteConfirmDialog.ShowAsync();
        }
        #endregion

        #region Lists_SelectionChanged
        private async void ConnectionDropBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ShowSaveConfirmDialog(ConnectToCloud);
        }

        private async void BlobListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedBlob = (BlobListItem)BlobListView.SelectedItem;
            if (selectedBlob == null) return;
            if (BlobListView.SelectedItems.Count != 1) return;

            if (selectedBlob.LocalBlobContent == null)
            {
                await PopulateBlob(selectedBlob, true);
            }
            else
            {
                await PopulateBlob(selectedBlob, false);
            }
        }

        private async void ContainerDropBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ShowSaveConfirmDialog(async () =>
            {
                DisableSelection();
                HideBlobControls();
                HideBlobListControls();
                SearchBlobTxt.Visibility = Visibility.Collapsed;

                var selectedContainer = (CloudBlobContainer) ContainerDropBox.SelectedItem;
                if (selectedContainer == null) return;

                try
                {
                    InfoMessageText.Text = "Working ...";
                    var containerBlobList = (await _storageClient.GetBlobsFromCloudAsync(selectedContainer))
                        .Results.Cast<CloudBlockBlob>().ToList();

                    var blobListItems = containerBlobList
                        .Select(blob => new BlobListItem {Blob = blob}).ToList();

                    _blobListViewItems = new ObservableCollection<BlobListItem>(blobListItems);
                    OnPropertyChanged(nameof(_blobListViewItems));
                    ShowBlobListControls();
                    SearchBlobTxt.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    await ShowDialogToUser(ex.Message);
                }

                InfoMessageText.Text = "";
                EnableSelection();

            });
        }

        #endregion

        private async Task SaveSelectedBlobContent(BlobListItem selectedBlob)
        {
            var contentText = selectedBlob.LocalBlobContent;

            if (!IsJsonValid(contentText))
            {
                await ShowDialogToUser("Json format is invalid.");
                return;
            }

            try
            {
                InfoMessageText.Text = "Working ...";
                await _storageClient.UploadDataToBlobAsync(selectedBlob.Blob, selectedBlob.LocalBlobContent);
                selectedBlob.CloudBlobContent = selectedBlob.LocalBlobContent;
                selectedBlob.Saved = true;
                await ShowMessageToUser($"Blob {selectedBlob.Blob.Name} content have been saved.");
            }
            catch (Exception ex)
            {
                InfoMessageText.Text = "";
                await ShowDialogToUser(ex.Message);
            }
        }

        private async Task<BlobListItem> SaveAllBlobs()
        {
            foreach (var blob in _blobListViewItems)
            {
                if (blob.LocalBlobContent == null) continue;

                var contentText = blob.LocalBlobContent;

                if (!IsJsonValid(contentText))
                {
                    await ShowDialogToUser("Json format is invalid.");
                    return blob;
                }

                try
                {
                    await _storageClient.UploadDataToBlobAsync(blob.Blob, blob.LocalBlobContent);
                    blob.CloudBlobContent = blob.LocalBlobContent;
                    blob.Saved = true;
                }
                catch (Exception ex)
                {
                    InfoMessageText.Text = "";
                    await ShowDialogToUser(ex.Message);
                }
            }
            return default(BlobListItem);
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

        private async void ConnectToCloud()
        {
            DisableSelection();
            HideBlobControls();
            BlobListView.Visibility = Visibility.Collapsed;
            SearchBlobTxt.Visibility = Visibility.Collapsed;
            AddBlobButton.Visibility = Visibility.Collapsed;
            DeleteMBlobsButton.Visibility = Visibility.Collapsed;

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

        private async Task PopulateContainerDropBox()
        {
            var containerSegment = await _storageClient.GetContainersFromCloudAsync();
            _containerDropBoxItems = new ObservableCollection<CloudBlobContainer>(containerSegment.Results.ToList());
            OnPropertyChanged(nameof(_containerDropBoxItems));
        }

        private async Task PopulateBlob(BlobListItem selectedBlob, bool fromStorage)
        {
            DisableSelection();
            try
            {
                InfoMessageText.Text = "Working ...";
                BlobNameTxt.Text = selectedBlob.Blob.Name;
                var blobData = await _storageClient.GetDataFromBlobAsync(selectedBlob.Blob);
                if (fromStorage)
                {
                    selectedBlob.LocalBlobContent = blobData;
                    selectedBlob.CloudBlobContent = blobData;
                    BlobContentTxt.Text = blobData;
                }
                else
                {
                    BlobContentTxt.Text = selectedBlob.LocalBlobContent;
                }
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
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            if (sender.Text.Any())
            {
                _searchSuggestions = new ObservableCollection<string>
                (_blobListViewItems.Select(x => x.Blob.Name)
                    .Where(x => x.IndexOf(sender.Text, StringComparison.CurrentCultureIgnoreCase) >= 0));
                OnPropertyChanged(nameof(_searchSuggestions));
            }
            else
            {
                _searchSuggestions = new ObservableCollection<string>(_blobListViewItems.Select(x => x.Blob.Name));
                OnPropertyChanged(nameof(_searchSuggestions));
            }
        }

        private void SearchBlobTxt_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
                BlobListView.SelectedItem = _blobListViewItems.Single(x => x.Blob.Name.Equals((string)args.ChosenSuggestion));
        }

        private async void DeleteBlob(BlobListItem selectedBlob)
        {
            try
            {
                InfoMessageText.Text = "Working ...";
                await _storageClient.RemoveBlobAsync(selectedBlob.Blob);
                _blobListViewItems.Remove(selectedBlob);

                HideBlobControls();

                await ShowMessageToUser($"Blob \"{selectedBlob.Blob.Name}\" have been deleted.");
            }
            catch (Exception ex)
            {
                await ShowDialogToUser(ex.Message);
            }
        }

        private async void DeleteBlobList(List<BlobListItem> selectedBlobList)
        {
            InfoMessageText.Text = "Working ...";
            foreach (var selectedBlob in selectedBlobList)
            {
                try
                {
                    await _storageClient.RemoveBlobAsync(selectedBlob.Blob);
                    _blobListViewItems.Remove(selectedBlob);
                    HideBlobControls();
                }
                catch (Exception ex)
                {
                    await ShowDialogToUser(ex.Message);
                }
            }
            await ShowMessageToUser("Selected blobs have been deleted.");
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

        private void ShowBlobListControls()
        {
            BlobListView.Visibility = Visibility.Visible;
            AddBlobButton.Visibility = Visibility.Visible;
            DeleteMBlobsButton.Visibility = Visibility.Visible;
        }

        private void HideBlobListControls()
        {
            BlobListView.Visibility = Visibility.Collapsed;
            AddBlobButton.Visibility = Visibility.Collapsed;
            DeleteMBlobsButton.Visibility = Visibility.Collapsed;
        }

        private void BlobContentTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedBlob = (BlobListItem)BlobListView.SelectedItem;
            if (selectedBlob == null) return;
            selectedBlob.LocalBlobContent = BlobContentTxt.Text;
            selectedBlob.Saved = selectedBlob.LocalBlobContent.Equals(selectedBlob.CloudBlobContent);
        }

        private async Task ShowSaveConfirmDialog(Action actionAfterSaving)
        {
            if (_blobListViewItems.All(x => x.Saved))
            {
                actionAfterSaving.Invoke();
            }
            else
            {
                var exitDialog = new MessageDialog("Do you want to exit without saving ?");
                exitDialog.Commands.Add(new UICommand("Save all and exit.", async s =>
                {
                    var badBlob = await SaveAllBlobs();
                    if (badBlob == null)
                    {
                        actionAfterSaving.Invoke();
                    }
                    else
                    {
                        BlobListView.SelectedItem = badBlob;
                    }

                }));
                exitDialog.Commands.Add(new UICommand("Continue without saving.", s =>
                {
                    actionAfterSaving.Invoke();
                }));
                await exitDialog.ShowAsync();
            }
        }
    }
}
