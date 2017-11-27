using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ConfigurationStorageManager
{
    public sealed partial class StorageSelectionPage : Page
    {
        private const string VAULT_NAME = "ConnectionStrings";
        private ObservableCollection<ConnectionModel> _connectionList;

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
                SaveConnectionToStorage(connection);
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
            DeleteConnectionFromStorage(connection);
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

            SaveConnectionToStorage(newConnection);
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

            if (!_connectionList.Count().Equals(0))
            {
                var storage = new PasswordVault();
                var connectionListFromStorage = storage.FindAllByResource(VAULT_NAME);

                if (connectionListFromStorage.SingleOrDefault(x => x.UserName.Equals(ConnectionNameTxt.Text)) != null)
                {
                    var errorMessage = new MessageDialog("Such connection name already exists.");
                    await errorMessage.ShowAsync();
                    return false;
                }
            }
            return true;
        }

        private void SaveConnectionToStorage(ConnectionModel connection)
        {
            var storage = new PasswordVault();
            try
            {
                storage.Retrieve(VAULT_NAME, connection.ConnectionName);
                UpdateConnectionToStorage(connection);
            }
            catch
            {
                storage.Add(new PasswordCredential(VAULT_NAME, connection.NewConnectionName, connection.NewConnectionString));
            }
        }

        private void UpdateConnectionToStorage(ConnectionModel connection)
        {
            var storage = new PasswordVault();
            storage.Remove(new PasswordCredential(VAULT_NAME, connection.ConnectionName, connection.ConnectionString));
            storage.Add(new PasswordCredential(VAULT_NAME, connection.NewConnectionName, connection.NewConnectionString));
            connection.UpdateConnection();
        }

        private void DeleteConnectionFromStorage(ConnectionModel connection)
        {
            var storage = new PasswordVault();
            storage.Remove(new PasswordCredential(VAULT_NAME, connection.ConnectionName, connection.NewConnectionString));
        }
    }
}
