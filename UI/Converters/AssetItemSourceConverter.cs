﻿namespace Macabre2D.UI.Converters {

    using Macabre2D.UI.Models;
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public sealed class AssetItemSourceConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is FolderAsset folderAsset) {
                return folderAsset.Children;
            }
            else {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}