using System.Windows.Controls;
using ShiftManagement.App.ViewModels;

namespace ShiftManagement.App.Views;

public partial class WishInputView : UserControl
{
    public WishInputView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is WishInputViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
    }
}
