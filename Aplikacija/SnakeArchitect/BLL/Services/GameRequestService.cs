using BLL.IServices;
using DAL.Models;
using DAL.UnitOfWork;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class GameRequestService : IGameRequestService
    {
        private readonly IUnitOfWork _unitOfWork;

        public GameRequestService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool Success, string Message, int RequestId)> SendGameRequestAsync(int senderId, int recipientId, int gameRoomId)
        {
            var hostPlayer = _unitOfWork.Player
                .Find(p => p.UserId == senderId && p.GameRoomId == gameRoomId && p.isHost)
                .FirstOrDefault();

            if (hostPlayer == null)
                return (false, "Samo host može slati pozivnice.", 0);

            if (senderId == recipientId)
                return (false, "Ne možeš pozvati samog/samu sebe.", 0);

            try { await _unitOfWork.User.GetOne(recipientId); }
            catch { return (false, "Korisnik nije pronađen.", 0); }

            var existing = _unitOfWork.GameRequest
                .Find(gr => gr.SenderId == senderId &&
                            gr.RecipientId == recipientId &&
                            gr.GameRoomId == gameRoomId &&
                            !gr.Accepted)
                .FirstOrDefault();

            if (existing != null)
                return (false, "Pozivnica već poslana.", 0);

            var alreadyIn = _unitOfWork.Player
                .Find(p => p.UserId == recipientId && p.GameRoomId == gameRoomId)
                .FirstOrDefault();

            if (alreadyIn != null)
                return (false, "Igrač je već u sobi.", 0);

            var request = new GameRequest(senderId, recipientId, gameRoomId, false);
            await _unitOfWork.GameRequest.Add(request);
            await _unitOfWork.Save();

            return (true, "Pozivnica poslana.", request.ID);
        }

        public async Task<List<object>> GetIncomingRequestsAsync(int userId)
        {
            return _unitOfWork.GameRequest
                .Find(gr => gr.RecipientId == userId && !gr.Accepted)
                .Select(gr => (object)new
                {
                    gr.ID,
                    gr.GameRoomId,
                    gr.SenderId,
                    SenderUsername = gr.Sender != null ? gr.Sender.Username : string.Empty,
                    RoomName = gr.GameRoom != null ? gr.GameRoom.Name : string.Empty
                })
                .ToList();
        }

        public async Task<(bool Success, string Message, int PlayerId, int RoomId)> AcceptGameRequestAsync(int requestId, int userId)
        {
            GameRequest request;
            try { request = await _unitOfWork.GameRequest.GetOne(requestId); }
            catch { return (false, "Pozivnica nije pronađena.", 0, 0); }

            if (request.RecipientId != userId)
                return (false, "FORBIDDEN", 0, 0);

            GameRoom room;
            try { room = await _unitOfWork.GameRoom.GetOne(request.GameRoomId); }
            catch { return (false, "Soba nije pronađena.", 0, 0); }

            if (!room.isActive)
                return (false, "Soba više nije aktivna.", 0, 0);

            var alreadyIn = _unitOfWork.Player
                .Find(p => p.UserId == userId && p.GameRoomId == request.GameRoomId)
                .FirstOrDefault();

            if (alreadyIn != null)
                return (false, "Već si u ovoj sobi.", 0, 0);

            var player = new Player(userId, request.GameRoomId, false);
            await _unitOfWork.Player.Add(player);

            request.Accepted = true;
            _unitOfWork.GameRequest.Update(request);

            await _unitOfWork.Save();

            return (true, "Pozivnica prihvaćena. Pridružen/a si igri.", player.ID, request.GameRoomId);
        }

        public async Task<(bool Success, string Message)> DeclineGameRequestAsync(int requestId, int userId)
        {
            GameRequest request;
            try { request = await _unitOfWork.GameRequest.GetOne(requestId); }
            catch { return (false, "Pozivnica nije pronađena."); }

            if (request.RecipientId != userId && request.SenderId != userId)
                return (false, "FORBIDDEN");

            _unitOfWork.GameRequest.Delete(request);
            await _unitOfWork.Save();

            return (true, "Pozivnica odbijena.");
        }
    }
}