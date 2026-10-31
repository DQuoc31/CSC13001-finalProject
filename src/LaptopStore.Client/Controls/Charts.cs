using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace LaptopStore.Client.Controls;

/// <summary>Một cột trong biểu đồ doanh thu.</summary>
public class ChartBar
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    /// <summary>Phần giá trị phụ vẽ chồng ở đáy cột (ví dụ lợi nhuận gộp).</summary>
    public double Secondary { get; set; }
    public string? ValueText { get; set; }
    public bool IsHighlight { get; set; }
}

/// <summary>Một lát trong biểu đồ tròn.</summary>
public class ChartSlice
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public Brush Brush { get; set; } = Brushes.Gray;
    public string Percent { get; set; } = string.Empty;
}

/// <summary>Lớp nền cho biểu đồ: vẽ lại khi danh sách dữ liệu thay đổi.</summary>
public abstract class ChartBase : FrameworkElement
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IEnumerable), typeof(ChartBase),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsChanged));

    public IEnumerable? Items
    {
        get => (IEnumerable?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (ChartBase)d;
        if (e.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= chart.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += chart.OnCollectionChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    protected static readonly Typeface TextFace = new(new FontFamily("Segoe UI Variable Text, Segoe UI"),
        FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    protected static readonly Typeface TextFaceBold = new(new FontFamily("Segoe UI Variable Text, Segoe UI"),
        FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    protected FormattedText Text(string s, double size, Brush brush, bool bold = false) =>
        new(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, bold ? TextFaceBold : TextFace,
            size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);

    protected static SolidColorBrush Hex(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }
}

/// <summary>Biểu đồ cột doanh thu theo tháng (tự vẽ, không cần thư viện ngoài).</summary>
public class BarChart : ChartBase
{
    private static readonly Brush GridBrush = Hex("#EEF1F5");
    private static readonly Brush LabelBrush = Hex("#94A3B8");
    private static readonly Brush StrongLabel = Hex("#0F172A");
    private static readonly Brush BarBrush = Hex("#DBEAFE");
    private static readonly Brush SecondaryBrush = Hex("#BFDBFE");
    private static readonly Brush HighlightSecondary = Hex("#1E40AF");
    private static readonly Brush TooltipBrush = Hex("#0F172A");
    private static readonly Brush HighlightBrush = CreateHighlight();

    private static Brush CreateHighlight()
    {
        var b = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString("#60A5FA"),
            (Color)ColorConverter.ConvertFromString("#2563EB"), 90);
        b.Freeze();
        return b;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var bars = Items?.OfType<ChartBar>().ToList();
        if (bars == null || bars.Count == 0 || ActualWidth < 50 || ActualHeight < 50) return;

        const double left = 40, bottom = 26, top = 30;
        double plotH = ActualHeight - bottom - top;
        double plotW = ActualWidth - left;

        double max = NiceMax(bars.Max(b => b.Value));
        var gridPen = new Pen(GridBrush, 1);

        // Lưới ngang + nhãn trục Y
        for (int g = 0; g <= 4; g++)
        {
            double y = Math.Round(top + plotH * (1 - g / 4.0)) + 0.5;
            dc.DrawLine(gridPen, new Point(left, y), new Point(ActualWidth, y));
            var t = Text(FormatAxis(max * g / 4), 11, LabelBrush);
            dc.DrawText(t, new Point(left - 8 - t.Width, y - t.Height / 2));
        }

        double slot = plotW / bars.Count;
        double barW = Math.Min(38, slot * 0.56);

        for (int i = 0; i < bars.Count; i++)
        {
            var b = bars[i];
            double x = left + i * slot + (slot - barW) / 2;
            double h = max > 0 ? plotH * b.Value / max : 0;
            double y = top + plotH - h;

            if (h > 0)
            {
                var rect = new Rect(x, y, barW, h);
                dc.PushClip(new RectangleGeometry(new Rect(x, y, barW, h)));
                dc.DrawRoundedRectangle(b.IsHighlight ? HighlightBrush : BarBrush, null,
                    new Rect(rect.X, rect.Y, rect.Width, rect.Height + 6), 6, 6);
                double sh = max > 0 ? plotH * Math.Min(b.Secondary, b.Value) / max : 0;
                if (sh > 0)
                    dc.DrawRectangle(b.IsHighlight ? HighlightSecondary : SecondaryBrush, null,
                        new Rect(x, top + plotH - sh, barW, sh));
                dc.Pop();
            }

            var label = Text(b.Label, 11, b.IsHighlight ? StrongLabel : LabelBrush, b.IsHighlight);
            dc.DrawText(label, new Point(x + barW / 2 - label.Width / 2, ActualHeight - bottom + 8));

            if (b.IsHighlight && !string.IsNullOrEmpty(b.ValueText))
            {
                var tip = Text(b.ValueText, 11.5, Brushes.White, true);
                double tw = tip.Width + 18, th = 24;
                double tx = Math.Min(Math.Max(x + barW / 2 - tw / 2, left), ActualWidth - tw);
                double ty = Math.Max(y - th - 6, 0);
                dc.DrawRoundedRectangle(TooltipBrush, null, new Rect(tx, ty, tw, th), 7, 7);
                dc.DrawText(tip, new Point(tx + 9, ty + (th - tip.Height) / 2));
            }
        }
    }

    private static double NiceMax(double v)
    {
        if (v <= 0) return 1;
        double exp = Math.Pow(10, Math.Floor(Math.Log10(v)));
        double f = v / exp;
        double nice = f <= 1 ? 1 : f <= 2 ? 2 : f <= 4 ? 4 : f <= 5 ? 5 : f <= 8 ? 8 : 10;
        return nice * exp;
    }

    private static string FormatAxis(double v) => v >= 1000 ? (v / 1000).ToString("0.#") + "k" : v.ToString("0");
}

/// <summary>Biểu đồ tròn rỗng giữa (donut) cho tỉ lệ trạng thái đơn hàng.</summary>
public class DonutChart : ChartBase
{
    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(DonutChart),
        new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    private static readonly Brush EmptyBrush = Hex("#EEF1F5");

    protected override void OnRender(DrawingContext dc)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        double r = (size - Thickness) / 2;
        var slices = Items?.OfType<ChartSlice>().Where(s => s.Value > 0).ToList() ?? new();
        double total = slices.Sum(s => s.Value);

        if (total <= 0)
        {
            dc.DrawEllipse(null, new Pen(EmptyBrush, Thickness), center, r, r);
            return;
        }

        double angle = -90;
        foreach (var s in slices)
        {
            double sweep = 360 * s.Value / total;
            var pen = new Pen(s.Brush, Thickness);
            if (sweep >= 359.99)
            {
                dc.DrawEllipse(null, pen, center, r, r);
                break;
            }

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(PointOn(center, r, angle), false, false);
                ctx.ArcTo(PointOn(center, r, angle + sweep), new Size(r, r), 0, sweep > 180,
                    SweepDirection.Clockwise, true, false);
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);
            angle += sweep;
        }
    }

    private static Point PointOn(Point c, double r, double deg)
    {
        double rad = deg * Math.PI / 180;
        return new Point(c.X + r * Math.Cos(rad), c.Y + r * Math.Sin(rad));
    }
}
