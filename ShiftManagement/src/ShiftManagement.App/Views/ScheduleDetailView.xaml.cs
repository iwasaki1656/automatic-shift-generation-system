using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShiftManagement.App.ViewModels;
using ShiftManagement.Core.Models;

namespace ShiftManagement.App.Views;

public partial class ScheduleDetailView : UserControl
{
    public ScheduleDetailView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ScheduleDetailViewModel vm)
            {
                await vm.LoadScheduleAsync();
            }
        };
    }

    private void OnCellClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ScheduleGridCell cell)
        {
            if (DataContext is ScheduleDetailViewModel vm)
            {
                vm.SelectedCell = cell;
                vm.StatusMessage = $"選択中: {cell.Date:M/d} (現在の勤務: {cell.ShiftType?.DisplayName})";
            }
        }
    }

    private void OnShiftTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ShiftType newShift)
        {
            if (DataContext is ScheduleDetailViewModel vm && vm.SelectedCell != null)
            {
                vm.ChangeCellShift(newShift);
            }
        }
    }
}
