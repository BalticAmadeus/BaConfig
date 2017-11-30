﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI.Popups;

namespace ConfigurationStorageManager
{
    public class ConnectionStorageService
    {
        private const string VAULT_NAME = "ConnectionStrings";

        public async static void AddConnectionToStorage(ConnectionModel connection)
        {
            var storage = new PasswordVault();
            try
            {
                storage.Add(new PasswordCredential(VAULT_NAME, connection.NewConnectionName, connection.NewConnectionString));
            }
            catch (Exception ex)
            {
                var errorMessage = new MessageDialog(ex.Message);
                await errorMessage.ShowAsync();
            }
        }

        public static void SaveConnectionToStorage(ConnectionModel connection)
        {
            var storage = new PasswordVault();
            try
            {
                storage.Retrieve(VAULT_NAME, connection.ConnectionName);
                UpdateConnectionToStorage(connection);
            }
            catch
            {
                AddConnectionToStorage(connection);
            }
        }

        public async static void UpdateConnectionToStorage(ConnectionModel connection)
        {
            var storage = new PasswordVault();
            storage.Remove(new PasswordCredential(VAULT_NAME, connection.ConnectionName, connection.ConnectionString));

            try
            {
                storage.Add(new PasswordCredential(VAULT_NAME, connection.NewConnectionName, connection.NewConnectionString));
            }
            catch (Exception ex)
            {
                var errorMessage = new MessageDialog(ex.Message);
                await errorMessage.ShowAsync();
            }

            connection.UpdateConnection();
        }

        public static void DeleteConnectionFromStorage(ConnectionModel connection)
        {
            var storage = new PasswordVault();
            storage.Remove(new PasswordCredential(VAULT_NAME, connection.ConnectionName, connection.NewConnectionString));
        }

        public static ObservableCollection<ConnectionModel> GetAllConnectionsFromStorage()
        {
            var vault = new PasswordVault();
            var connectionList = new ObservableCollection<ConnectionModel>();

            try
            {
                var connectionListFromStorage = vault.FindAllByResource(VAULT_NAME);
                foreach (var connection in connectionListFromStorage)
                {
                    connection.RetrievePassword();
                    connectionList.Add(new ConnectionModel
                    {
                        ConnectionName = connection.UserName,
                        NewConnectionName = connection.UserName,
                        ConnectionString = connection.Password,
                        NewConnectionString = connection.Password
                    });
                }
            }
            catch { }
            return connectionList;
        }

        public static bool IsUniqueConnectionName(string name)
        {
            var storage = new PasswordVault();
            try
            {
                storage.Retrieve(VAULT_NAME, name);
                return false;
            }
            catch {}
            return true;
        }
    }
}