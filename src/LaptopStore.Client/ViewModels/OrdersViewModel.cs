using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Client.Services;
using System.Collections.ObjectModel;

namespace LaptopStore.Client.ViewModels;

public class OrdersViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private OrderDto? _selectedOrder;

    public OrderDto? SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    public ObservableCollection<OrderDto> Orders { get; } = new();

    public IAsyncRelayCommand LoadOrdersCommand { get; }

    public OrdersViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadOrdersCommand = new AsyncRelayCommand(LoadOrdersAsync);
    }

    public async Task LoadOrdersAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang tải danh sách đơn hàng...";
        try
        {
            var list = await _apiClient.GetOrdersAsync();
            Orders.Clear();
            foreach (var o in list)
            {
                Orders.Add(o);
            }
            StatusMessage = $"Đã tải {Orders.Count} đơn hàng.";
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
}
