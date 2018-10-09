using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;

namespace AzureStorageImageManager.ViewModel
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<ConfigViewModel>();
        }

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();

        public ConfigViewModel ConfigView => ServiceLocator.Current.GetInstance<ConfigViewModel>();

        public static void Cleanup()
        {
        }
    }
}