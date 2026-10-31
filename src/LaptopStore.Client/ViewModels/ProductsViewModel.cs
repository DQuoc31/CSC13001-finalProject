using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Client.Services;
using LaptopStore.Client.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace LaptopStore.Client.ViewModels;

public class ProductsViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;

    private string _searchKeyword = string.Empty;
    private CategoryDto? _selectedCategory;
    private ProductDto? _selectedProduct;
    private bool _showLowStockOnly;
    private decimal _inventoryValue;
    private int _lowStockTotal;

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                _ = LoadProductsAsync();
            }
        }
    }

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                _ = LoadProductsAsync();
            }
        }
    }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                DeleteProductCommand.NotifyCanExecuteChanged();
                EditProductCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Chỉ hiện sản phẩm có tồn kho chạm ngưỡng cảnh báo.</summary>
    public bool ShowLowStockOnly
    {
        get => _showLowStockOnly;
        set
        {
            if (SetProperty(ref _showLowStockOnly, value))
            {
                _ = LoadProductsAsync();
            }
        }
    }

    /// <summary>Tổng giá trị tồn kho (giá vốn × số lượng) của danh sách đang xem.</summary>
    public decimal InventoryValue { get => _inventoryValue; private set => SetProperty(ref _inventoryValue, value); }

    /// <summary>Số sản phẩm sắp hết hàng trong danh sách đang xem (trước khi lọc).</summary>
    public int LowStockTotal { get => _lowStockTotal; private set => SetProperty(ref _lowStockTotal, value); }

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<CategoryDto> Categories { get; } = new();

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand AddProductCommand { get; }
    public IAsyncRelayCommand EditProductCommand { get; }
    public IAsyncRelayCommand DeleteProductCommand { get; }

    public ProductsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadCommand = new AsyncRelayCommand(LoadAllAsync);
        AddProductCommand = new AsyncRelayCommand(AddProductAsync);
        EditProductCommand = new AsyncRelayCommand(EditProductAsync, () => SelectedProduct != null);
        DeleteProductCommand = new AsyncRelayCommand(DeleteSelectedProductAsync, () => SelectedProduct != null);
    }

    public async Task LoadAllAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang tải danh sách sản phẩm & danh mục...";
        try
        {
            var cats = await _apiClient.GetCategoriesAsync();
            Categories.Clear();
            var all = new CategoryDto { Id = 0, Name = "Tất cả", ProductCount = cats.Sum(c => c.ProductCount) };
            Categories.Add(all);
            foreach (var c in cats) Categories.Add(c);

            // Chọn sẵn chip "Tất cả" (gán trực tiếp để không tải lại danh sách hai lần)
            _selectedCategory = all;
            OnPropertyChanged(nameof(SelectedCategory));

            await LoadProductsAsync();
            StatusMessage = $"Đã tải {Products.Count} sản phẩm.";
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

    public async Task LoadProductsAsync()
    {
        try
        {
            int? catId = (SelectedCategory != null && SelectedCategory.Id > 0) ? SelectedCategory.Id : null;
            var list = await _apiClient.GetProductsAsync(SearchKeyword, catId);

            LowStockTotal = list.Count(p => p.StockQuantity <= p.MinStockLevel);
            InventoryValue = list.Sum(p => p.CostPrice * p.StockQuantity);
            if (ShowLowStockOnly)
                list = list.Where(p => p.StockQuantity <= p.MinStockLevel).OrderBy(p => p.StockQuantity).ToList();

            Products.Clear();
            foreach (var p in list)
            {
                Products.Add(p);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi tải sản phẩm: {ex.Message}";
        }
    }

    private async Task AddProductAsync()
    {
        var dialog = new ProductEditWindow(Categories.ToList());
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && dialog.ResultCreateDto != null)
        {
            IsBusy = true;
            StatusMessage = "Đang lưu sản phẩm mới...";
            try
            {
                var result = await _apiClient.CreateProductAsync(dialog.ResultCreateDto);
                if (result.IsSuccess)
                {
                    StatusMessage = $"Đã thêm thành công sản phẩm: {dialog.ResultCreateDto.Name}";
                    MessageBox.Show("Thêm mới sản phẩm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    StatusMessage = result.Error ?? "Không thể thêm sản phẩm. Kiểm tra lại dữ liệu hoặc SKU trùng lặp.";
                    MessageBox.Show(result.Error ?? "Thêm sản phẩm thất bại. Có thể mã SKU đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private async Task EditProductAsync()
    {
        if (SelectedProduct == null) return;

        var dialog = new ProductEditWindow(Categories.ToList(), SelectedProduct);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && dialog.ResultUpdateDto != null)
        {
            IsBusy = true;
            StatusMessage = $"Đang cập nhật sản phẩm {SelectedProduct.Name}...";
            try
            {
                var result = await _apiClient.UpdateProductAsync(dialog.ResultUpdateDto);
                if (result.IsSuccess)
                {
                    StatusMessage = $"Đã cập nhật sản phẩm: {dialog.ResultUpdateDto.Name}";
                    MessageBox.Show("Cập nhật thông tin sản phẩm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    StatusMessage = result.Error ?? "Cập nhật sản phẩm thất bại.";
                    MessageBox.Show(result.Error ?? "Không thể cập nhật sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private async Task DeleteSelectedProductAsync()
    {
        if (SelectedProduct == null) return;
        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa sản phẩm '{SelectedProduct.Name}'?",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var result = await _apiClient.DeleteProductAsync(SelectedProduct.Id);
            if (result.IsSuccess)
            {
                Products.Remove(SelectedProduct);
                SelectedProduct = null;
                StatusMessage = "Xóa sản phẩm thành công.";
                MessageBox.Show("Đã xóa sản phẩm thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = result.Error ?? "Không thể xóa sản phẩm.";
                MessageBox.Show(result.Error ?? "Không thể xóa sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
