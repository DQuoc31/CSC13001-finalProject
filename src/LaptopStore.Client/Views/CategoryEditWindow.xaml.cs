using LaptopStore.Application.DTOs.Categories;
using System.Windows;

namespace LaptopStore.Client.Views;

public partial class CategoryEditWindow : Window
{
    public CreateCategoryDto? ResultCreateDto { get; private set; }
    public UpdateCategoryDto? ResultUpdateDto { get; private set; }
    public bool IsEditMode => _existingCategory != null;

    private readonly CategoryDto? _existingCategory;

    public CategoryEditWindow(CategoryDto? existingCategory = null)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowCaption.Apply(this);
        _existingCategory = existingCategory;

        if (existingCategory != null)
        {
            TxtTitle.Text = $"Chỉnh Sửa Danh Mục: {existingCategory.Name}";
            Title = $"Chỉnh sửa - {existingCategory.Name}";

            TxtName.Text = existingCategory.Name;
            TxtOrder.Text = existingCategory.DisplayOrder.ToString();
            TxtDescription.Text = existingCategory.Description ?? string.Empty;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Vui lòng nhập tên danh mục.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtName.Focus();
            return;
        }

        int.TryParse(TxtOrder.Text.Trim(), out var order);
        var desc = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();

        if (IsEditMode)
        {
            ResultUpdateDto = new UpdateCategoryDto
            {
                Id = _existingCategory!.Id,
                Name = name,
                DisplayOrder = order,
                Description = desc
            };
        }
        else
        {
            ResultCreateDto = new CreateCategoryDto
            {
                Name = name,
                DisplayOrder = order,
                Description = desc
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
