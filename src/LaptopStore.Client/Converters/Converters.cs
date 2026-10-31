using LaptopStore.Application.DTOs.Products;
using LaptopStore.Domain.Enums;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LaptopStore.Client.Converters;

/// <summary>Văn bản tiếng Việt và màu sắc hiển thị cho các giá trị nghiệp vụ.</summary>
public static class Display
{
    public static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string OrderStatusText(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Processing => "Đang xử lý",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Completed => "Hoàn thành",
        OrderStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };

    public static string PaymentMethodText(PaymentMethod m) => m switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.CreditCard => "Thẻ",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        PaymentMethod.Installment => "Trả góp 0%",
        _ => m.ToString()
    };

    public static string PaymentStatusText(PaymentStatus s) => s switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.PartiallyPaid => "Thanh toán một phần",
        PaymentStatus.Paid => "Đã thanh toán",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        _ => s.ToString()
    };

    public static string RoleText(UserRole r) => r switch
    {
        UserRole.Admin => "Quản trị viên",
        UserRole.Manager => "Quản lý",
        UserRole.SalesStaff => "Nhân viên bán hàng",
        UserRole.WarehouseStaff => "Nhân viên kho",
        _ => r.ToString()
    };

    /// <summary>Hạng thành viên dựa trên tổng chi tiêu tích lũy.</summary>
    public static string Tier(decimal totalSpent) => totalSpent switch
    {
        >= 200_000_000 => "Kim cương",
        >= 100_000_000 => "Vàng",
        >= 50_000_000 => "Bạc",
        _ => "Thành viên"
    };

    /// <summary>Mức tồn kho: 0 = hết, 1 = rất thấp, 2 = thấp, 3 = ổn.</summary>
    public static int StockLevel(ProductDto p)
    {
        if (p.StockQuantity <= 0) return 0;
        int min = Math.Max(p.MinStockLevel, 1);
        if (p.StockQuantity <= Math.Ceiling(min / 2.0)) return 1;
        if (p.StockQuantity <= min) return 2;
        return 3;
    }

    public static string ProductState(ProductDto p)
    {
        if (p.Status == ProductStatus.Discontinued) return "Ngừng KD";
        return StockLevel(p) switch { 0 => "Hết hàng", 1 => "Sắp hết", 2 => "Tồn thấp", _ => "Đang bán" };
    }

    /// <summary>Số tiền rút gọn: 1,26 tỷ / 286,4 tr / 950 N.</summary>
    public static string ShortMoney(decimal v)
    {
        if (Math.Abs(v) >= 1_000_000_000) return (v / 1_000_000_000m).ToString("0.##", Vi) + " tỷ";
        if (Math.Abs(v) >= 1_000_000) return (v / 1_000_000m).ToString("0.#", Vi) + " tr";
        if (Math.Abs(v) >= 1_000) return (v / 1_000m).ToString("0", Vi) + " N";
        return v.ToString("0", Vi);
    }

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var picked = words.Length >= 2 ? words[^2..] : words;
        return string.Concat(picked.Select(w => char.ToUpper(w[0], Vi)));
    }

    // Bảng màu cho các nhãn trạng thái: (chữ, nền)
    public static (string Fg, string Bg) Tone(string tone) => tone switch
    {
        "ok" => ("#047857", "#E7F8F1"),
        "warn" => ("#B45309", "#FEF4E2"),
        "bad" => ("#B91C1C", "#FDECEC"),
        "info" => ("#0369A1", "#E6F4FC"),
        "violet" => ("#6D28D9", "#F1EBFE"),
        "accent" => ("#1D4ED8", "#EFF6FF"),
        _ => ("#475569", "#EEF2F6")
    };

    public static string ToneOf(object? value) => value switch
    {
        OrderStatus s => s switch
        {
            OrderStatus.Pending => "warn",
            OrderStatus.Processing => "violet",
            OrderStatus.Shipping => "info",
            OrderStatus.Completed => "ok",
            OrderStatus.Cancelled => "bad",
            _ => "mute"
        },
        ProductDto p => p.Status == ProductStatus.Discontinued ? "mute"
            : StockLevel(p) switch { 0 or 1 => "bad", 2 => "warn", _ => "ok" },
        string tier => tier switch { "Kim cương" => "info", "Vàng" => "warn", _ => "mute" },
        _ => "mute"
    };

    private static readonly string[][] AvatarPalette =
    {
        new[] { "#34D399", "#0EA5E9" }, new[] { "#F472B6", "#FB923C" }, new[] { "#60A5FA", "#6366F1" },
        new[] { "#A78BFA", "#EC4899" }, new[] { "#FBBF24", "#F97316" }, new[] { "#2DD4BF", "#3B82F6" },
        new[] { "#4ADE80", "#14B8A6" }, new[] { "#C084FC", "#6366F1" }
    };

    public static Brush AvatarBrush(string? key)
    {
        int hash = 0;
        foreach (var ch in key ?? string.Empty) hash = unchecked(hash * 31 + ch);
        var pair = AvatarPalette[Math.Abs(hash % AvatarPalette.Length)];
        var brush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString(pair[0]),
            (Color)ColorConverter.ConvertFromString(pair[1]), 45);
        brush.Freeze();
        return brush;
    }

    // Ô vuông màu nhạt cho ảnh thu nhỏ sản phẩm: (chữ, nền)
    private static readonly (string Fg, string Bg)[] ThumbPalette =
    {
        ("#2563EB", "#EFF6FF"), ("#0284C7", "#E6F4FC"), ("#059669", "#E7F8F1"), ("#DC2626", "#FDECEC"),
        ("#7C3AED", "#F1EBFE"), ("#334155", "#F1F5F9"), ("#D97706", "#FEF4E2")
    };

    public static (string Fg, string Bg) ThumbTone(string? key)
    {
        int hash = 0;
        foreach (var ch in key ?? string.Empty) hash = unchecked(hash * 31 + ch);
        return ThumbPalette[Math.Abs(hash % ThumbPalette.Length)];
    }

    public static Brush Hex(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }
}

