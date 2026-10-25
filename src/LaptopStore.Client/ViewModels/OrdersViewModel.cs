using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Client.Services;
using LaptopStore.Client.Views;
using LaptopStore.Domain.Enums;
using System.Collections.ObjectModel;
using System.Windows;

namespace LaptopStore.Client.ViewModels;

public class OrdersViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private OrderDto? _selectedOrder;

    public OrderDto? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetProperty(ref _selectedOrder, value))
            {
                UpdateStatusCommand.NotifyCanExecuteChanged();
                CancelOrderCommand.NotifyCanExecuteChanged();
                _ = LoadOrderDetailsAsync();
            }
        }
    }

    public ObservableCollection<OrderDto> Orders { get; } = new();
    public ObservableCollection<OrderItemDto> SelectedOrderItems { get; } = new();

    public IAsyncRelayCommand LoadOrdersCommand { get; }
    public IAsyncRelayCommand CreateOrderCommand { get; }
    public IAsyncRelayCommand<string> UpdateStatusCommand { get; }
    public IAsyncRelayCommand CancelOrderCommand { get; }

    public OrdersViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadOrdersCommand = new AsyncRelayCommand(LoadOrdersAsync);
        CreateOrderCommand = new AsyncRelayCommand(CreateOrderAsync);
        UpdateStatusCommand = new AsyncRelayCommand<string>(UpdateStatusAsync, s => SelectedOrder != null);
        CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync, () => SelectedOrder != null && SelectedOrder.Status != OrderStatus.Cancelled);
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

    private async Task LoadOrderDetailsAsync()
    {
        SelectedOrderItems.Clear();
        if (SelectedOrder == null) return;

        try
        {
            var detail = await _apiClient.GetOrderByIdAsync(SelectedOrder.Id);
            if (detail?.Items != null)
            {
                foreach (var item in detail.Items)
                {
                    SelectedOrderItems.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi tải chi tiết đơn hàng: {ex.Message}";
        }
    }

    private async Task CreateOrderAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang chuẩn bị dữ liệu sản phẩm & khách hàng...";
        try
        {
            var customers = await _apiClient.GetCustomersAsync();
            var products = await _apiClient.GetProductsAsync();

            var dialog = new CreateOrderWindow(customers, products);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true && dialog.ResultOrderDto != null)
            {
                StatusMessage = "Đang lập đơn hàng và kiểm tra tồn kho...";
                var createdOrder = await _apiClient.CreateOrderAsync(dialog.ResultOrderDto);
                if (createdOrder != null)
                {
                    StatusMessage = $"Tạo đơn hàng {createdOrder.OrderNumber} thành công!";
                    MessageBox.Show(
                        $"Đơn hàng {createdOrder.OrderNumber} đã được lập thành công!\nTổng tiền: {createdOrder.FinalAmount:N0} VNĐ\nKho hàng đã được cập nhật tương ứng.",
                        "Lập đơn hàng thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    await LoadOrdersAsync();
                }
                else
                {
                    StatusMessage = "Lập đơn hàng thất bại. Kiểm tra số lượng tồn kho hoặc dữ liệu.";
                    MessageBox.Show("Không thể tạo đơn hàng. Vui lòng kiểm tra lại số lượng tồn kho.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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

    private async Task UpdateStatusAsync(string? statusStr)
    {
        if (SelectedOrder == null || string.IsNullOrWhiteSpace(statusStr)) return;

        if (!Enum.TryParse<OrderStatus>(statusStr, out var newStatus)) return;

        IsBusy = true;
        StatusMessage = $"Đang cập nhật trạng thái đơn {SelectedOrder.OrderNumber}...";
        try
        {
            var ok = await _apiClient.UpdateOrderStatusAsync(SelectedOrder.Id, new UpdateOrderStatusDto
            {
                Status = newStatus,
                PaymentStatus = newStatus == OrderStatus.Completed ? PaymentStatus.Paid : SelectedOrder.PaymentStatus
            });

            if (ok)
            {
                StatusMessage = $"Đã cập nhật trạng thái đơn hàng sang: {newStatus}";
                MessageBox.Show($"Cập nhật trạng thái thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadOrdersAsync();
            }
            else
            {
                StatusMessage = "Cập nhật trạng thái thất bại.";
                MessageBox.Show("Không thể cập nhật trạng thái đơn hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private async Task CancelOrderAsync()
    {
        if (SelectedOrder == null) return;

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn hủy đơn hàng '{SelectedOrder.OrderNumber}'?\nSố lượng sản phẩm trong đơn sẽ tự động được hoàn trả vào kho hàng.",
            "Xác nhận hủy đơn hàng",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        StatusMessage = $"Đang hủy đơn hàng {SelectedOrder.OrderNumber}...";
        try
        {
            var ok = await _apiClient.CancelOrderAsync(SelectedOrder.Id, "Khách hàng yêu cầu hủy đơn");
            if (ok)
            {
                StatusMessage = $"Đã hủy đơn hàng {SelectedOrder.OrderNumber} và hoàn tồn kho thành công.";
                MessageBox.Show("Đã hủy đơn hàng và hoàn trả số lượng vào kho.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadOrdersAsync();
            }
            else
            {
                StatusMessage = "Hủy đơn hàng thất bại.";
                MessageBox.Show("Không thể hủy đơn hàng này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
