using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BLL.IServices;
using DAL.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace SnakeArchitectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameRequestController : ControllerBase
    {
        private readonly IGameRequestService _gameRequestService;
        private readonly IHubContext<ChatHub> _hubContext;

        public GameRequestController(IGameRequestService gameRequestService, IHubContext<ChatHub> hubContext)
        {
            _gameRequestService = gameRequestService;
            _hubContext = hubContext;
        }

       
        [HttpPost("send")]
        public async Task<IActionResult> SendGameRequest([FromBody] SendGameRequestDTO dto)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _gameRequestService.SendGameRequestAsync(senderId, dto.RecipientId, dto.GameRoomId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { requestId = result.RequestId, message = result.Message });
        }

        [HttpPost("join/{roomId}")]
        public async Task<IActionResult> RequestJoinGame(int roomId)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _gameRequestService.RequestJoinGameAsync(senderId, roomId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { requestId = result.RequestId, message = result.Message });
        }

       
        [HttpGet("incoming")]
        public async Task<IActionResult> GetIncomingRequests()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var requests = await _gameRequestService.GetIncomingRequestsAsync(userId);
            return Ok(requests);
        }

       
        [HttpPost("{requestId}/accept")]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _gameRequestService.AcceptGameRequestAsync(requestId, userId);

            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN") return Forbid();
                if (result.Message == "Pozivnica nije pronađena." || result.Message == "Soba nije pronađena.")
                    return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            await _hubContext.Clients.Group(result.JoinedUsername)
                .SendAsync("JoinedGameRoom", result.RoomId, result.PlayerId);

            return Ok(new
            {
                playerId = result.PlayerId,
                roomId = result.RoomId,
                message = result.Message
            });
        }


        [HttpDelete("{requestId}")]
        public async Task<IActionResult> DeclineRequest(int requestId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _gameRequestService.DeclineGameRequestAsync(requestId, userId);

            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN") return Forbid();
                return NotFound(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}
