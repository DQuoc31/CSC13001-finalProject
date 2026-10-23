namespace LaptopStore.Application.DTOs.Ai;

public class AiQueryRequestDto
{
    public string Prompt { get; set; } = string.Empty;
}

public class AiQueryResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public string? SuggestedAction { get; set; }
    public List<AiToolCallResultDto> ExecutedTools { get; set; } = new();
}

public class AiToolCallResultDto
{
    public string ToolName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public object? ResultSummary { get; set; }
}
