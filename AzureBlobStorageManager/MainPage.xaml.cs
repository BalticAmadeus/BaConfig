using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ConfigurationStorageManager
{
    public sealed partial class MainPage : Page
    {
        private const string VAULT_NAME = "ConnectionStrings";

        private ObservableCollection<CloudBlobContainer> _containerDropBoxItems = new ObservableCollection<CloudBlobContainer>();
        private ObservableCollection<ConnectionModel> _connectionDropBoxItems = new ObservableCollection<ConnectionModel>();

        private TextBox _newBlobContentEditor;
        private TextBox _newBlobNameTxt;

        public MainPage()
        {
            this.InitializeComponent();
            GetConnectionsFromStorage();
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

            if (pivotItemContent == null)
            {
                if (!IsBlobValid(_newBlobNameTxt.Text, _newBlobContentEditor.Text)) return;

                try
                {
                    ShowMessageToUser("Working ...");
                    var newblob = await CloudStorageManagetment.AddNewBlobAsync(
                    (CloudBlobContainer)ContainerDropBoxList.SelectedItem, _newBlobNameTxt.Text, _newBlobContentEditor.Text);

                    var positionToInsert = BlobPivot.Items.Count() - 1;

                    var newPivotItem = await GetPivotItemFromBlob(newblob);
                    if(newPivotItem != null)
                        BlobPivot.Items.Insert(positionToInsert, newPivotItem);

                    ShowMessageToUser($"Blob \"{_newBlobNameTxt.Text}\" have been created.");

                    _newBlobNameTxt.Text = "";
                    _newBlobContentEditor.Text = "";
                }
                catch(Exception ex)
                {
                    MessageText.Text = "";
                    ShowDialogToUser(ex.InnerException.Message);
                }
            }
            else
            {
                var contentText = pivotItemContent.Content.Text;
                if (!IsJsonValid(contentText))
                {
                    ShowDialogToUser("Json format is invalid.");
                }

                try
                {
                    ShowMessageToUser("Working ...");
                    await CloudStorageManagetment.UploadDataToBlobAsync(pivotItemContent.Blob, contentText);
                    ShowMessageToUser($"Blob {pivotItemContent.Blob.Name} content have been saved.");
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
                    ShowMessageToUser("Working ...");
                    await CloudStorageManagetment.RemoveBlobAsync(blobContent.Blob);
                    BlobPivot.Items.Remove(selectedPivotItem);
                    ShowMessageToUser($"Blob \"{blobContent.Blob.Name}\" have been deleted.");
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
            ConnectToStorage();
            ReconnectButton.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Lists_SelectionChanged
        private void ConnectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConnectToStorage();
        }

        private async void ContainerDropBoxList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BlobPivot.Items.Clear();
            var selectedContainer = (CloudBlobContainer)ContainerDropBoxList.SelectedItem;
            if (selectedContainer == null) return;
            try
            {
                MessageText.Text = "Working ...";
                var containerBlobSegment = await CloudStorageManagetment.GetBlobsFromCloudAsync(selectedContainer);
                var containerBlobList = containerBlobSegment.Results.ToList();

                foreach (CloudBlockBlob blob in containerBlobList)
                {
                    var newPivotItem = await GetPivotItemFromBlob(blob);
                    if (newPivotItem != null)
                        BlobPivot.Items.Add(newPivotItem);
                }

                BlobPivot.Items.Add(GetAddNewPivotItem());

                if (BlobPivot.Items.Count != 1)
                {
                    SaveButton.Visibility = Visibility.Visible;
                    DeletButton.Visibility = Visibility.Visible;
                }
                MessageText.Text = "";
            }
            catch(Exception ex)
            {
                MessageText.Text = "";
                ShowDialogToUser(ex.InnerException.Message);
            }
        }

        private void BlobPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPivotItem = (PivotItem)BlobPivot.SelectedItem;
            var blobContent = (PivotItemTag)selectedPivotItem.Tag;
            if (blobContent == null)
            {
                DeletButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (DeletButton.Visibility == Visibility.Collapsed)
                    DeletButton.Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Validations
        private bool IsBlobValid(string blobName, string blobContent)
        {
            if (blobName.Count().Equals(0) || blobContent.Count().Equals(0))
            {
               ShowDialogToUser("Blob name or blob content can not be empty.");
                return false;
            }

            if (!IsBlobNameValid(_newBlobNameTxt.Text))
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
            var blobPivotItemList = BlobPivot.Items.ToList();

            foreach (PivotItem pivotItem in blobPivotItemList)
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
        #endregion

        private async void ConnectToStorage()
        {
            var connection = (ConnectionModel)ConnectionList.SelectedItem;
            if (connection == null) return;

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
                ShowMessageToUser("Successfully connected to cloud storage.");
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
                return pivotItem;
            }
            catch(Exception ex)
            {
                ShowDialogToUser(ex.InnerException.Message);
                return null;
            }
        }

        private PivotItem GetAddNewPivotItem()
        {
            var icon = new AppBarButton() { Icon = new SymbolIcon(Symbol.Add) };
            var pivotItem = new PivotItem() { Header = icon };
            var grid = new Grid();

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _newBlobNameTxt = new TextBox
            {
                Header = "New blob name:",
                PlaceholderText = "Blob Name ...",
                Margin = new Thickness(5, 10, 0, 10)
            };

            var editorText = new TextBlock { Text = "Content:" };

            _newBlobContentEditor = new TextBox
            {
                TextWrapping = TextWrapping.Wrap,
                IsSpellCheckEnabled = false,
                PlaceholderText = "Blob Content ...",
                AcceptsReturn = true,
                Margin = new Thickness(0, 10, 0, 30)
            };

            ScrollViewer.SetVerticalScrollBarVisibility(_newBlobContentEditor, ScrollBarVisibility.Visible);

            Grid.SetRow(_newBlobNameTxt, 0);
            Grid.SetRow(editorText, 1);
            Grid.SetRow(_newBlobContentEditor, 2);

            grid.Children.Add(_newBlobNameTxt);
            grid.Children.Add(editorText);
            grid.Children.Add(_newBlobContentEditor);

            pivotItem.Content = grid;

            return pivotItem;
        }

        private  async void ShowMessageToUser(string message)
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
    }
}
