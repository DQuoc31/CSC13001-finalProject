using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LaptopStore.Client.Controls;

/// <summary>
/// Attached properties dùng trong các template của Themes/Controls.xaml:
/// chữ gợi ý (placeholder), icon đứng trước, trạng thái "đang chọn" cho nút điều hướng.
/// </summary>
public static class Ui
{
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached(
        "Placeholder", typeof(string), typeof(Ui), new FrameworkPropertyMetadata(string.Empty));

    public static string GetPlaceholder(DependencyObject d) => (string)d.GetValue(PlaceholderProperty);
    public static void SetPlaceholder(DependencyObject d, string value) => d.SetValue(PlaceholderProperty, value);

    public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
        "Icon", typeof(Geometry), typeof(Ui), new FrameworkPropertyMetadata(null));

    public static Geometry? GetIcon(DependencyObject d) => (Geometry?)d.GetValue(IconProperty);
    public static void SetIcon(DependencyObject d, Geometry? value) => d.SetValue(IconProperty, value);

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.RegisterAttached(
        "IsActive", typeof(bool), typeof(Ui), new FrameworkPropertyMetadata(false));

    public static bool GetIsActive(DependencyObject d) => (bool)d.GetValue(IsActiveProperty);
    public static void SetIsActive(DependencyObject d, bool value) => d.SetValue(IsActiveProperty, value);

    public static readonly DependencyProperty BadgeProperty = DependencyProperty.RegisterAttached(
        "Badge", typeof(object), typeof(Ui), new FrameworkPropertyMetadata(null));

    public static object? GetBadge(DependencyObject d) => d.GetValue(BadgeProperty);
    public static void SetBadge(DependencyObject d, object? value) => d.SetValue(BadgeProperty, value);

    /// <summary>Tự cuộn ScrollViewer xuống cuối mỗi khi nội dung tăng chiều cao (dùng cho khung chat).</summary>
    public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.RegisterAttached(
        "AutoScroll", typeof(bool), typeof(Ui), new PropertyMetadata(false, OnAutoScrollChanged));

    public static bool GetAutoScroll(DependencyObject d) => (bool)d.GetValue(AutoScrollProperty);
    public static void SetAutoScroll(DependencyObject d, bool value) => d.SetValue(AutoScrollProperty, value);

    private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer sv) return;
        if ((bool)e.NewValue) sv.ScrollChanged += ScrollToEndOnGrow;
        else sv.ScrollChanged -= ScrollToEndOnGrow;
    }

    private static void ScrollToEndOnGrow(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange > 0) ((ScrollViewer)sender).ScrollToEnd();
    }
}