/// <summary>Chuyển enum/giá trị nghiệp vụ sang chữ tiếng Việt.</summary>
public class DisplayTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        OrderStatus s => Display.OrderStatusText(s),
        PaymentMethod m => Display.PaymentMethodText(m),
        PaymentStatus p => Display.PaymentStatusText(p),
        UserRole r => Display.RoleText(r),
        ProductDto p => Display.ProductState(p),
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Màu chữ (parameter = "fg") hoặc nền (parameter = "bg") của nhãn trạng thái.</summary>
public class ToneBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var (fg, bg) = Display.Tone(Display.ToneOf(value));
        return Display.Hex((parameter as string) == "bg" ? bg : fg);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Chữ viết tắt từ họ tên / tên sản phẩm.</summary>
public class InitialsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string;
        if ((parameter as string) == "brand")
        {
            // Lấy 2 ký tự đầu của hãng/tên sản phẩm cho ô ảnh thu nhỏ
            var word = s?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "?";
            return word.Length >= 2 ? word[..2].ToUpper(Display.Vi) : word.ToUpper(Display.Vi);
        }
        return Display.Initials(s);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Màu gradient cố định theo tên (ảnh đại diện).</summary>
public class AvatarBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Display.AvatarBrush(value as string);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Màu ô thu nhỏ sản phẩm theo hãng: parameter "fg" hoặc "bg".</summary>
public class ThumbBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var (fg, bg) = Display.ThumbTone(value as string);
        return Display.Hex((parameter as string) == "bg" ? bg : fg);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Thanh tồn kho: parameter "width" (độ dài thanh, tối đa 64px) hoặc "brush".</summary>
public class StockBarConverter : IValueConverter
{
    public double MaxWidth { get; set; } = 64;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ProductDto p) return (parameter as string) == "brush" ? Brushes.Transparent : 0.0;
        if ((parameter as string) == "brush")
            return Display.Hex(Display.StockLevel(p) switch { 0 or 1 => "#EF4444", 2 => "#F59E0B", _ => "#10B981" });

        double full = Math.Max(Math.Max(p.MinStockLevel, 1) * 4, 20);
        return Math.Clamp(p.StockQuantity / full, 0.04, 1) * MaxWidth;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Số tiền rút gọn (1,26 tỷ).</summary>
public class ShortMoneyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal d ? Display.ShortMoney(d) : value?.ToString() ?? string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Hạng thành viên từ tổng chi tiêu.</summary>
public class TierConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var tier = value is decimal d ? Display.Tier(d) : "Thành viên";
        if (parameter is "fg" or "bg")
        {
            var (fg, bg) = Display.Tone(Display.ToneOf(tier));
            return Display.Hex((string)parameter == "bg" ? bg : fg);
        }
        return tier;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Biên lợi nhuận của sản phẩm.</summary>
public class MarginConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ProductDto p || p.Price <= 0) return string.Empty;
        var margin = (p.Price - p.CostPrice) / p.Price;
        return margin.ToString("0.0%", Display.Vi);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>
/// null / chuỗi rỗng / số 0 / tập rỗng / false → Collapsed. Parameter "invert" để đảo ngược.
/// </summary>
public class VisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool visible = value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            int i => i != 0,
            ICollection c => c.Count > 0,
            _ => true
        };
        if ((parameter as string) == "invert") visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>So sánh giá trị với parameter (dạng chuỗi) → bool.</summary>
public class EqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Viết hoa toàn bộ (tiêu đề cột bảng).</summary>
public class UpperCaseConverter : IValueConverter
{
    public static readonly UpperCaseConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString()?.ToUpper(Display.Vi) ?? string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>
/// Đảo dấu Thickness. Vùng nhập của TextBox/PasswordBox đã tự cộng Padding bên trong,
/// nên template bù lại bằng margin âm để chữ, con trỏ và placeholder thẳng hàng.
/// </summary>
public class NegateThicknessConverter : IValueConverter
{
    public static readonly NegateThicknessConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Thickness t ? new Thickness(-t.Left, -t.Top, -t.Right, -t.Bottom) : new Thickness();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
