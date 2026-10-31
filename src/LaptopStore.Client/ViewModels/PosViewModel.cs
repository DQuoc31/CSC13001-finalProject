using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaptopStore.Application.DTOs.Categories;
using LaptopStore.Application.DTOs.Customers;
using LaptopStore.Application.DTOs.Orders;
using LaptopStore.Application.DTOs.Products;
using LaptopStore.Client.Services;
using LaptopStore.Domain.Enums;
using System.Collections.ObjectModel;

namespace LaptopStore.Client.ViewModels;

/// <summary>Sản phẩm trên lưới POS, kèm số lượng đang có trong giỏ.</summary>
public class PosProductItem : ObservableObject
{
    private int _inCart;

    public PosProductItem(ProductDto product) => Product = product;

    public ProductDto Product { get; }

    public int InCart
    {
        get => _inCart;
        set
        {
            if (SetProperty(ref _inCart, value)) OnPropertyChanged(nameof(IsInCart));
        }
    }

    public bool IsInCart => InCart > 0;
}

/// <summary>Một dòng trong giỏ hàng.</summary>
public class CartLine : ObservableObject
{
    private int _quantity;

    public CartLine(ProductDto product, int quantity)
    {
        Product = product;
        _quantity = quantity;
    }

    public ProductDto Product { get; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value)) OnPropertyChanged(nameof(SubTotal));
        }
    }

    public decimal SubTotal => Product.Price * Quantity;
}

