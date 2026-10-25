using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Domain.Enums;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace LaptopStore.Client.Views;

public class CartItemModel
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal SubTotal => UnitPrice * Quantity;
}

public partial class CreateOrderWindow : Window
{
    private readonly List<ProductDto> _products;
    private readonly List<CustomerDto> _customers;

    public ObservableCollection<CartItemModel> CartItems { get; } = new();
    public CreateOrderDto? ResultOrderDto { get; private set; }

    public CreateOrderWindow(List<CustomerDto> customers, List<ProductDto> products)
    {
        InitializeComponent();
        _customers = customers;
        _products = products;

        CboCustomer.ItemsSource = _customers;
        if (_customers.Count > 0) CboCustomer.SelectedIndex = 0;

        CboProduct.ItemsSource = _products;
        if (_products.Count > 0) CboProduct.SelectedIndex = 0;

        CboPaymentMethod.ItemsSource = Enum.GetValues(typeof(PaymentMethod));
        CboPaymentMethod.SelectedItem = PaymentMethod.Cash;

        DgCartItems.ItemsSource = CartItems;
        RecalculateTotals();
    }

    private void CboProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CboProduct.SelectedItem is ProductDto product)
        {
            TxtQuantity.ToolTip = $"Tồn kho khả dụng: {product.StockQuantity} chiếc";
        }
    }

    private void AddProductToCart_Click(object sender, RoutedEventArgs e)
    {
        if (CboProduct.SelectedItem is not ProductDto product)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần thêm vào đơn hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtQuantity.Text.Trim(), out var qty) || qty <= 0)
        {
            MessageBox.Show("Số lượng mua phải là số nguyên dương lớn hơn 0.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtQuantity.Focus();
            return;
        }

        // Kiểm tra tồn kho (Test case: Order-02: Mua số lượng > tồn kho -> Từ chối đơn hàng)
        var existingInCart = CartItems.FirstOrDefault(c => c.ProductId == product.Id)?.Quantity ?? 0;
        if (existingInCart + qty > product.StockQuantity)
        {
            MessageBox.Show(
                $"Số lượng yêu cầu ({existingInCart + qty}) vượt quá số lượng tồn kho khả dụng ({product.StockQuantity}) của sản phẩm '{product.Name}'.\nĐơn hàng bị từ chối theo quy định kiểm thử tồn kho!",
                "Cảnh báo tồn kho",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var existingItem = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
        if (existingItem != null)
        {
            existingItem.Quantity += qty;
        }
        else
        {
            CartItems.Add(new CartItemModel
            {
                ProductId = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                UnitPrice = product.Price,
                Quantity = qty
            });
        }

        // Refresh DataGrid
        DgCartItems.Items.Refresh();
        RecalculateTotals();
        TxtQuantity.Text = "1";
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is CartItemModel item)
        {
            CartItems.Remove(item);
            RecalculateTotals();
        }
    }

    private void TxtDiscount_TextChanged(object sender, TextChangedEventArgs e)
    {
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        decimal total = CartItems.Sum(i => i.SubTotal);
        decimal.TryParse(TxtDiscount?.Text.Trim(), out var discount);
        if (discount < 0) discount = 0;
        if (discount > total) discount = total;

        decimal finalAmount = total - discount;

        if (TxtTotalAmount != null) TxtTotalAmount.Text = $"{total:N0} đ";
        if (TxtDiscountDisplay != null) TxtDiscountDisplay.Text = $"-{discount:N0} đ";
        if (TxtFinalAmount != null) TxtFinalAmount.Text = $"{finalAmount:N0} đ";
    }

    private void ConfirmCreateOrder_Click(object sender, RoutedEventArgs e)
    {
        if (CboCustomer.SelectedValue == null)
        {
            MessageBox.Show("Vui lòng chọn khách hàng cho đơn hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CartItems.Count == 0)
        {
            MessageBox.Show("Đơn hàng không có sản phẩm nào! Vui lòng thêm ít nhất một sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal.TryParse(TxtDiscount.Text.Trim(), out var discount);
        if (discount < 0) discount = 0;

        int customerId = (int)CboCustomer.SelectedValue;
        var paymentMethod = (PaymentMethod)CboPaymentMethod.SelectedItem;

        ResultOrderDto = new CreateOrderDto
        {
            CustomerId = customerId,
            DiscountAmount = discount,
            PaymentMethod = paymentMethod,
            ShippingAddress = string.IsNullOrWhiteSpace(TxtShippingAddress.Text) ? null : TxtShippingAddress.Text.Trim(),
            CustomerNote = string.IsNullOrWhiteSpace(TxtNote.Text) ? null : TxtNote.Text.Trim(),
            Items = CartItems.Select(i => new CreateOrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Discount = 0
            }).ToList()
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
