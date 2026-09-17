using System.Windows.Controls;
using ShiftManagement.App.ViewModels;

namespace ShiftManagement.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DashboardViewModel vm)
            {
                await vm.LoadDataAsync();
            }
        };
    }
}
