using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using AzureStorageImageManager.Model;
using AzureStorageImageManager.ViewModel;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorageImageManager
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel MainViewModel { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            MainViewModel = this.DataContext as MainViewModel;

            if (MainViewModel != null && (string.IsNullOrEmpty(MainViewModel.AppSettings.StorageAccountName) ||
                                          string.IsNullOrEmpty(MainViewModel.AppSettings.StorageAccountKey)))
            {
                BtnConfig.Visibility = Visibility.Visible;
                GrdResults.Visibility = Visibility.Collapsed;
                BtnConfig.Click += BtnConfig_Click;
            }
        }

        private async void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            await DigSettings.ShowAsync();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!string.IsNullOrEmpty(MainViewModel.AppSettings.StorageAccountName) &&
                !string.IsNullOrEmpty(MainViewModel.AppSettings.StorageAccountKey))
            {
                await MainViewModel.RefreshContainerAsync();
            }
        }

        private async void BtnGoToConfig_OnClick(object sender, RoutedEventArgs e)
        {
            await DigSettings.ShowAsync();
        }

        private async void BtnUpload_OnClick(object sender, RoutedEventArgs e)
        {
            var pk = new FileOpenPicker()
            {
                CommitButtonText = "Select",
                FileTypeFilter = { ".jpg", ".png", ".bmp", ".jpeg", ".gif" },
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            var sFiles = await pk.PickMultipleFilesAsync();
            if (null != sFiles && sFiles.Any())
            {
                var kvp = await MainViewModel.UploadImagesAsync(sFiles);
                if (!kvp.Key)
                {
                    var dig = new MessageDialog(kvp.Value, "Error");
                    await dig.ShowAsync();
                }
                else
                {
                    await MainViewModel.GetImageListAsync();
                }
            }
        }

        private void BtnSelect_OnChecked(object sender, RoutedEventArgs e)
        {
            GrdResults.SelectionMode = ListViewSelectionMode.Multiple;
        }

        private void BtnSelect_OnUnchecked(object sender, RoutedEventArgs e)
        {
            GrdResults.SelectionMode = ListViewSelectionMode.Single;
        }

        private async void BtnAbout_OnClick(object sender, RoutedEventArgs e)
        {
            await DigAbout.ShowAsync();
        }

        private async void GrdResults_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                if (items.Any())
                {
                    var storageFile = items[0] as StorageFile;
                    if (storageFile != null)
                    {
                        var contentType = storageFile.ContentType;

                        StorageFolder folder = ApplicationData.Current.LocalFolder;

                        if (contentType == "image/png" ||
                            contentType == "image/jpeg" ||
                            contentType == "image/gif" ||
                            contentType == "image/bmp")
                        {
                            StorageFile newFile = await storageFile.CopyAsync(folder, storageFile.Name, NameCollisionOption.GenerateUniqueName);
                            await MainViewModel.UploadImagesAsync(new ReadOnlyCollection<StorageFile>(
                                new List<StorageFile>
                                {
                                    newFile
                                }));
                            await MainViewModel.GetImageListAsync();
                        }
                    }
                }
            }
        }

        private void GrdResults_OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "Holy High! It can drag!";
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.IsContentVisible = true;
                e.DragUIOverride.IsGlyphVisible = true;
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.IsBusy = true;
            var frameworkElement = e.OriginalSource as FrameworkElement;
            var datacontext = frameworkElement?.DataContext as BlobImage;
            if (datacontext != null)
            {
                var pk = new FileSavePicker()
                {
                    CommitButtonText = "Select",
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                };

                pk.FileTypeChoices.Add("Image File", new List<string>() { ".png", ".jpg", ".gif", ".jpeg", ".bmp" });
                pk.SuggestedFileName = datacontext.FileName;

                var sFile = await pk.PickSaveFileAsync();

                CloudBlockBlob blockBlob = MainViewModel.SelectedContainer.GetBlockBlobReference(datacontext.FileName);
                await blockBlob.DownloadToFileAsync(sFile);
            }

            MainViewModel.IsBusy = false;
        }

        private void BrdImg_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }

        private async void BtnPrivacy_OnClick(object sender, RoutedEventArgs e)
        {
            await DigPp.ShowAsync();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.IsBusy = true;
            var frameworkElement = e.OriginalSource as FrameworkElement;
            var datacontext = frameworkElement?.DataContext as BlobImage;
            if (datacontext != null)
            {
                await MainViewModel.DeleteSingleItemAsync(datacontext);
            }

            MainViewModel.IsBusy = false;
        }

        private async void BtnViewLarge_Click(object sender, RoutedEventArgs e)
        {
            var frameworkElement = e.OriginalSource as FrameworkElement;
            var datacontext = frameworkElement?.DataContext as BlobImage;
            if (datacontext != null)
            {
                await Windows.System.Launcher.LaunchUriAsync(datacontext.Uri);
            }
        }

        private async void DigSettings_OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await MainViewModel.RefreshContainerAsync();
        }

        private async void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            var frameworkElement = e.OriginalSource as FrameworkElement;
            var datacontext = frameworkElement?.DataContext as BlobImage;
            if (datacontext != null)
            {
                TxtOldName.Text = datacontext.FileName;
                TxtNewName.Text = "New_" + datacontext.FileName;
                TxtNewName.Focus(FocusState.Programmatic);
                await DigRename.ShowAsync();
            }
        }

        private async void DigRename_OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await MainViewModel.RenameAsync(TxtOldName.Text, TxtNewName.Text.Trim());
            await MainViewModel.GetImageListAsync();
        }
    }
}
