using System.ComponentModel;

namespace ConfigurationStorageManager
{
    public class ConnectionModel : INotifyPropertyChanged
    {
        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }

        private string _newConnectionString;
        public string NewConnectionString
        {
            get { return _newConnectionString; }
            set
            {
                _newConnectionString = value;
                OnPropertyChanged("NewConnectionString");
            }
        }

        private string _newConnectionName;
        public string NewConnectionName
        {
            get { return _newConnectionName; }
            set
            {
                _newConnectionName = value;
                OnPropertyChanged("NewConnectionName");
            }
        }

        private bool _isEnabled = false;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        private string _showPasswordParam = "Hidden";
        public string ShowPasswordParam
        {
            get { return _showPasswordParam; }
            set
            {
                _showPasswordParam = value;
                OnPropertyChanged("ShowPasswordParam");
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
