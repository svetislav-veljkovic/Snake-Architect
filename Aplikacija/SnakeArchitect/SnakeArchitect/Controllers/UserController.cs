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
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDTO dto)
        {
            var result = await _userService.RegisterAsync(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            return Ok(new { message = result.Message });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var result = await _userService.LoginAsync(dto.Username, dto.Password);
            if (!result.Success)
                return Unauthorized(new { message = "Pogresno korisnicko ime ili lozinka." });
            return Ok(new { token = result.Token, userId = result.UserId, username = result.Username });
        }
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Korisnik nije pronadjen." });
            return Ok(user);
        }
        [HttpGet("search/{username}")]
        [Authorize]
        public async Task<IActionResult> SearchUsers(string username)
        {
            var users = await _userService.SearchUsersAsync(username);
            return Ok(users);
        }
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDTO dto)
        {
            var requestingUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _userService.UpdateUserAsync(id, requestingUserId, dto);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN")
                    return Forbid();
                if (result.Message == "Korisnik nije pronadjen.")
                    return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message, token = result.Token, username = result.Username });
        }
        [HttpPut("{id}/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDTO dto)
        {
            var requestingUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _userService.ChangePasswordAsync(id, requestingUserId, dto.CurrentPassword, dto.NewPassword);
            if (!result.Success)
            {
                if (result.Message == "FORBIDDEN")
                    return Forbid();
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }
        [HttpGet("{id}/stats")]
        [Authorize]
        public async Task<IActionResult> GetStats(int id)
        {
            var stats = await _userService.GetStatsAsync(id);
            if (stats == null)
                return NotFound(new { message = "Korisnik nije pronadjen." });
            return Ok(stats);
        }
        [HttpGet("{id}/history")]
        [Authorize]
        public async Task<IActionResult> GetRecentMatchHistory(int id, [FromQuery] int limit = 5)
        {
            var requestingUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (id != requestingUserId)
                return Forbid();
            if (limit <= 0) limit = 5;
            if (limit > 50) limit = 50;
            var history = await _userService.GetRecentMatchHistoryAsync(id, limit);
            return Ok(history);
        }
        [HttpGet("{id}/history/{otherUserId}")]
        [Authorize]
        public async Task<IActionResult> GetMatchHistory(int id, int otherUserId, [FromQuery] int limit = 5)
        {
            var requestingUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (id != requestingUserId)
                return Forbid();
            if (limit <= 0) limit = 5;
            if (limit > 50) limit = 50;
            var history = await _userService.GetMatchHistoryAsync(id, otherUserId, limit);
            return Ok(history);
        }
    }
}
