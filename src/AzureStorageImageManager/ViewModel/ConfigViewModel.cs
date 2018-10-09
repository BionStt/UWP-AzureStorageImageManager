using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace AzureStorageImageManager.ViewModel
{
    public class ConfigViewModel : ViewModelBase
    {
        private string _verifyMessage;
        private bool _isGoToMainEnabled;
        public AppSettings AppSettings { get; set; }

        public RelayCommand CommandVerify { get; set; }

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; RaisePropertyChanged(); }
        }

        public bool IsGoToMainEnabled
        {
            get => _isGoToMainEnabled;
            set { _isGoToMainEnabled = value; RaisePropertyChanged(); }
        }

        public string VerifyMessage
        {
            get => _verifyMessage;
            set
            {
                _verifyMessage = value;
                RaisePropertyChanged();
            }
        }

        public ConfigViewModel()
        {
            AppSettings = new AppSettings();
            CommandVerify = new RelayCommand(async () =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(AppSettings.StorageAccountName) &&
                        !string.IsNullOrEmpty(AppSettings.StorageAccountKey))
                    {
                        IsBusy = true;

                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse($"DefaultEndpointsProtocol=https;AccountName={AppSettings.StorageAccountName};AccountKey={AppSettings.StorageAccountKey}");
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                        var containers = await blobClient.ListContainersSegmentedAsync(null);

                        VerifyMessage = $"Test Success. Please click the continue button on the app bar.";
                        IsGoToMainEnabled = true;

                        IsBusy = false;
                    }
                    else
                    {
                        VerifyMessage = $"Values can not be null or empty.";
                    }
                }
                catch (Exception e)
                {
                    IsGoToMainEnabled = false;
                    VerifyMessage = e.Message;
                }
            });
        }
    }
}
