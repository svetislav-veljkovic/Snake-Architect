using BLL.IServices;
using BLL.Services;
using DAL.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SnakeArchitectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatDTO dto)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _chatService.SendMessageAsync(dto, senderId);

            if (result == null)
                return NotFound(new { message = "Primatelj nije pronađen." });

            var errorProp = result.GetType().GetProperty("error");
            if (errorProp != null)
            {
                var errorVal = errorProp.GetValue(result)?.ToString();
                if (errorVal == "Forbid") return Forbid();
                return BadRequest(new { message = errorVal });
            }

            return Ok(result);
        }

        [HttpGet("conversation/{otherUserId}")]
        public IActionResult GetConversation(int otherUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var conversation = _chatService.GetConversationAsync(userId, otherUserId, page, pageSize).Result;
            return Ok(conversation);
        }

        [HttpGet("inbox")]
        public IActionResult GetInbox()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var inbox = _chatService.GetInboxAsync(userId).Result;
            return Ok(inbox);
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _chatService.DeleteMessageAsync(messageId, userId);

            if (!success)
                return NotFound(new { message = "Poruka nije pronađena ili nemate pravo da je obrišete." });

            return Ok(new { message = "Poruka obrisana." });
        }
    }
}