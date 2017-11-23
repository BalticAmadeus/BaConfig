using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace ConfigurationStorageManager
{
    public sealed partial class MainPage : Page
    {
        private const string VAULT_NAME = "ConnectionStrings";

        private ObservableCollection<CloudBlobContainer> _containerDropBoxItems = new ObservableCollection<CloudBlobContainer>();
        private ObservableCollection<ConnectionModel> _connectionDropBoxItems = new ObservableCollection<ConnectionModel>();

        private List<FilteredItem> _allItemsForFiltering;


        public MainPage()
        {
            this.InitializeComponent();
            GetConnectionsFromStorage();
            _allItemsForFiltering = new List<FilteredItem>();
        }

        #region Buttons_Click
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(StorageSelectionPage), _connectionDropBoxItems);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (BlobPivot.SelectedItem == null) return;
            var pivotItemContent = (PivotItemTag)((PivotItem)BlobPivot.SelectedItem).Tag;

            if (pivotItemContent != null)
            {
                var contentText = pivotItemContent.Content.Text;
                if (!IsJsonValid(contentText))
                {
                    ShowDialogToUser("Json format is invalid.");
                }

                try
                {
                    MessageText.Text = "Working ...";
                    await CloudStorageManagetment.UploadDataToBlobAsync(pivotItemContent.Blob, contentText);
                    await ShowMessageToUser($"Blob {pivotItemContent.Blob.Name} content have been saved.");
                }
                catch(Exception ex)
                {
                    MessageText.Text = "";
                    ShowDialogToUser(ex.InnerException.Message);
                }
            }
        }

        private async void DeleteButton_CLick(object sender, RoutedEventArgs e)
        {
            var selectedPivotItem = (PivotItem)BlobPivot.SelectedItem;
            var blobContent = (PivotItemTag)selectedPivotItem.Tag;
            if (blobContent == null) return;

            var dialogYes = new UICommand("Yes", async cmd =>
            {
                try
                {
                    MessageText.Text = "Working ...";
                    await CloudStorageManagetment.RemoveBlobAsync(blobContent.Blob);
                    BlobPivot.Items.Remove(selectedPivotItem);
                    await ShowMessageToUser($"Blob \"{blobContent.Blob.Name}\" have been deleted.");
                    _allItemsForFiltering.RemoveAll(x=>x.Blob.Equals(blobContent.Blob));
                }
                catch(Exception ex)
                {
                    ShowDialogToUser(ex.InnerException.Message);
                }
            });

            var dialogNo = new UICommand("No");

            var deleteConfirmDialog = new MessageDialog($"Do you want to delete \"{blobContent.Blob.Name}\" blob ?");
            deleteConfirmDialog.Commands.Add(dialogYes);
            deleteConfirmDialog.Commands.Add(dialogNo);
            await deleteConfirmDialog.ShowAsync();
        }

        private void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionList.IsEnabled = false;
            ContainerDropBoxList.IsEnabled = false;

            ConnectToStorage();

            ReconnectButton.Visibility = Visibility.Collapsed;
            ConnectionList.IsEnabled = true;
            ContainerDropBoxList.IsEnabled = true;
        }

        private async void NewBlobButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionList.SelectedItem == null || ContainerDropBoxList.SelectedItem == null) return;

            var newBlobDialog = new NewBlobDialog(BlobPivot.Items.ToList());
            var result = await newBlobDialog.ShowAsync();
            if (result != ContentDialogResult.Secondary) return;

            var blobName = newBlobDialog.BlobName;
            var blobContent = newBlobDialog.BlobContent;

            try
            {
                MessageText.Text = "Working ...";
                var newblob = await CloudStorageManagetment.AddNewBlobAsync(
                (CloudBlobContainer)ContainerDropBoxList.SelectedItem, blobName, blobContent);

                var positionToInsert = BlobPivot.Items.Count() - 1;

                var newPivotItem = await GetPivotItemFromBlob(newblob);
                if (newPivotItem != null)
                    BlobPivot.Items.Insert(positionToInsert, newPivotItem);

                await ShowMessageToUser($"Blob \"{blobName}\" have been created.");

            }
            catch (Exception ex)
            {
                MessageText.Text = "";
                ShowDialogToUser(ex.InnerException.Message);
            }
        }
        #endregion

        #region Lists_SelectionChanged
        private void ConnectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConnectionList.IsEnabled = false;
            ContainerDropBoxList.IsEnabled = false;

            ConnectToStorage();

            ConnectionList.IsEnabled = true;
            ContainerDropBoxList.IsEnabled = true;
        }

        private async void ContainerDropBoxList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContainerDropBoxList.IsEnabled = false;
            ConnectionList.IsEnabled = false;
            BlobPivot.Items.Clear();
            _allItemsForFiltering.Clear();

            var selectedContainer = (CloudBlobContainer)ContainerDropBoxList.SelectedItem;
            if (selectedContainer == null) return;

            try
            {
                MessageText.Text = "Working ...";
                var containerBlobSegment = await CloudStorageManagetment.GetBlobsFromCloudAsync(selectedContainer);
                var containerBlobList = containerBlobSegment.Results.ToList();
                _allItemsForFiltering.Clear();

                foreach (CloudBlockBlob blob in containerBlobList)
                {
                    var newPivotItem = await GetPivotItemFromBlob(blob);
                    if (newPivotItem != null)
                        BlobPivot.Items.Add(newPivotItem);
                }

                if (BlobPivot.Items.Count != 1)
                {
                    SaveButton.Visibility = Visibility.Visible;
                    DeletButton.Visibility = Visibility.Visible;
                }
            }
            catch(Exception ex)
            {
                ShowDialogToUser(ex.InnerException.Message);
            }

            MessageText.Text = "";
            ContainerDropBoxList.IsEnabled = true;
            ConnectionList.IsEnabled = true;
        }

        private void BlobPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPivotItem = (PivotItem)BlobPivot.SelectedItem;
            if (selectedPivotItem == null) return;

            var blobContent = (PivotItemTag)selectedPivotItem.Tag;
            if (blobContent == null)
            {
                DeletButton.Visibility = Visibility.Collapsed;
                SaveButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                DeletButton.Visibility = Visibility.Visible;
                SaveButton.Visibility = Visibility.Visible;
            }
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
            var connection = (ConnectionModel)ConnectionList.SelectedItem;
            if (connection == null) return;

            BlobPivot.Items.Clear();
            _containerDropBoxItems.Clear();
            _allItemsForFiltering.Clear();

            var isConnectedToStorage = CloudStorageManagetment.CreateConnectionWithCloud(connection.ConnectionString);
            if (!isConnectedToStorage)
            {
                ShowDialogToUser("Error: Failed connect to cloud storage !");
                ReconnectButton.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                MessageText.Text = "Working ...";
                await PopulateContainerDropBoxList();
                await ShowMessageToUser("Successfully connected to cloud storage.");
            }
            catch (Exception ex)
            {
                MessageText.Text = "";
                ShowDialogToUser(ex.InnerException.Message);
                ReconnectButton.Visibility = Visibility.Visible;
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
            catch(Exception ex)
            {
                ShowDialogToUser(ex.InnerException.Message);
            }
        }

        private async Task PopulateContainerDropBoxList()
        {
            var containerSegment = await CloudStorageManagetment.GetContainersFromCloudAsync();
            containerSegment.Results.ToList().ForEach(x => _containerDropBoxItems.Add(x));
        }

        private async Task<PivotItem> GetPivotItemFromBlob(CloudBlockBlob blob)
        {
            try
            {
                var blobData = await CloudStorageManagetment.GetDataFromBlobAsync(blob);
                var pivotItem = new PivotItem { Header = blob.Name };
                var grid = new Grid();

                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var contentEditorLabel = new TextBlock { Text = "Editor:" };

                var contentEditor = new TextBox
                {
                    IsSpellCheckEnabled = false,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    Margin = new Thickness(0, 10, 0, 30)
                };

                contentEditor.Text = blobData;
                ScrollViewer.SetVerticalScrollBarVisibility(contentEditor, ScrollBarVisibility.Visible);

                Grid.SetRow(contentEditorLabel, 0);
                Grid.SetRow(contentEditor, 1);

                grid.Children.Add(contentEditorLabel);
                grid.Children.Add(contentEditor);

                pivotItem.Tag = new PivotItemTag { Blob = blob, Content = contentEditor };
                pivotItem.Content = grid;
                
                _allItemsForFiltering.Add(new FilteredItem { Blob = blob, PivotItem = pivotItem });

                return pivotItem;

            }
            catch(Exception ex)
            {
                ShowDialogToUser(ex.InnerException.Message);
                return null;
            }
        }

        private  async Task ShowMessageToUser(string message)
        {
            MessageText.Text = message;
            await Task.Delay(3500);
            MessageText.Text = "";
        }

        private async void ShowDialogToUser(string message)
        {
            var messageDialog = new MessageDialog(message);
            await messageDialog.ShowAsync();
        }

        private void SearchBlobTxt_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                try
                {
                    if (sender.Text.Count() > 0)
                    {
                        var filteredItems = _allItemsForFiltering.Where(x => x.Blob.Name.IndexOf(sender.Text, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
                        sender.ItemsSource = filteredItems;
                    }
                    else
                    {
                        sender.ItemsSource = _allItemsForFiltering;
                    }
                }
                catch
                {

                }
            }
        }

        private void SearchBlobTxt_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var suggestion = (FilteredItem)args.ChosenSuggestion;
            if (suggestion == null) return;
            if (((PivotItem)BlobPivot.SelectedItem).Equals(suggestion.PivotItem)) return;

            BlobPivot.SelectedItem= ((FilteredItem)args.ChosenSuggestion).PivotItem;
        }

        private void SearchBlobTxt_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = (FilteredItem)args.SelectedItem;
            if (selectedItem == null) return;
            if (((PivotItem)BlobPivot.SelectedItem).Equals(selectedItem.PivotItem)) return;

            BlobPivot.SelectedItem = ((FilteredItem)args.SelectedItem).PivotItem;
        }
    }
}
