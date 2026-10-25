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
            Categories.Add(new CategoryDto { Id = 0, Name = "Tất cả danh mục" });
            foreach (var c in cats) Categories.Add(c);

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
                var ok = await _apiClient.CreateProductAsync(dialog.ResultCreateDto);
                if (ok)
                {
                    StatusMessage = $"Đã thêm thành công sản phẩm: {dialog.ResultCreateDto.Name}";
                    MessageBox.Show("Thêm mới sản phẩm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    StatusMessage = "Không thể thêm sản phẩm. Kiểm tra lại dữ liệu hoặc SKU trùng lặp.";
                    MessageBox.Show("Thêm sản phẩm thất bại. Có thể mã SKU đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                var ok = await _apiClient.UpdateProductAsync(dialog.ResultUpdateDto);
                if (ok)
                {
                    StatusMessage = $"Đã cập nhật sản phẩm: {dialog.ResultUpdateDto.Name}";
                    MessageBox.Show("Cập nhật thông tin sản phẩm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    StatusMessage = "Cập nhật sản phẩm thất bại.";
                    MessageBox.Show("Không thể cập nhật sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var ok = await _apiClient.DeleteProductAsync(SelectedProduct.Id);
            if (ok)
            {
                Products.Remove(SelectedProduct);
                SelectedProduct = null;
                StatusMessage = "Xóa sản phẩm thành công.";
                MessageBox.Show("Đã xóa sản phẩm thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "Không thể xóa sản phẩm.";
                MessageBox.Show("Không thể xóa sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
