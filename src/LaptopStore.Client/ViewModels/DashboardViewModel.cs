using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Client.Controls;
using LaptopStore.Client.Converters;
using LaptopStore.Client.Services;
using LaptopStore.Domain.Enums;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace LaptopStore.Client.ViewModels;

/// <summary>Một dòng trong bảng xếp hạng Top bán chạy.</summary>
public class TopSellingItem
{
    public int Rank { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    /// <summary>Tỉ lệ so với sản phẩm dẫn đầu, dùng để vẽ thanh ngang.</summary>
    public GridLength BarWidth { get; set; }
    public GridLength RestWidth { get; set; }
}

public class DashboardViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;

    private decimal _totalRevenue;
    private decimal _monthRevenue;
    private int _totalOrders;
    private int _completedOrders;
    private int _pendingOrders;
    private int _totalProducts;
    private int _lowStockCount;
    private decimal? _monthGrowth;

    public decimal TotalRevenue { get => _totalRevenue; set => SetProperty(ref _totalRevenue, value); }
    public decimal MonthRevenue { get => _monthRevenue; set => SetProperty(ref _monthRevenue, value); }
    public int TotalOrders { get => _totalOrders; set => SetProperty(ref _totalOrders, value); }
    public int CompletedOrders { get => _completedOrders; set => SetProperty(ref _completedOrders, value); }
    public int PendingOrders { get => _pendingOrders; set => SetProperty(ref _pendingOrders, value); }
    public int TotalProducts { get => _totalProducts; set => SetProperty(ref _totalProducts, value); }
    public int LowStockCount { get => _lowStockCount; set => SetProperty(ref _lowStockCount, value); }

    /// <summary>% tăng/giảm doanh thu tháng này so với tháng trước (null nếu tháng trước = 0).</summary>
    public decimal? MonthGrowth
    {
        get => _monthGrowth;
        set
        {
            if (SetProperty(ref _monthGrowth, value))
            {
                OnPropertyChanged(nameof(MonthGrowthText));
                OnPropertyChanged(nameof(IsGrowthPositive));
            }
        }
    }

    public string MonthGrowthText => MonthGrowth is decimal g ? $"{(g >= 0 ? "+" : "")}{g:0.0}%" : "—";
    public bool IsGrowthPositive => MonthGrowth is null or >= 0;
    public string MonthLabel => $"Doanh thu tháng {DateTime.Now.Month}";

    public ObservableCollection<TopSellingItem> TopSellingProducts { get; } = new();
    public ObservableCollection<ProductDto> LowStockProducts { get; } = new();
    public ObservableCollection<ChartBar> RevenueBars { get; } = new();
    public ObservableCollection<ChartSlice> StatusSlices { get; } = new();

    public IAsyncRelayCommand RefreshCommand { get; }

    public DashboardViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
    }

    public async Task LoadDataAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang tải dữ liệu tổng quan...";
        try
        {
            var summary = await _apiClient.GetDashboardSummaryAsync();
            if (summary != null)
            {
                TotalRevenue = summary.TotalRevenue;
                MonthRevenue = summary.MonthRevenue;
                TotalOrders = summary.TotalOrders;
                CompletedOrders = summary.CompletedOrders;
                TotalProducts = summary.TotalProducts;
                LowStockCount = summary.LowStockProducts;
            }

            var topList = await _apiClient.GetTopSellingProductsAsync();
            var maxRevenue = topList.Count > 0 ? topList.Max(t => t.TotalRevenue) : 0;
            TopSellingProducts.Clear();
            int rank = 1;
            foreach (var item in topList.Take(5))
            {
                double ratio = maxRevenue > 0 ? (double)(item.TotalRevenue / maxRevenue) : 0;
                TopSellingProducts.Add(new TopSellingItem
                {
                    Rank = rank++,
                    ProductName = item.ProductName,
                    Brand = item.Brand,
                    TotalQuantitySold = item.TotalQuantitySold,
                    TotalRevenue = item.TotalRevenue,
                    BarWidth = new GridLength(Math.Max(ratio, 0.02), GridUnitType.Star),
                    RestWidth = new GridLength(Math.Max(1 - ratio, 0), GridUnitType.Star)
                });
            }

            var lowStock = await _apiClient.GetLowStockProductsAsync();
            LowStockProducts.Clear();
            foreach (var p in lowStock.OrderBy(p => p.StockQuantity).Take(5)) LowStockProducts.Add(p);
            if (summary == null) LowStockCount = lowStock.Count;

            await LoadOrderChartsAsync();
            StatusMessage = "Cập nhật dữ liệu thành công.";
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

    /// <summary>Tính biểu đồ doanh thu 12 tháng và tỉ lệ trạng thái từ danh sách đơn hàng.</summary>
    private async Task LoadOrderChartsAsync()
    {
        var orders = await _apiClient.GetOrdersAsync();

        // Doanh thu theo tháng (chỉ đơn hoàn thành), 12 tháng gần nhất tính cả tháng hiện tại
        var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var revenueByMonth = orders
            .Where(o => o.Status == OrderStatus.Completed)
            .GroupBy(o => { var d = o.CompletedAt ?? o.CreatedAt; return new DateTime(d.Year, d.Month, 1); })
            .ToDictionary(g => g.Key, g => g.Sum(o => o.FinalAmount));

        RevenueBars.Clear();
        for (int i = 11; i >= 0; i--)
        {
            var month = thisMonth.AddMonths(-i);
            revenueByMonth.TryGetValue(month, out var revenue);
            RevenueBars.Add(new ChartBar
            {
                Label = $"T{month.Month}",
                Value = (double)(revenue / 1_000_000m),
                ValueText = Display.ShortMoney(revenue),
                IsHighlight = i == 0
            });
        }

        revenueByMonth.TryGetValue(thisMonth.AddMonths(-1), out var lastMonthRevenue);
        revenueByMonth.TryGetValue(thisMonth, out var currentMonthRevenue);
        MonthGrowth = lastMonthRevenue > 0 ? (currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue * 100 : null;

        // Tỉ lệ trạng thái đơn hàng
        var palette = new Dictionary<OrderStatus, string>
        {
            [OrderStatus.Completed] = "#10B981",
            [OrderStatus.Processing] = "#8B5CF6",
            [OrderStatus.Shipping] = "#0EA5E9",
            [OrderStatus.Pending] = "#F59E0B",
            [OrderStatus.Cancelled] = "#EF4444"
        };
        int total = orders.Count;
        StatusSlices.Clear();
        foreach (var (status, color) in palette)
        {
            int count = orders.Count(o => o.Status == status);
            StatusSlices.Add(new ChartSlice
            {
                Label = Display.OrderStatusText(status),
                Value = count,
                Brush = (Brush)Display.Hex(color),
                Percent = total > 0 ? $"{count * 100.0 / total:0}%" : "0%"
            });
        }

        TotalOrders = total;
        CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
        PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);
    }
}
