using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using BLL.IServices;

namespace SnakeArchitectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendController : ControllerBase
    {
        private readonly IFriendService _friendService;

        public FriendController(IFriendService friendService)
        {
            _friendService = friendService;
        }

        [HttpPost("request/{recipientId}")]
        public async Task<IActionResult> SendFriendRequest(int recipientId)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _friendService.SendFriendRequestAsync(senderId, recipientId);

            if (result == null)
                return NotFound(new { message = "Korisnik nije pronađen." });

            var errorProp = result.GetType().GetProperty("error");
            if (errorProp != null)
                return BadRequest(new { message = errorProp.GetValue(result)?.ToString() });

            return Ok(new { message = "Zahtev za prijateljstvo poslan." });
        }

        [HttpGet("requests")]
        public IActionResult GetIncomingRequests()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(_friendService.GetIncomingRequestsAsync(userId).Result);
        }

        [HttpGet("requests/sent")]
        public IActionResult GetSentRequests()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(_friendService.GetSentRequestsAsync(userId).Result);
        }

        [HttpPost("request/{requestId}/accept")]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _friendService.AcceptRequestAsync(requestId, userId);

            if (result == null)
                return NotFound(new { message = "Zahtjev nije pronađen." });

            var errorProp = result.GetType().GetProperty("error");
            if (errorProp != null && errorProp.GetValue(result)?.ToString() == "Forbid")
                return Forbid();

            return Ok(new { message = "Zahtev prihvaćen. Sada ste prijatelji." });
        }

        [HttpDelete("request/{requestId}")]
        public async Task<IActionResult> DeclineOrCancelRequest(int requestId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _friendService.DeclineOrCancelRequestAsync(requestId, userId);

            if (!success)
                return NotFound(new { message = "Zahtev nije pronađen ili nemate autorizaciju." });

            return Ok(new { message = "Zahtev odbijen/povučen." });
        }

        [HttpGet("list")]
        public IActionResult GetFriends()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(_friendService.GetFriendsListAsync(userId).Result);
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await _friendService.RemoveFriendAsync(userId, friendId);

            if (!success)
                return NotFound(new { message = "Niste prijatelji." });

            return Ok(new { message = "Prijatelј uklonjen." });
        }
    }
}