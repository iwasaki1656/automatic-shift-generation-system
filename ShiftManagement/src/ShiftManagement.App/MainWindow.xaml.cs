using System.Windows;
using ShiftManagement.App.ViewModels;

namespace ShiftManagement.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
