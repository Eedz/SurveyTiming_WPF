using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SurveyTiming_WPF
{
    public class DataGridScrollToSelectedBehavior
    {
        public static readonly DependencyProperty ScrollOnSelectedProperty =
            DependencyProperty.RegisterAttached(
                "ScrollOnSelected",
                typeof(bool),
                typeof(DataGridScrollToSelectedBehavior),
                new PropertyMetadata(false, OnScrollOnSelectedChanged));

        public static bool GetScrollOnSelected(DependencyObject obj) =>
            (bool)obj.GetValue(ScrollOnSelectedProperty);

        public static void SetScrollOnSelected(DependencyObject obj, bool value) =>
            obj.SetValue(ScrollOnSelectedProperty, value);

        private static void OnScrollOnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid && e.NewValue is bool enabled)
            {
                if (enabled)
                {
                    dataGrid.SelectionChanged += DataGrid_SelectionChanged;
                }
                else
                {
                    dataGrid.SelectionChanged -= DataGrid_SelectionChanged;
                }
            }
        }

        private static void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem != null)
            {
                grid.ScrollIntoView(grid.SelectedItem);
            }
        }
    }
}
