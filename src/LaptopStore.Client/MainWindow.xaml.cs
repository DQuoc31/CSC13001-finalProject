using LaptopStore.Client.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LaptopStore.Client;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        SourceInitialized += (_, _) => WindowCaption.Apply(this);
    }
}

/// <summary>
/// Tô màu thanh tiêu đề cửa sổ cùng tông sidebar (#00427F). Chỉ có tác dụng trên Windows 11; bản cũ hơn bỏ qua.
/// </summary>
internal static class WindowCaption
{
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void Apply(Window window, int r = 0x00, int g = 0x42, int b = 0x7F)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int caption = r | (g << 8) | (b << 16); // COLORREF = 0x00BBGGRR
        int text = 0x00FFFFFF;
        try
        {
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
    }
}
