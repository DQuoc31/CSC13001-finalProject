using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Client.Services;
using System.Collections.ObjectModel;

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
        set => SetProperty(ref _selectedProduct, value);
    }

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<CategoryDto> Categories { get; } = new();

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand DeleteProductCommand { get; }

    public ProductsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadCommand = new AsyncRelayCommand(LoadAllAsync);
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

    private async Task DeleteSelectedProductAsync()
    {
        if (SelectedProduct == null) return;
        var confirm = System.Windows.MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa sản phẩm '{SelectedProduct.Name}'?",
            "Xác nhận xóa",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var ok = await _apiClient.DeleteProductAsync(SelectedProduct.Id);
            if (ok)
            {
                Products.Remove(SelectedProduct);
                SelectedProduct = null;
                StatusMessage = "Xóa sản phẩm thành công.";
            }
            else
            {
                StatusMessage = "Không thể xóa sản phẩm.";
            }
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
