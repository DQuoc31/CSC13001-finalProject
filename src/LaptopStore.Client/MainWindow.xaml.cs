using LaptopStore.Client.ViewModels;
using System.Windows;

namespace LaptopStore.Client;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
