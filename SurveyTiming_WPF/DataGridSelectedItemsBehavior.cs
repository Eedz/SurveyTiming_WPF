using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SurveyTiming_WPF
{
    public static class DataGridSelectedItemsBehavior
    {
        public static readonly DependencyProperty BindableSelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "BindableSelectedItems",
                typeof(object),
                typeof(DataGridSelectedItemsBehavior),
                new PropertyMetadata(null, OnBindableSelectedItemsChanged));

        public static void SetBindableSelectedItems(DependencyObject element, object value)
        {
            element.SetValue(BindableSelectedItemsProperty, value);
        }

        public static object GetBindableSelectedItems(DependencyObject element)
        {
            return element.GetValue(BindableSelectedItemsProperty);
        }

        private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                dataGrid.SelectionChanged -= DataGrid_SelectionChanged;

                if (e.NewValue != null)
                {
                    dataGrid.SelectionChanged += DataGrid_SelectionChanged;
                }
            }
        }

        private static void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var targetCollection = GetBindableSelectedItems(dataGrid);

                // We expect an ObservableCollection<T>
                if (targetCollection is IList list)
                {
                    list.Clear();
                    foreach (var item in dataGrid.SelectedItems)
                    {
                        if (item != null && list.GetType().GenericTypeArguments[0].IsAssignableFrom(item.GetType()))
                        {
                            list.Add(item);
                        }
                    }
                }
            }
        }
    }
}

