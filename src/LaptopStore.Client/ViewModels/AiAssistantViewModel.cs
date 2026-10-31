using CommunityToolkit.Mvvm.Input;
using LaptopStore.Client.Services;
using System.Collections.ObjectModel;

namespace LaptopStore.Client.ViewModels;

public class ChatMessageItem
{
    public string Sender { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsAi { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;
    public string? ToolInfo { get; set; }
}

public class AiAssistantViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private string _userPrompt = string.Empty;

    public string UserPrompt
    {
        get => _userPrompt;
        set
        {
            if (SetProperty(ref _userPrompt, value))
                SendMessageCommand.NotifyCanExecuteChanged();
        }
    }

    public ObservableCollection<ChatMessageItem> Messages { get; } = new();

    public IAsyncRelayCommand SendMessageCommand { get; }
    public IRelayCommand<string> QuickPromptCommand { get; }

    public AiAssistantViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, () => !string.IsNullOrWhiteSpace(UserPrompt) && !IsBusy);
        QuickPromptCommand = new RelayCommand<string>(ExecuteQuickPrompt);

        // Tin nhắn chào mừng mặc định
        Messages.Add(new ChatMessageItem
        {
            Sender = "Trợ lý AI",
            Message = "Xin chào! Tôi là Trợ lý AI của TechGear Laptop Store. Bạn có thể hỏi tôi về báo cáo doanh thu, cảnh báo tồn kho hoặc tư vấn chọn máy theo ngân sách.",
            IsAi = true
        });
    }

    /// <summary>Tên dễ hiểu của các tool nghiệp vụ mà AI đã gọi.</summary>
    private static string DescribeTool(string toolName) => toolName switch
    {
        "get_sales_report" => "Báo cáo doanh thu",
        "get_low_stock_products" => "Sản phẩm sắp hết hàng",
        "search_products_by_requirement" => "Tìm sản phẩm phù hợp",
        _ => toolName
    };

    private void ExecuteQuickPrompt(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return;
        UserPrompt = prompt;
        _ = SendMessageAsync();
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserPrompt)) return;

        var text = UserPrompt.Trim();
        UserPrompt = string.Empty;

        Messages.Add(new ChatMessageItem
        {
            Sender = "Bạn",
            Message = text,
            IsAi = false
        });

        IsBusy = true;
        StatusMessage = "AI đang suy nghĩ và truy vấn dữ liệu nghiệp vụ...";

        try
        {
            var result = await _apiClient.AskAiAsync(text);
            if (result != null)
            {
                string? tools = result.ExecutedTools.Count > 0
                    ? "Đã tra cứu dữ liệu: " + string.Join(", ", result.ExecutedTools.Select(t => DescribeTool(t.ToolName)).Distinct())
                    : null;

                Messages.Add(new ChatMessageItem
                {
                    Sender = "Trợ lý AI",
                    Message = result.Answer,
                    IsAi = true,
                    ToolInfo = tools
                });
            }
            else
            {
                Messages.Add(new ChatMessageItem
                {
                    Sender = "Trợ lý AI",
                    Message = "Không nhận được phản hồi từ máy chủ AI. Vui lòng kiểm tra lại kết nối API server.",
                    IsAi = true
                });
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessageItem
            {
                Sender = "Hệ thống",
                Message = $"Lỗi gửi câu hỏi tới AI: {ex.Message}",
                IsAi = true
            });
        }
        finally
        {
            IsBusy = false;
            StatusMessage = "Sẵn sàng.";
        }
    }
}
