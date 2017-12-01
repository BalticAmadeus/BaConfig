using System.ComponentModel;

namespace ConfigurationStorageManager.Models
{
    public class ConnectionModel : INotifyPropertyChanged
    {
        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }

        private string _newConnectionString;
        public string NewConnectionString
        {
            get => _newConnectionString;
            set
            {
                _newConnectionString = value;
                OnPropertyChanged(nameof(NewConnectionString));
            }
        }

        private string _newConnectionName;
        public string NewConnectionName
        {
            get => _newConnectionName;
            set
            {
                _newConnectionName = value;
                OnPropertyChanged(nameof(NewConnectionName));
            }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled; 
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        private string _showPasswordParam = "Hidden";
        public string ShowPasswordParam
        {
            get => _showPasswordParam; 
            set
            {
                _showPasswordParam = value;
                OnPropertyChanged(nameof(ShowPasswordParam));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateConnection()
        {
            ConnectionName = NewConnectionName;
            ConnectionString = NewConnectionString;
        }

        public override string ToString()
        {
            return ConnectionName;
        }
    }
}
