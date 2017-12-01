using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using ConfigurationStorageManager.Services;

namespace ConfigurationStorageManager
{
    public sealed partial class StorageSelectionPage : Page
    {
        private ObservableCollection<ConnectionModel> _connectionList;
        private ConnectionStorageService _connectionStorage = new ConnectionStorageService();

        public StorageSelectionPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _connectionList = (ObservableCollection<ConnectionModel>)e.Parameter;
        }

        #region Buttons_Click
        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var editButton = (Button)e.OriginalSource;
            var connection = (ConnectionModel)editButton.DataContext;
            if (connection.IsEnabled)
            {
                connection.IsEnabled = false;
                editButton.Content = "Edit";
                if (connection.NewConnectionName.Count().Equals(0) || connection.NewConnectionString.Count().Equals(0))
                {
                    var errorMessage = new MessageDialog("Connection name or connection string can not be empty.");
                    await errorMessage.ShowAsync();
                    connection.NewConnectionName = connection.ConnectionName;
                    connection.NewConnectionString = connection.ConnectionString;
                    return;
                }
                _connectionStorage.SaveConnectionToStorage(connection);
            }
            else
            {
                connection.IsEnabled = true;
                editButton.Content = "Save";
                connection.ConnectionString = connection.NewConnectionString;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var deleteButton = (Button)e.OriginalSource;
            var connection = (ConnectionModel)deleteButton.DataContext;
            _connectionStorage.DeleteConnectionFromStorage(connection);
            _connectionList.Remove(connection);
        }

        private void ShowConnectionStringBtn_Click(object sender, RoutedEventArgs e)
        {
            var showButton = (Button)e.OriginalSource;
            var connection = (ConnectionModel)showButton.DataContext;
            if (connection.ShowPasswordParam == "Hidden")
            {
                connection.ShowPasswordParam = "Visible";
                showButton.Content = "Hide connection string";
            }
            else
            {
                connection.ShowPasswordParam = "Hidden";
                showButton.Content = "Show connection string";
            }
        }

        private async void NewConnectionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!await IsConnectionValidAsync(ConnectionNameTxt.Text, ConnectionStringTxt.Text)) return;

            var newConnection = new ConnectionModel
            {
                ConnectionName = ConnectionNameTxt.Text,
                NewConnectionName = ConnectionNameTxt.Text,
                NewConnectionString = ConnectionStringTxt.Text
            };

            ConnectionNameTxt.Text = "";
            ConnectionStringTxt.Text = "";

            _connectionStorage.SaveConnectionToStorage(newConnection);
            _connectionList.Add(newConnection);
        }
        #endregion

        private async Task<bool> IsConnectionValidAsync(string connectionName, string connectionString)
        {
            if (connectionName.Count().Equals(0) || connectionString.Count().Equals(0))
            {
                var errorMessage = new MessageDialog("Connection name or connection string can not be empty.");
                await errorMessage.ShowAsync();
                return false;
            }

            if (!_connectionList.Any())
            {
                if (!_connectionStorage.IsUniqueConnectionName(connectionName))
                {
                    var errorMessage = new MessageDialog("Such connection name already exists.");
                    await errorMessage.ShowAsync();
                    return false;
                }
            }
            return true;
        }
    }
}
