using System.Windows.Controls;
using ShiftManagement.App.ViewModels;

namespace ShiftManagement.App.Views;

public partial class StaffManagementView : UserControl
{
    public StaffManagementView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StaffManagementViewModel vm)
            {
                await vm.LoadStaffAsync();
            }
        };
    }
}
