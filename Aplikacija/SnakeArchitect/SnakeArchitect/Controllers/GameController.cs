using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using BLL.Services.IServices;
using SnakeArchitectApi;
namespace SnakeArchitectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly IHubContext<ChatHub> _hubContext;
        public GameController(IGameService gameService, IHubContext<ChatHub> hubContext)
        {
            _gameService = gameService;
            _hubContext = hubContext;
        }
        [HttpPost("roll/{roomId}")]
        public async Task<IActionResult> RollDice(int roomId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var roll = await _gameService.RollDiceAsync(roomId, userId);
            if (!roll.Success || roll.Result == null)
            {
                if (roll.Message == "Soba nije pronadjena.")
                    return NotFound(new { message = roll.Message });
                return BadRequest(new { message = roll.Message });
            }
            await _hubContext.Clients.Group("game:" + roomId)
                .SendAsync(
                    "ReceiveMove",
                    roll.Result.PlayerId,
                    roll.Result.FromPosition,
                    roll.Result.ToPosition,
                    roll.Result.MoveType);
            if (roll.Result.IsWinner)
            {
                await _hubContext.Clients.Group("game:" + roomId)
                    .SendAsync("ReceiveWinner", roll.Result.PlayerId);
            }
            return Ok(roll.Result);
        }
        [HttpGet("{roomId}/state")]
        public async Task<IActionResult> GetGameState(int roomId)
        {
            var state = await _gameService.GetGameStateAsync(roomId);
            if (state == null)
                return NotFound(new { message = "Soba nije pronadjena." });
            return Ok(state);
        }
        [HttpGet("{roomId}/moves")]
        public async Task<IActionResult> GetMoves(int roomId)
        {
            var moves = await _gameService.GetMovesAsync(roomId);
            if (moves == null)
                return NotFound(new { message = "Soba nije pronadjena." });
            return Ok(moves);
        }
        [HttpGet("{roomId}/winner")]
        public async Task<IActionResult> GetWinner(int roomId)
        {
            var winner = await _gameService.GetWinnerAsync(roomId);
            if (winner == null)
                return NotFound(new { message = "Soba nije pronadjena." });
            return Ok(winner);
        }
    }
}
