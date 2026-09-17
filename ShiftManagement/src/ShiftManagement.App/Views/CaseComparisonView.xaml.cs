using System.Windows.Controls;
using ShiftManagement.App.ViewModels;

namespace ShiftManagement.App.Views;

public partial class CaseComparisonView : UserControl
{
    public CaseComparisonView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CaseComparisonViewModel vm)
            {
                await vm.LoadCasesAsync();
            }
        };
    }
}