/// <summary>Trang Bán hàng tại quầy (Point of Sale).</summary>
public class PosViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;

    private List<PosProductItem> _allProducts = new();
    private string _searchKeyword = string.Empty;
    private CategoryDto? _selectedCategory;
    private CustomerDto? _selectedCustomer;
    private string _discountText = "0";
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    private string? _shippingAddress;
    private string? _note;
    private string? _warning;
    private string? _successMessage;

    /// <summary>Phát ra sau khi lập đơn thành công để trang chính cập nhật số đếm.</summary>
    public event Action? OrderCreated;

    public ObservableCollection<PosProductItem> Products { get; } = new();
    public ObservableCollection<CategoryDto> Categories { get; } = new();
    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<CartLine> Cart { get; } = new();

    public string SearchKeyword
    {
        get => _searchKeyword;
        set { if (SetProperty(ref _searchKeyword, value)) ApplyFilter(); }
    }

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set { if (SetProperty(ref _selectedCategory, value)) ApplyFilter(); }
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    /// <summary>Giảm giá (VNĐ) nhập dạng chữ để cho phép gõ tự do.</summary>
    public string DiscountText
    {
        get => _discountText;
        set { if (SetProperty(ref _discountText, value)) RaiseTotals(); }
    }

    public string? ShippingAddress { get => _shippingAddress; set => SetProperty(ref _shippingAddress, value); }
    public string? Note { get => _note; set => SetProperty(ref _note, value); }

    /// <summary>Cảnh báo hiển thị ngay trong giỏ (ví dụ vượt tồn kho).</summary>
    public string? Warning { get => _warning; set => SetProperty(ref _warning, value); }
    public string? SuccessMessage { get => _successMessage; set => SetProperty(ref _successMessage, value); }

    // Phương thức thanh toán (mỗi RadioButton gắn với một thuộc tính bool)
    public bool IsCash { get => _paymentMethod == PaymentMethod.Cash; set { if (value) SetPayment(PaymentMethod.Cash); } }
    public bool IsBankTransfer { get => _paymentMethod == PaymentMethod.BankTransfer; set { if (value) SetPayment(PaymentMethod.BankTransfer); } }
    public bool IsCard { get => _paymentMethod == PaymentMethod.CreditCard; set { if (value) SetPayment(PaymentMethod.CreditCard); } }
    public bool IsInstallment { get => _paymentMethod == PaymentMethod.Installment; set { if (value) SetPayment(PaymentMethod.Installment); } }

    public int ItemCount => Cart.Sum(c => c.Quantity);
    public decimal TotalAmount => Cart.Sum(c => c.SubTotal);
    public decimal DiscountAmount
    {
        get
        {
            decimal.TryParse(DiscountText?.Replace(".", "").Replace(",", "").Trim(), out var d);
            return Math.Clamp(d, 0, TotalAmount);
        }
    }
    public decimal FinalAmount => TotalAmount - DiscountAmount;

    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand<PosProductItem> AddToCartCommand { get; }
    public IRelayCommand<CartLine> IncreaseCommand { get; }
    public IRelayCommand<CartLine> DecreaseCommand { get; }
    public IRelayCommand<CartLine> RemoveCommand { get; }
    public IRelayCommand ClearCartCommand { get; }
    public IAsyncRelayCommand CheckoutCommand { get; }

    public PosViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddToCartCommand = new RelayCommand<PosProductItem>(p => { if (p != null) ChangeQuantity(p.Product, +1); });
        IncreaseCommand = new RelayCommand<CartLine>(l => { if (l != null) ChangeQuantity(l.Product, +1); });
        DecreaseCommand = new RelayCommand<CartLine>(l => { if (l != null) ChangeQuantity(l.Product, -1); });
        RemoveCommand = new RelayCommand<CartLine>(l => { if (l != null) ChangeQuantity(l.Product, -l.Quantity); });
        ClearCartCommand = new RelayCommand(ClearCart);
        CheckoutCommand = new AsyncRelayCommand(CheckoutAsync);
        Cart.CollectionChanged += (_, _) => RaiseTotals();
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang tải sản phẩm & khách hàng...";
        try
        {
            var products = await _apiClient.GetProductsAsync();
            var categories = await _apiClient.GetCategoriesAsync();
            var customers = await _apiClient.GetCustomersAsync();

            _allProducts = products
                .Where(p => p.Status != ProductStatus.Discontinued)
                .Select(p => new PosProductItem(p) { InCart = Cart.FirstOrDefault(c => c.Product.Id == p.Id)?.Quantity ?? 0 })
                .ToList();

            var selectedCategoryId = SelectedCategory?.Id ?? 0;
            Categories.Clear();
            Categories.Add(new CategoryDto { Id = 0, Name = "Tất cả" });
            foreach (var c in categories) Categories.Add(c);
            _selectedCategory = Categories.FirstOrDefault(c => c.Id == selectedCategoryId) ?? Categories[0];
            OnPropertyChanged(nameof(SelectedCategory));

            var selectedCustomerId = SelectedCustomer?.Id;
            Customers.Clear();
            foreach (var c in customers) Customers.Add(c);
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == selectedCustomerId) ?? Customers.FirstOrDefault();

            ApplyFilter();
            StatusMessage = $"Sẵn sàng bán hàng · {_allProducts.Count} sản phẩm.";
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

    private void ApplyFilter()
    {
        var keyword = SearchKeyword.Trim();
        var query = _allProducts.AsEnumerable();
        if (SelectedCategory is { Id: > 0 } cat) query = query.Where(p => p.Product.CategoryId == cat.Id);
        if (keyword.Length > 0)
        {
            query = query.Where(p =>
                p.Product.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                p.Product.Sku.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                p.Product.Brand.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        Products.Clear();
        foreach (var p in query) Products.Add(p);
    }

    /// <summary>
    /// Thêm/bớt sản phẩm trong giỏ. Kiểm tra tồn kho ngay khi thêm
    /// (Test case Order-02: mua vượt tồn kho → từ chối và cảnh báo).
    /// </summary>
    private void ChangeQuantity(ProductDto product, int delta)
    {
        SuccessMessage = null;
        var line = Cart.FirstOrDefault(c => c.Product.Id == product.Id);
        int current = line?.Quantity ?? 0;
        int next = current + delta;

        if (delta > 0 && next > product.StockQuantity)
        {
            Warning = product.StockQuantity <= 0
                ? $"{product.Name} đã hết hàng."
                : $"{product.Name} chỉ còn {product.StockQuantity} chiếc trong kho — không thể thêm {next}.";
            return;
        }

        Warning = null;
        if (next <= 0)
        {
            if (line != null) Cart.Remove(line);
        }
        else if (line == null)
        {
            Cart.Add(new CartLine(product, next));
        }
        else
        {
            line.Quantity = next;
        }

        var item = _allProducts.FirstOrDefault(p => p.Product.Id == product.Id);
        if (item != null) item.InCart = Math.Max(next, 0);
        RaiseTotals();
    }

    private void ClearCart()
    {
        Cart.Clear();
        foreach (var p in _allProducts) p.InCart = 0;
        Warning = null;
        DiscountText = "0";
        ShippingAddress = null;
        Note = null;
    }

    private void SetPayment(PaymentMethod method)
    {
        _paymentMethod = method;
        OnPropertyChanged(nameof(IsCash));
        OnPropertyChanged(nameof(IsBankTransfer));
        OnPropertyChanged(nameof(IsCard));
        OnPropertyChanged(nameof(IsInstallment));
    }

    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(FinalAmount));
    }

    private async Task CheckoutAsync()
    {
        SuccessMessage = null;
        if (SelectedCustomer == null)
        {
            Warning = "Vui lòng chọn khách hàng cho đơn hàng.";
            return;
        }
        if (Cart.Count == 0)
        {
            Warning = "Giỏ hàng đang trống — hãy chọn ít nhất một sản phẩm.";
            return;
        }

        var dto = new CreateOrderDto
        {
            CustomerId = SelectedCustomer.Id,
            DiscountAmount = DiscountAmount,
            PaymentMethod = _paymentMethod,
            ShippingAddress = string.IsNullOrWhiteSpace(ShippingAddress) ? null : ShippingAddress.Trim(),
            CustomerNote = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
            Items = Cart.Select(c => new CreateOrderItemDto { ProductId = c.Product.Id, Quantity = c.Quantity }).ToList()
        };

        IsBusy = true;
        Warning = null;
        StatusMessage = "Đang lập đơn hàng và kiểm tra tồn kho...";
        try
        {
            var result = await _apiClient.CreateOrderAsync(dto);
            if (result.IsSuccess)
            {
                var order = result.Value;
                SuccessMessage = $"Đã lập đơn {order.OrderNumber} · {order.FinalAmount:N0} đ. Tồn kho đã được cập nhật.";
                StatusMessage = $"Tạo đơn hàng {order.OrderNumber} thành công!";
                ClearCart();
                OrderCreated?.Invoke();
                await LoadAsync();
            }
            else
            {
                Warning = result.Error ?? "Không thể tạo đơn hàng. Vui lòng kiểm tra lại số lượng tồn kho.";
                StatusMessage = Warning;
            }
        }
        catch (Exception ex)
        {
            Warning = $"Lỗi: {ex.Message}";
            StatusMessage = Warning;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
