using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using AzureStorageImageManager.Model;
using Edi.UWP.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Edi.UWP.Helpers.Extensions;

namespace AzureStorageImageManager.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private RelayCommand _commandRefresh;
        private string _containerDisplayName;
        private ObservableCollection<CloudBlobContainer> _containers;
        private bool _isBusy;
        private bool _isContainerInited;
        private bool _isDeleteButtonEnabled;
        private bool _isRefereshEnabled;
        private ObservableCollection<BlobImage> _listBlobItems;
        private BlobImage _selectedImage;
        private ObservableCollection<BlobImage> _selectedImages;
        private RelayCommand<IList<object>> _selectionChangedCommand;
        private CloudBlobContainer _selectedContainer;

        public MainViewModel()
        {
            AppSettings = new AppSettings();
            if (IsInDesignMode)
            {
                SelectedContainer = new CloudBlobContainer(new Uri("http://some.windows.net"));
                ContainerDisplayName = "Test Container";
            }
            else
            {
                if (!string.IsNullOrEmpty(AppSettings.StorageAccountName) &&
                    !string.IsNullOrEmpty(AppSettings.StorageAccountKey))
                {
                    RefreshContainerAsync();
                }

                else
                {
                    ContainerDisplayName = "Unconfigured";
                }
            }

            CommandRefresh = new RelayCommand(async () => await GetImageListAsync());
            CommandDelete = new RelayCommand(async () => await DeleteSelectedItemsAsync());
            SelectedImages = new ObservableCollection<BlobImage>();
        }

        public AppSettings AppSettings { get; set; }

        public CloudBlobContainer SelectedContainer
        {
            get => _selectedContainer;
            set
            {
                _selectedContainer = value;
                RaisePropertyChanged();
                GetImageListAsync();
            }
        }

        public ObservableCollection<CloudBlobContainer> Containers
        {
            get => _containers;
            set
            {
                _containers = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand CommandRefresh
        {
            get => _commandRefresh;
            set
            {
                _commandRefresh = value;
                RaisePropertyChanged();
            }
        }

        public string ContainerDisplayName
        {
            get => _containerDisplayName;
            set
            {
                _containerDisplayName = value;
                RaisePropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                IsRefereshEnabled = !value;
                RaisePropertyChanged();
            }
        }

        public bool IsRefereshEnabled
        {
            get => _isRefereshEnabled;
            set
            {
                _isRefereshEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool IsDeleteButtonEnabled
        {
            get => _isDeleteButtonEnabled;
            set
            {
                _isDeleteButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        public BlobImage SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<BlobImage> ListBlobItems
        {
            get => _listBlobItems;
            set
            {
                _listBlobItems = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<BlobImage> SelectedImages
        {
            get => _selectedImages;
            set
            {
                _selectedImages = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand<IList<object>> SelectionChangedCommand
        {
            get
            {
                return _selectionChangedCommand ?? (_selectionChangedCommand = new RelayCommand<IList<object>>(
                           items =>
                           {
                               SelectedImages.Clear();
                               foreach (var item in items)
                               {
                                   if (item is BlobImage img)
                                       SelectedImages.Add(img);
                               }
                               IsDeleteButtonEnabled = SelectedImages.Any();
                           }
                       ));
            }
        }
        
        #region Delete

        public RelayCommand CommandDelete { get; set; }

        public async Task DeleteSingleItemAsync(BlobImage img)
        {
            var blockBlob = SelectedContainer.GetBlockBlobReference(img.FileName);
            await blockBlob.DeleteAsync();
            ListBlobItems.Remove(img);
            RaisePropertyChanged(nameof(ListBlobItems));
        }

        public async Task DeleteSelectedItemsAsync()
        {
            IsBusy = true;
            foreach (var item in SelectedImages)
            {
                var blockBlob = SelectedContainer.GetBlockBlobReference(item.FileName);
                await blockBlob.DeleteAsync();
            }
            IsBusy = false;
            await GetImageListAsync();
        }

        #endregion

        #region Upload

        public async Task<KeyValuePair<bool, string>> UploadImagesAsync(IReadOnlyList<StorageFile> sFiles)
        {
            IsBusy = true;
            try
            {
                foreach (var storageFile in sFiles)
                {
                    var blockBlob = SelectedContainer.GetBlockBlobReference(storageFile.Name);
                    await blockBlob.UploadFromFileAsync(storageFile);
                }

                IsBusy = false;
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e)
            {
                IsBusy = false;
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        #endregion

        public async Task RenameAsync(string oldName, string newName)
        {
            IsBusy = true;
            CloudBlockBlob source = (CloudBlockBlob)await SelectedContainer.GetBlobReferenceFromServerAsync(oldName);
            CloudBlockBlob target = SelectedContainer.GetBlockBlobReference(newName);

            await target.StartCopyAsync(source);

            while (target.CopyState.Status == CopyStatus.Pending)
                await Task.Delay(100);

            if (target.CopyState.Status != CopyStatus.Success)
                throw new Exception("Rename failed: " + target.CopyState.Status);

            await source.DeleteAsync();
            IsBusy = false;
        }

        public async Task RefreshContainerAsync()
        {
            var storageAccount =
                CloudStorageAccount.Parse(
                    $"DefaultEndpointsProtocol=https;AccountName={AppSettings.StorageAccountName};AccountKey={AppSettings.StorageAccountKey}");
            var blobClient = storageAccount.CreateCloudBlobClient();
            var containers = await blobClient.ListContainersSegmentedAsync(null);
            Containers = containers.Results.ToObservableCollection();
            _isContainerInited = true;
        }

        public async Task GetImageListAsync()
        {
            if (!_isContainerInited)
            {
                await RefreshContainerAsync();
            }

            IsBusy = true;
            ListBlobItems = new ObservableCollection<BlobImage>();

            var blobs = await SelectedContainer.ListBlobsSegmentedAsync(null);

            var listBlobProperties = (from item in blobs.Results
                                      where item.GetType() == typeof(CloudBlockBlob)
                                      select (CloudBlockBlob)item
                                      into blob
                                      select new BlobImage(blob.Properties.LastModified, blob.Uri)
                                      {
                                          FileName = blob.Name
                                      })
                                    .OrderByDescending(p => p.LastModified)
                                    .ToList();

            ListBlobItems = listBlobProperties.ToObservableCollection();
            IsBusy = false;
        }
    }
}