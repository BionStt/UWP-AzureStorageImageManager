using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace AzureStorageImageManager
{
    public class SelectionChangedConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var gv = parameter as GridView;
            if (gv == null) throw new ArgumentNullException(nameof(gv));
            return gv.SelectedItems;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
