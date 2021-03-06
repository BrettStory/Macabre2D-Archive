﻿namespace Macabre2D.UI.Converters {

    using Macabre2D.UI.Models;
    using Macabre2D.UI.Models.FrameworkWrappers;
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Windows.Data;

    public sealed class ComponentParentToCollectionConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var collection = new ObservableCollection<IParent<ComponentWrapper>>();
            if (value is IParent<ComponentWrapper> parent) {
                collection.Add(parent);
            }

            return collection;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}