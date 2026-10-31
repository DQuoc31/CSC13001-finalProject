using LaptopStore.Client.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace LaptopStore.Client.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        // PasswordBox không hỗ trợ binding: đồng bộ thủ công với MainViewModel.LoginPassword
        Loaded += (_, _) =>
        {
            if (DataContext is MainViewModel vm && TxtPassword.Password != vm.LoginPassword)
                TxtPassword.Password = vm.LoginPassword;
        };
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue) Dispatcher.BeginInvoke(() => TxtUsername.Focus());
        };
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.LoginPassword = TxtPassword.Password;
    }
}
