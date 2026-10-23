using LaptopStore.Application.DTOs.Ai;
using LaptopStore.Domain.Enums;

namespace LaptopStore.Application.Interfaces;

public interface IAiAssistantService
{
    Task<AiQueryResponseDto> ProcessUserPromptAsync(string prompt, int currentUserId, UserRole currentRole);
}
