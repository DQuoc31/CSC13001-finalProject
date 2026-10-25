using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Domain.Enums;
using System.Windows;

namespace LaptopStore.Client.Views;

public partial class ProductEditWindow : Window
{
    public CreateProductDto? ResultCreateDto { get; private set; }
    public UpdateProductDto? ResultUpdateDto { get; private set; }
    public bool IsEditMode => _existingProduct != null;

    private readonly ProductDto? _existingProduct;

    public ProductEditWindow(List<CategoryDto> categories, ProductDto? existingProduct = null)
    {
        InitializeComponent();
        _existingProduct = existingProduct;

        // Điền danh sách danh mục (bỏ danh mục id=0 "Tất cả")
        CboCategory.ItemsSource = categories.Where(c => c.Id > 0).ToList();
        if (CboCategory.Items.Count > 0)
        {
            CboCategory.SelectedIndex = 0;
        }

        if (existingProduct != null)
        {
            TxtTitle.Text = $"✏️ Chỉnh Sửa Sản Phẩm: {existingProduct.Name}";
            Title = $"Chỉnh sửa - {existingProduct.Sku}";

            TxtName.Text = existingProduct.Name;
            TxtSku.Text = existingProduct.Sku;
            TxtBrand.Text = existingProduct.Brand;
            TxtModel.Text = existingProduct.Model;
            TxtPrice.Text = existingProduct.Price.ToString("F0");
            TxtCostPrice.Text = existingProduct.CostPrice.ToString("F0");
            TxtStock.Text = existingProduct.StockQuantity.ToString();
            TxtMinStock.Text = existingProduct.MinStockLevel.ToString();
            TxtWarranty.Text = existingProduct.WarrantyMonths.ToString();

            TxtCpu.Text = existingProduct.Cpu ?? string.Empty;
            TxtRam.Text = existingProduct.Ram ?? string.Empty;
            TxtStorage.Text = existingProduct.Storage ?? string.Empty;
            TxtGpu.Text = existingProduct.Gpu ?? string.Empty;
            TxtScreen.Text = existingProduct.ScreenSize ?? string.Empty;
            TxtOs.Text = existingProduct.OperatingSystem ?? string.Empty;
            TxtDescription.Text = existingProduct.Description ?? string.Empty;

            CboCategory.SelectedValue = existingProduct.CategoryId;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtName.Text.Trim();
        var sku = TxtSku.Text.Trim();
        var brand = TxtBrand.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Vui lòng nhập tên sản phẩm.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            MessageBox.Show("Vui lòng nhập mã SKU cho sản phẩm.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSku.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            MessageBox.Show("Vui lòng nhập hãng sản xuất.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtBrand.Focus();
            return;
        }

        if (CboCategory.SelectedValue == null)
        {
            MessageBox.Show("Vui lòng chọn danh mục cho sản phẩm.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtPrice.Text.Trim(), out var price) || price <= 0)
        {
            MessageBox.Show("Giá bán niêm yết phải là số lớn hơn 0.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtPrice.Focus();
            return;
        }

        decimal.TryParse(TxtCostPrice.Text.Trim(), out var costPrice);
        if (costPrice < 0) costPrice = 0;

        if (!int.TryParse(TxtStock.Text.Trim(), out var stock) || stock < 0)
        {
            MessageBox.Show("Số lượng tồn kho phải là số nguyên không âm.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtStock.Focus();
            return;
        }

        int.TryParse(TxtMinStock.Text.Trim(), out var minStock);
        if (minStock < 0) minStock = 3;

        int.TryParse(TxtWarranty.Text.Trim(), out var warranty);
        if (warranty < 0) warranty = 12;

        int categoryId = (int)CboCategory.SelectedValue;

        if (IsEditMode)
        {
            ResultUpdateDto = new UpdateProductDto
            {
                Id = _existingProduct!.Id,
                Name = name,
                Sku = sku,
                Brand = brand,
                Model = TxtModel.Text.Trim(),
                CategoryId = categoryId,
                Price = price,
                CostPrice = costPrice,
                StockQuantity = stock,
                MinStockLevel = minStock,
                WarrantyMonths = warranty,
                Cpu = string.IsNullOrWhiteSpace(TxtCpu.Text) ? null : TxtCpu.Text.Trim(),
                Ram = string.IsNullOrWhiteSpace(TxtRam.Text) ? null : TxtRam.Text.Trim(),
                Storage = string.IsNullOrWhiteSpace(TxtStorage.Text) ? null : TxtStorage.Text.Trim(),
                Gpu = string.IsNullOrWhiteSpace(TxtGpu.Text) ? null : TxtGpu.Text.Trim(),
                ScreenSize = string.IsNullOrWhiteSpace(TxtScreen.Text) ? null : TxtScreen.Text.Trim(),
                OperatingSystem = string.IsNullOrWhiteSpace(TxtOs.Text) ? null : TxtOs.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim(),
                Status = stock > 0 ? ProductStatus.InStock : ProductStatus.OutOfStock
            };
        }
        else
        {
            ResultCreateDto = new CreateProductDto
            {
                Name = name,
                Sku = sku,
                Brand = brand,
                Model = TxtModel.Text.Trim(),
                CategoryId = categoryId,
                Price = price,
                CostPrice = costPrice,
                StockQuantity = stock,
                MinStockLevel = minStock,
                WarrantyMonths = warranty,
                Cpu = string.IsNullOrWhiteSpace(TxtCpu.Text) ? null : TxtCpu.Text.Trim(),
                Ram = string.IsNullOrWhiteSpace(TxtRam.Text) ? null : TxtRam.Text.Trim(),
                Storage = string.IsNullOrWhiteSpace(TxtStorage.Text) ? null : TxtStorage.Text.Trim(),
                Gpu = string.IsNullOrWhiteSpace(TxtGpu.Text) ? null : TxtGpu.Text.Trim(),
                ScreenSize = string.IsNullOrWhiteSpace(TxtScreen.Text) ? null : TxtScreen.Text.Trim(),
                OperatingSystem = string.IsNullOrWhiteSpace(TxtOs.Text) ? null : TxtOs.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim()
            };
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
