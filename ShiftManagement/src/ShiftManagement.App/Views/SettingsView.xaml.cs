using System.Windows.Controls;
using ShiftManagement.App.ViewModels;

namespace ShiftManagement.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                await vm.LoadSettingsAsync();
            }
        };
    }
}
