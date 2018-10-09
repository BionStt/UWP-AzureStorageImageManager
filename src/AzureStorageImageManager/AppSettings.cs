using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AzureStorageImageManager
{
    public class AppSettings : INotifyPropertyChanged
    {
        public string StorageAccountName
        {
            get => ReadSettings(nameof(StorageAccountName), string.Empty);
            set
            {
                SaveSettings(nameof(StorageAccountName), value);
                NotifyPropertyChanged();
            }
        }

        public string StorageAccountKey
        {
            get => ReadSettings(nameof(StorageAccountKey), string.Empty);
            set
            {
                SaveSettings(nameof(StorageAccountKey), value);
                NotifyPropertyChanged();
            }
        }

        public ApplicationDataContainer SettingsContainer { get; set; }

        public AppSettings()
        {
            SettingsContainer = ApplicationData.Current.RoamingSettings;
        }

        private void SaveSettings(string key, object value)
        {
            SettingsContainer.Values[key] = value;
        }

        private T ReadSettings<T>(string key, T defaultValue)
        {
            if (SettingsContainer.Values.ContainsKey(key))
            {
                return (T)SettingsContainer.Values[key];
            }
            if (null != defaultValue)
            {
                return defaultValue;
            }
            return default(T);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName]string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
