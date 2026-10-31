using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Client.Services;
using LaptopStore.Client.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace LaptopStore.Client.ViewModels;

public class CustomersViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private string _searchKeyword = string.Empty;
    private CustomerDto? _selectedCustomer;

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                _ = LoadCustomersAsync();
            }
        }
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                EditCustomerCommand.NotifyCanExecuteChanged();
                DeleteCustomerCommand.NotifyCanExecuteChanged();
                _ = LoadCustomerOrdersAsync();
            }
        }
    }

    public ObservableCollection<CustomerDto> Customers { get; } = new();

    /// <summary>Tổng chi tiêu của các khách hàng đang hiển thị.</summary>
    public decimal TotalSpentSum => Customers.Sum(c => c.TotalSpent);
    public ObservableCollection<OrderDto> SelectedCustomerOrders { get; } = new();

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand AddCustomerCommand { get; }
    public IAsyncRelayCommand EditCustomerCommand { get; }
    public IAsyncRelayCommand DeleteCustomerCommand { get; }

    public CustomersViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadCommand = new AsyncRelayCommand(LoadCustomersAsync);
        AddCustomerCommand = new AsyncRelayCommand(AddCustomerAsync);
        EditCustomerCommand = new AsyncRelayCommand(EditCustomerAsync, () => SelectedCustomer != null);
        DeleteCustomerCommand = new AsyncRelayCommand(DeleteCustomerAsync, () => SelectedCustomer != null);
    }

    public async Task LoadCustomersAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang tải danh sách khách hàng...";
        try
        {
            var list = await _apiClient.GetCustomersAsync(SearchKeyword);
            Customers.Clear();
            foreach (var c in list)
            {
                Customers.Add(c);
            }
            OnPropertyChanged(nameof(TotalSpentSum));
            StatusMessage = $"Đã tải {Customers.Count} khách hàng.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCustomerOrdersAsync()
    {
        SelectedCustomerOrders.Clear();
        if (SelectedCustomer == null) return;

        try
        {
            var orders = await _apiClient.GetCustomerOrdersAsync(SelectedCustomer.Id);
            foreach (var o in orders)
            {
                SelectedCustomerOrders.Add(o);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi tải lịch sử đơn hàng: {ex.Message}";
        }
    }

    private async Task AddCustomerAsync()
    {
        var dialog = new CustomerEditWindow();
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && dialog.ResultCreateDto != null)
        {
            IsBusy = true;
            StatusMessage = "Đang thêm khách hàng mới...";
            try
            {
                var result = await _apiClient.CreateCustomerAsync(dialog.ResultCreateDto);
                if (result.IsSuccess)
                {
                    StatusMessage = $"Đã thêm khách hàng: {dialog.ResultCreateDto.FullName}";
                    MessageBox.Show("Thêm mới thông tin khách hàng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCustomersAsync();
                }
                else
                {
                    StatusMessage = result.Error ?? "Thêm khách hàng thất bại. Số điện thoại có thể đã tồn tại.";
                    MessageBox.Show(result.Error ?? "Không thể thêm khách hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async Task EditCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        var dialog = new CustomerEditWindow(SelectedCustomer);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && dialog.ResultUpdateDto != null)
        {
            IsBusy = true;
            StatusMessage = $"Đang cập nhật khách hàng: {SelectedCustomer.FullName}...";
            try
            {
                var result = await _apiClient.UpdateCustomerAsync(dialog.ResultUpdateDto);
                if (result.IsSuccess)
                {
                    StatusMessage = $"Đã cập nhật khách hàng: {dialog.ResultUpdateDto.FullName}";
                    MessageBox.Show("Cập nhật thông tin khách hàng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCustomersAsync();
                }
                else
                {
                    StatusMessage = result.Error ?? "Cập nhật khách hàng thất bại.";
                    MessageBox.Show(result.Error ?? "Không thể cập nhật khách hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa khách hàng '{SelectedCustomer.FullName}' ({SelectedCustomer.CustomerCode})?",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var result = await _apiClient.DeleteCustomerAsync(SelectedCustomer.Id);
            if (result.IsSuccess)
            {
                Customers.Remove(SelectedCustomer);
                SelectedCustomer = null;
                SelectedCustomerOrders.Clear();
                StatusMessage = "Xóa khách hàng thành công.";
                MessageBox.Show("Đã xóa khách hàng thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = result.Error ?? "Xóa khách hàng thất bại. Khách hàng đã có đơn hàng giao dịch.";
                MessageBox.Show(result.Error ?? "Không thể xóa khách hàng đã có đơn hàng giao dịch trong hệ thống.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
