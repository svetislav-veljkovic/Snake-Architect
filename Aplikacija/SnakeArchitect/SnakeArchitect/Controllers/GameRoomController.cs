using BLL.Services.IServices;
using DAL.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SnakeArchitectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameRoomController : ControllerBase
    {
        private readonly IGameRoomService _gameRoomService;
        private readonly IHubContext<ChatHub> _hubContext;

        public GameRoomController(IGameRoomService gameRoomService, IHubContext<ChatHub> hubContext)
        {
            _gameRoomService = gameRoomService;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] CreateGameRoomDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _gameRoomService.CreateRoomAsync(dto.Name, userId, dto.MinPlayers);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new
            {
                roomId = result.RoomId,
                message = result.Message
            });
        }

        [HttpPost("{id}/board")]
        public async Task<IActionResult> CreateBoard(int id, [FromBody] InitBoardDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _gameRoomService.CreateBoardAsync(id, userId, dto.Rows, dto.Columns);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN") return Forbid();
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { boardId = result.BoardId, message = result.Message });
        }

        [HttpPost("{id}/board/confirm")]
        public async Task<IActionResult> ConfirmBoard(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _gameRoomService.ConfirmBoardAsync(id, userId);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN") return Forbid();
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveRooms()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var rooms = await _gameRoomService.GetActiveRoomsAsync(userId);
            return Ok(rooms);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoom(int id)
        {
            var room = await _gameRoomService.GetRoomByIdAsync(id);
            if (room == null)
                return NotFound(new { message = "Soba nije pronadjena." });

            return Ok(room);
        }

        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinRoom(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _gameRoomService.JoinRoomAsync(id, userId);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { playerId = result.PlayerId, message = result.Message });
        }

        [HttpPost("{id}/reconnect")]
        public async Task<IActionResult> Reconnect(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _gameRoomService.ReconnectAsync(id, userId);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartRoom(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _gameRoomService.StartRoomAsync(id, userId);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN")
                    return Forbid();

                return BadRequest(new { message = result.Message });
            }

            await _hubContext.Clients.Group("game:" + id).SendAsync("GameStarted");
            return Ok(new { message = result.Message });
        }

        [HttpPost("{id}/leave")]
        public async Task<IActionResult> LeaveRoom(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _gameRoomService.LeaveRoomAsync(id, userId);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpPost("{id}/leave-permanent")]
        public async Task<IActionResult> PermanentlyLeaveRoom(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _gameRoomService.PermanentlyLeaveRoomAsync(id, userId);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            await _hubContext.Clients.Group("game:" + id)
                .SendAsync("PlayerPermanentlyLeft", userId);

            return Ok(new { message = result.Message });
        }

        [HttpDelete("{id}")]
public async Task<IActionResult> DeleteRoom(int id)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var result = await _gameRoomService.DeleteRoomAsync(id, userId);
    if (!result.Success)
    {
        if (result.Message == "FORBIDDEN")
            return Forbid();

        return NotFound(new { message = result.Message });
    }

    await _hubContext.Clients.Group("game:" + id)
        .SendAsync("RoomDeleted", "Host je otkazao sobu.");

    return Ok(new { message = result.Message });
}
        



        
    }
}
