using LaptopStore.Application.DTOs.Ai;
using LaptopStore.Application.Interfaces;
using LaptopStore.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaptopStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiAssistantService _aiAssistantService;

    public AiController(IAiAssistantService aiAssistantService)
    {
        _aiAssistantService = aiAssistantService;
    }

    /// <summary>
    /// Gửi câu hỏi nghiệp vụ tới Trợ lý AI.
    /// AI sẽ tự động phân tích ý định, gọi các Tool/API tương ứng và kiểm soát phân quyền dựa theo Role của người dùng.
    /// </summary>
    [HttpPost("ask")]
    [Authorize]
    public async Task<IActionResult> Ask([FromBody] AiQueryRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { message = "Nội dung câu hỏi không được để trống." });

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = int.TryParse(userIdStr, out var userId);

        var roleStr = User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<UserRole>(roleStr, out var role))
        {
            role = UserRole.SalesStaff;
        }

        var response = await _aiAssistantService.ProcessUserPromptAsync(request.Prompt, userId, role);
        return Ok(response);
    }
}
