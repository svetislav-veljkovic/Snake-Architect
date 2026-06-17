
using BLL.Services.IServices;
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
    public class GameBoardController : ControllerBase
    {
        private readonly IGameBoardService _boardService;

        public GameBoardController(IGameBoardService boardService)
        {
            _boardService = boardService;
        }

        [HttpGet("{boardId}")]
        public async Task<IActionResult> GetBoard(int boardId)
        {
            var board = await _boardService.GetBoardAsync(boardId);
            if (board == null)
                return NotFound(new { message = "Tabla nije pronađena." });

            return Ok(board);
        }

        [HttpPost("{boardId}/snake")]
        public async Task<IActionResult> AddSnake(int boardId, [FromBody] SnakeDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _boardService.AddSnakeAsync(boardId, userId, dto);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN") return Forbid();
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { snakeId = result.Id, message = result.Message });
        }

        [HttpDelete("{boardId}/snake/{snakeId}")]
        public async Task<IActionResult> RemoveSnake(int boardId, int snakeId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _boardService.RemoveSnakeAsync(boardId, snakeId, userId);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN") return Forbid();
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpPost("{boardId}/ladder")]
        public async Task<IActionResult> AddLadder(int boardId, [FromBody] LadderDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _boardService.AddLadderAsync(boardId, userId, dto);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN") return Forbid();
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { ladderId = result.Id, message = result.Message });
        }

        [HttpDelete("{boardId}/ladder/{ladderId}")]
        public async Task<IActionResult> RemoveLadder(int boardId, int ladderId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _boardService.RemoveLadderAsync(boardId, ladderId, userId);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN") return Forbid();
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpDelete("{boardId}/clear")]
        public async Task<IActionResult> ClearBoard(int boardId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _boardService.ClearBoardAsync(boardId, userId);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN") return Forbid();
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}