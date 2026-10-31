using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Client.Converters;
using LaptopStore.Client.Services;
using LaptopStore.Client.Views;
using LaptopStore.Domain.Enums;
using System.Collections.ObjectModel;
using System.Windows;

namespace LaptopStore.Client.ViewModels;

/// <summary>Tab lọc theo trạng thái đơn hàng (Status = null nghĩa là tất cả).</summary>
public class OrderTab
{
    public OrderStatus? Status { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Một bước trên dòng thời gian xử lý đơn. State: done / current / todo / cancel.</summary>
public class OrderStep
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string State { get; set; } = "todo";
    public bool IsLast { get; set; }
}

public class OrdersViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private OrderDto? _selectedOrder;
    private List<OrderDto> _allOrders = new();
    private string _searchKeyword = string.Empty;
    private OrderTab? _selectedTab;

    /// <summary>Phát ra khi người dùng muốn lập đơn mới — trang chính sẽ mở POS.</summary>
    public event Action? CreateOrderRequested;

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value)) ApplyFilter();
        }
    }

    public OrderTab? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value)) ApplyFilter();
        }
    }

    public ObservableCollection<OrderTab> Tabs { get; } = new();
    public ObservableCollection<OrderStep> SelectedOrderSteps { get; } = new();

    /// <summary>Trạng thái kế tiếp hợp lệ của đơn đang chọn (dùng cho nút thao tác chính).</summary>
    public string? NextStatusKey => SelectedOrder?.Status switch
    {
        OrderStatus.Pending => nameof(OrderStatus.Processing),
        OrderStatus.Processing => nameof(OrderStatus.Shipping),
        OrderStatus.Shipping => nameof(OrderStatus.Completed),
        _ => null
    };

    public string NextStatusLabel => SelectedOrder?.Status switch
    {
        OrderStatus.Pending => "Xác nhận & xử lý",
        OrderStatus.Processing => "Chuyển sang Đang giao",
        OrderStatus.Shipping => "Hoàn thành đơn",
        _ => string.Empty
    };

    public string PaymentText => SelectedOrder == null ? string.Empty
        : $"{Display.PaymentMethodText(SelectedOrder.PaymentMethod)} · {Display.PaymentStatusText(SelectedOrder.PaymentStatus)}";

    public OrderDto? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetProperty(ref _selectedOrder, value))
            {
                UpdateStatusCommand.NotifyCanExecuteChanged();
                CancelOrderCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(NextStatusKey));
                OnPropertyChanged(nameof(NextStatusLabel));
                OnPropertyChanged(nameof(PaymentText));
                BuildSteps();
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
        CreateOrderCommand = new AsyncRelayCommand(() =>
        {
            CreateOrderRequested?.Invoke();
            return Task.CompletedTask;
        });
        UpdateStatusCommand = new AsyncRelayCommand<string>(UpdateStatusAsync,
            s => SelectedOrder != null && SelectedOrder.Status != OrderStatus.Cancelled && SelectedOrder.Status != OrderStatus.Completed);
        CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync,
            () => SelectedOrder != null && SelectedOrder.Status != OrderStatus.Cancelled && SelectedOrder.Status != OrderStatus.Completed);
    }

    public async Task LoadOrdersAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang tải danh sách đơn hàng...";
        try
        {
            var selectedId = SelectedOrder?.Id;
            _allOrders = await _apiClient.GetOrdersAsync();
            RebuildTabs();
            ApplyFilter();
            if (selectedId != null)
                SelectedOrder = Orders.FirstOrDefault(o => o.Id == selectedId);
            StatusMessage = $"Đã tải {_allOrders.Count} đơn hàng.";
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

    private void RebuildTabs()
    {
        var current = SelectedTab?.Status;
        var tabs = new List<OrderTab> { new() { Status = null, Label = "Tất cả", Count = _allOrders.Count } };
        foreach (var status in new[] { OrderStatus.Pending, OrderStatus.Processing, OrderStatus.Shipping, OrderStatus.Completed, OrderStatus.Cancelled })
        {
            tabs.Add(new OrderTab
            {
                Status = status,
                Label = Display.OrderStatusText(status),
                Count = _allOrders.Count(o => o.Status == status)
            });
        }

        Tabs.Clear();
        foreach (var t in tabs) Tabs.Add(t);
        _selectedTab = Tabs.FirstOrDefault(t => t.Status == current) ?? Tabs[0];
        OnPropertyChanged(nameof(SelectedTab));
    }

    private void ApplyFilter()
    {
        var keyword = SearchKeyword.Trim();
        var query = _allOrders.AsEnumerable();
        if (SelectedTab?.Status is OrderStatus status) query = query.Where(o => o.Status == status);
        if (keyword.Length > 0)
        {
            query = query.Where(o =>
                o.OrderNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerPhone.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        var selectedId = SelectedOrder?.Id;
        Orders.Clear();
        foreach (var o in query.OrderByDescending(o => o.CreatedAt)) Orders.Add(o);
        if (selectedId != null && Orders.All(o => o.Id != selectedId)) SelectedOrder = null;
    }

    /// <summary>Dựng dòng thời gian xử lý từ trạng thái hiện tại của đơn.</summary>
    private void BuildSteps()
    {
        SelectedOrderSteps.Clear();
        var order = SelectedOrder;
        if (order == null) return;

        var created = $"{order.CreatedAt:dd'/'MM · HH:mm}"
            + (string.IsNullOrEmpty(order.CreatedByUserName) ? "" : $" · {order.CreatedByUserName}");

        if (order.Status == OrderStatus.Cancelled)
        {
            SelectedOrderSteps.Add(new OrderStep { Title = "Tạo đơn", Subtitle = created, State = "done" });
            SelectedOrderSteps.Add(new OrderStep { Title = "Đã hủy", Subtitle = "Số lượng đã được hoàn lại kho", State = "cancel", IsLast = true });
            return;
        }

        int rank = (int)order.Status; // Pending = 1 … Completed = 4
        var steps = new (string Title, int Rank)[] { ("Tạo đơn", 1), ("Đang xử lý", 2), ("Đang giao", 3), ("Hoàn thành", 4) };
        foreach (var (title, stepRank) in steps)
        {
            string state = rank > stepRank || (rank == 4 && stepRank == 4) ? "done"
                : rank == stepRank ? "current" : "todo";
            string subtitle = stepRank switch
            {
                1 => created,
                4 when order.CompletedAt != null => $"{order.CompletedAt:dd'/'MM · HH:mm}",
                _ => state switch { "current" => "Đang thực hiện", "todo" => "Chưa bắt đầu", _ => "Đã xong" }
            };
            SelectedOrderSteps.Add(new OrderStep
            {
                Title = stepRank == 1 && state == "current" ? "Chờ xác nhận" : title,
                Subtitle = subtitle,
                State = state
            });
        }
        SelectedOrderSteps[^1].IsLast = true;
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

    private async Task UpdateStatusAsync(string? statusStr)
    {
        if (SelectedOrder == null || string.IsNullOrWhiteSpace(statusStr)) return;

        if (!Enum.TryParse<OrderStatus>(statusStr, out var newStatus)) return;

        IsBusy = true;
        StatusMessage = $"Đang cập nhật trạng thái đơn {SelectedOrder.OrderNumber}...";
        try
        {
            var result = await _apiClient.UpdateOrderStatusAsync(SelectedOrder.Id, new UpdateOrderStatusDto
            {
                Status = newStatus,
                PaymentStatus = newStatus == OrderStatus.Completed ? PaymentStatus.Paid : SelectedOrder.PaymentStatus
            });

            if (result.IsSuccess)
            {
                StatusMessage = $"Đã cập nhật trạng thái đơn hàng sang: {newStatus}";
                MessageBox.Show($"Cập nhật trạng thái thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadOrdersAsync();
            }
            else
            {
                StatusMessage = result.Error ?? "Cập nhật trạng thái thất bại.";
                MessageBox.Show(result.Error ?? "Không thể cập nhật trạng thái đơn hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var result = await _apiClient.CancelOrderAsync(SelectedOrder.Id, "Khách hàng yêu cầu hủy đơn");
            if (result.IsSuccess)
            {
                StatusMessage = $"Đã hủy đơn hàng {SelectedOrder.OrderNumber} và hoàn tồn kho thành công.";
                MessageBox.Show("Đã hủy đơn hàng và hoàn trả số lượng vào kho.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadOrdersAsync();
            }
            else
            {
                StatusMessage = result.Error ?? "Hủy đơn hàng thất bại.";
                MessageBox.Show(result.Error ?? "Không thể hủy đơn hàng này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
