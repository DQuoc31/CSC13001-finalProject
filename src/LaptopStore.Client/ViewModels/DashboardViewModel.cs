using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Reports;
using LaptopStore.Client.Services;
using System.Collections.ObjectModel;

namespace LaptopStore.Client.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;

    private decimal _totalRevenue;
    private decimal _monthRevenue;
    private int _totalOrders;
    private int _completedOrders;
    private int _totalProducts;
    private int _lowStockCount;

    public decimal TotalRevenue { get => _totalRevenue; set => SetProperty(ref _totalRevenue, value); }
    public decimal MonthRevenue { get => _monthRevenue; set => SetProperty(ref _monthRevenue, value); }
    public int TotalOrders { get => _totalOrders; set => SetProperty(ref _totalOrders, value); }
    public int CompletedOrders { get => _completedOrders; set => SetProperty(ref _completedOrders, value); }
    public int TotalProducts { get => _totalProducts; set => SetProperty(ref _totalProducts, value); }
    public int LowStockCount { get => _lowStockCount; set => SetProperty(ref _lowStockCount, value); }

    public ObservableCollection<TopSellingProductDto> TopSellingProducts { get; } = new();

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
            TopSellingProducts.Clear();
            foreach (var item in topList)
            {
                TopSellingProducts.Add(item);
            }
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
}
