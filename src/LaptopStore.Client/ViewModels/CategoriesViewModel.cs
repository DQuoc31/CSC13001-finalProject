using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Client.Services;
using LaptopStore.Client.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace LaptopStore.Client.ViewModels;

public class CategoriesViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private CategoryDto? _selectedCategory;

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                EditCategoryCommand.NotifyCanExecuteChanged();
                DeleteCategoryCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<CategoryDto> Categories { get; } = new();

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand AddCategoryCommand { get; }
    public IAsyncRelayCommand EditCategoryCommand { get; }
    public IAsyncRelayCommand DeleteCategoryCommand { get; }

    public CategoriesViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadCommand = new AsyncRelayCommand(LoadCategoriesAsync);
        AddCategoryCommand = new AsyncRelayCommand(AddCategoryAsync);
        EditCategoryCommand = new AsyncRelayCommand(EditCategoryAsync, () => SelectedCategory != null);
        DeleteCategoryCommand = new AsyncRelayCommand(DeleteCategoryAsync, () => SelectedCategory != null);
    }

    public async Task LoadCategoriesAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang tải danh sách danh mục...";
        try
        {
            var list = await _apiClient.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in list)
            {
                Categories.Add(c);
            }
            StatusMessage = $"Đã tải {Categories.Count} danh mục sản phẩm.";
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

    private async Task AddCategoryAsync()
    {
        var dialog = new CategoryEditWindow();
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && dialog.ResultCreateDto != null)
        {
            IsBusy = true;
            StatusMessage = "Đang thêm danh mục mới...";
            try
            {
                var ok = await _apiClient.CreateCategoryAsync(dialog.ResultCreateDto);
                if (ok)
                {
                    StatusMessage = $"Đã thêm danh mục: {dialog.ResultCreateDto.Name}";
                    MessageBox.Show("Thêm mới danh mục thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCategoriesAsync();
                }
                else
                {
                    StatusMessage = "Thêm danh mục thất bại.";
                    MessageBox.Show("Không thể tạo danh mục.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private async Task EditCategoryAsync()
    {
        if (SelectedCategory == null) return;

        var dialog = new CategoryEditWindow(SelectedCategory);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && dialog.ResultUpdateDto != null)
        {
            IsBusy = true;
            StatusMessage = $"Đang cập nhật danh mục: {SelectedCategory.Name}...";
            try
            {
                var ok = await _apiClient.UpdateCategoryAsync(dialog.ResultUpdateDto);
                if (ok)
                {
                    StatusMessage = $"Đã cập nhật danh mục: {dialog.ResultUpdateDto.Name}";
                    MessageBox.Show("Cập nhật danh mục thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCategoriesAsync();
                }
                else
                {
                    StatusMessage = "Cập nhật danh mục thất bại.";
                    MessageBox.Show("Không thể cập nhật danh mục.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null) return;

        if (SelectedCategory.ProductCount > 0)
        {
            MessageBox.Show($"Danh mục '{SelectedCategory.Name}' đang có {SelectedCategory.ProductCount} sản phẩm, không thể xóa trực tiếp!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa danh mục '{SelectedCategory.Name}'?",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var ok = await _apiClient.DeleteCategoryAsync(SelectedCategory.Id);
            if (ok)
            {
                Categories.Remove(SelectedCategory);
                SelectedCategory = null;
                StatusMessage = "Xóa danh mục thành công.";
                MessageBox.Show("Đã xóa danh mục thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "Xóa danh mục thất bại.";
                MessageBox.Show("Không thể xóa danh mục.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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
