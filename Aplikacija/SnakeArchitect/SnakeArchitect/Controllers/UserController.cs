
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
                return Unauthorized(new { message = "Pogrešno korisničko ime ili lozinka." });

            return Ok(new { token = result.Token, userId = result.UserId, username = result.Username });
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Korisnik nije pronađen." });

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

                return NotFound(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpGet("{id}/stats")]
        [Authorize]
        public async Task<IActionResult> GetStats(int id)
        {
            var stats = await _userService.GetStatsAsync(id);
            if (stats == null)
                return NotFound(new { message = "Korisnik nije pronađen." });

            return Ok(stats);
        }
    }
}