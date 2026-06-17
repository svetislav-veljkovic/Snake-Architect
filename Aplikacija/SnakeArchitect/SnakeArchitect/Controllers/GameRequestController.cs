using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DAL.Models;
using DAL.UnitOfWork;

namespace SnakeArchitectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameRequestController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public GameRequestController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // POST api/gamerequest/send
        // Host šalje pozivnicu prijatelju za igru
        [HttpPost("send")]
        public async Task<IActionResult> SendGameRequest([FromBody] SendGameRequestDTO dto)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Provjeri da li je korisnik host u toj sobi
            var hostPlayer = _unitOfWork.Player
                .Find(p => p.UserId == senderId && p.GameRoomId == dto.GameRoomId && p.isHost)
                .FirstOrDefault();

            if (hostPlayer == null)
                return BadRequest(new { message = "Samo host može slati pozivnice." });

            // Provjeri da primatelj postoji
            try { await _unitOfWork.User.GetOne(dto.RecipientId); }
            catch { return NotFound(new { message = "Korisnik nije pronađen." }); }

            if (senderId == dto.RecipientId)
                return BadRequest(new { message = "Ne možeš pozvati samog/samu sebe." });

            // Provjeri da li već postoji pozivnica
            var existing = _unitOfWork.GameRequest
                .Find(gr => gr.SenderId == senderId &&
                            gr.RecipientId == dto.RecipientId &&
                            gr.GameRoomId == dto.GameRoomId &&
                            !gr.Accepted)
                .FirstOrDefault();

            if (existing != null)
                return BadRequest(new { message = "Pozivnica već poslana." });

            // Provjeri da primatelj već nije u sobi
            var alreadyIn = _unitOfWork.Player
                .Find(p => p.UserId == dto.RecipientId && p.GameRoomId == dto.GameRoomId)
                .FirstOrDefault();

            if (alreadyIn != null)
                return BadRequest(new { message = "Igrač je već u sobi." });

            var request = new GameRequest(senderId, dto.RecipientId, dto.GameRoomId, false);
            await _unitOfWork.GameRequest.Add(request);
            await _unitOfWork.Save();

            return Ok(new { requestId = request.ID, message = "Pozivnica poslana." });
        }

        // GET api/gamerequest/incoming
        // Lista primljenih pozivnica za igru
        [HttpGet("incoming")]
        public IActionResult GetIncomingRequests()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var requests = _unitOfWork.GameRequest
                .Find(gr => gr.RecipientId == userId && !gr.Accepted)
                .Select(gr => new
                {
                    gr.ID,
                    gr.GameRoomId,
                    gr.SenderId,
                    SenderUsername = gr.Sender != null ? gr.Sender.Username : string.Empty,
                    RoomName = gr.GameRoom != null ? gr.GameRoom.Name : string.Empty
                })
                .ToList();

            return Ok(requests);
        }

        // POST api/gamerequest/{requestId}/accept
        // Prihvati pozivnicu – automatski pridruži u sobu
        [HttpPost("{requestId}/accept")]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            GameRequest request;
            try { request = await _unitOfWork.GameRequest.GetOne(requestId); }
            catch { return NotFound(new { message = "Pozivnica nije pronađena." }); }

            if (request.RecipientId != userId)
                return Forbid();

            // Provjeri da li je soba još aktivna
            GameRoom room;
            try { room = await _unitOfWork.GameRoom.GetOne(request.GameRoomId); }
            catch { return NotFound(new { message = "Soba nije pronađena." }); }

            if (!room.isActive)
                return BadRequest(new { message = "Soba više nije aktivna." });

            // Provjeri da li je već u sobi
            var alreadyIn = _unitOfWork.Player
                .Find(p => p.UserId == userId && p.GameRoomId == request.GameRoomId)
                .FirstOrDefault();

            if (alreadyIn != null)
                return BadRequest(new { message = "Već si u ovoj sobi." });

            // Pridruži igrača sobi
            var player = new Player(userId, request.GameRoomId, false);
            await _unitOfWork.Player.Add(player);

            // Označi pozivnicu kao prihvaćenu
            request.Accepted = true;
            _unitOfWork.GameRequest.Update(request);

            await _unitOfWork.Save();

            return Ok(new
            {
                playerId = player.ID,
                roomId = request.GameRoomId,
                message = "Pozivnica prihvaćena. Pridružen/a si igri."
            });
        }

        // DELETE api/gamerequest/{requestId}
        // Odbij pozivnicu
        [HttpDelete("{requestId}")]
        public async Task<IActionResult> DeclineRequest(int requestId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            GameRequest request;
            try { request = await _unitOfWork.GameRequest.GetOne(requestId); }
            catch { return NotFound(new { message = "Pozivnica nije pronađena." }); }

            if (request.RecipientId != userId && request.SenderId != userId)
                return Forbid();

            _unitOfWork.GameRequest.Delete(request);
            await _unitOfWork.Save();

            return Ok(new { message = "Pozivnica odbijena." });
        }
    }

    public class SendGameRequestDTO
    {
        public int RecipientId { get; set; }
        public int GameRoomId { get; set; }
    }
}
