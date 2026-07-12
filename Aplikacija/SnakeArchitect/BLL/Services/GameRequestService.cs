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
        private static readonly System.TimeSpan ReconnectGracePeriod = System.TimeSpan.FromMinutes(5);
        private readonly IUnitOfWork _unitOfWork;

        public GameRequestService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // FIX: ista provera kao u GameRoomService - korisnik ne sme biti
        // aktivan (host ili igrac) u vise od jedne sobe istovremeno.
        private bool IsUserActiveElsewhere(int userId, int excludeRoomId)
        {
            var roomIds = _unitOfWork.Player
                .Find(p => p.UserId == userId && p.IsConnected && p.GameRoomId != excludeRoomId)
                .Select(p => p.GameRoomId)
                .ToList();

            if (roomIds.Count == 0) return false;

            return _unitOfWork.GameRoom
                .Find(r => roomIds.Contains(r.ID) && r.isActive)
                .Any();
        }

        private static bool CanReconnect(Player player)
        {
            return player.IsConnected ||
                   !player.DisconnectedAt.HasValue ||
                   System.DateTime.UtcNow - player.DisconnectedAt.Value <= ReconnectGracePeriod;
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

            GameRoom room;
            try { room = await _unitOfWork.GameRoom.GetOne(gameRoomId); }
            catch { return (false, "Soba nije pronađena.", 0); }

            if (!room.BoardConfirmed)
                return (false, "Soba još nije dostupna. Prvo potvrdi tablu.", 0);

            var currentPlayers = _unitOfWork.Player
                .Find(p => p.GameRoomId == gameRoomId)
                .ToList();

            if (currentPlayers.Count(CanReconnect) >= room.MinPlayers)
                return (false, "Soba je popunjena.", 0);

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

        // NAPOMENA: ovde svesno NEMA provere "vec si aktivan u drugoj sobi",
        // jer korisnik SME da posalje vise zahteva za ulazak u vise soba
        // istovremeno. Ogranicenje se primenjuje tek kod prihvatanja
        // (AcceptGameRequestAsync), kada se stvarno pridruzuje sobi.
        public async Task<(bool Success, string Message, int RequestId)> RequestJoinGameAsync(int senderId, int gameRoomId)
        {
            GameRoom room;
            try { room = await _unitOfWork.GameRoom.GetOne(gameRoomId); }
            catch { return (false, "Soba nije pronađena.", 0); }

            if (!room.isActive)
                return (false, "Soba nije aktivna.", 0);

            if (!room.BoardConfirmed)
                return (false, "Soba još nije dostupna. Host prvo mora da potvrdi tablu.", 0);

            var currentPlayers = _unitOfWork.Player
                .Find(p => p.GameRoomId == gameRoomId)
                .ToList();

            var replacementSlot = currentPlayers
                .Where(p => !p.isHost && !CanReconnect(p))
                .OrderBy(p => p.ID)
                .FirstOrDefault();

            if (room.IsStarted && replacementSlot == null)
                return (false, "Partija je već počela i nema slobodno mesto.", 0);

            if (currentPlayers.Count(CanReconnect) >= room.MinPlayers && replacementSlot == null)
                return (false, "Soba je popunjena.", 0);

            var alreadyIn = _unitOfWork.Player
                .Find(p => p.UserId == senderId && p.GameRoomId == gameRoomId)
                .FirstOrDefault();

            if (alreadyIn != null && CanReconnect(alreadyIn))
                return (false, "Već si u ovoj sobi.", 0);

            var host = _unitOfWork.Player
                .Find(p => p.GameRoomId == gameRoomId && p.isHost)
                .FirstOrDefault();

            if (host == null)
                return (false, "Host nije pronađen.", 0);

            var existing = _unitOfWork.GameRequest
                .Find(gr => gr.SenderId == senderId &&
                            gr.RecipientId == host.UserId &&
                            gr.GameRoomId == gameRoomId &&
                            !gr.Accepted)
                .FirstOrDefault();

            if (existing != null)
                return (false, "Zahtev za ulazak je već poslat.", 0);

            var request = new GameRequest(senderId, host.UserId, gameRoomId, false);
            await _unitOfWork.GameRequest.Add(request);
            await _unitOfWork.Save();

            return (true, "Zahtev za ulazak je poslat hostu.", request.ID);
        }

        public Task<List<object>> GetIncomingRequestsAsync(int userId)
        {
            var requests = _unitOfWork.GameRequest
                .Find(gr => gr.RecipientId == userId && !gr.Accepted)
                .Select(gr => (object)new
                {
                    gr.ID,
                    gr.GameRoomId,
                    gr.SenderId,
                    IsJoinRequest = gr.GameRoom != null && gr.GameRoom.Players.Any(p => p.UserId == userId && p.isHost),
                    SenderUsername = gr.Sender != null ? gr.Sender.Username : string.Empty,
                    RoomName = gr.GameRoom != null ? gr.GameRoom.Name : string.Empty
                })
                .ToList();

            return Task.FromResult(requests);
        }

        public async Task<(bool Success, string Message, int PlayerId, int RoomId, int JoinedUserId, string JoinedUsername)> AcceptGameRequestAsync(int requestId, int userId)
        {
            GameRequest request;
            try { request = await _unitOfWork.GameRequest.GetOne(requestId); }
            catch { return (false, "Pozivnica nije pronađena.", 0, 0, 0, string.Empty); }

            if (request.RecipientId != userId)
                return (false, "FORBIDDEN", 0, 0, 0, string.Empty);

            GameRoom room;
            try { room = await _unitOfWork.GameRoom.GetOne(request.GameRoomId); }
            catch { return (false, "Soba nije pronađena.", 0, 0, 0, string.Empty); }

            if (!room.isActive)
                return (false, "Soba više nije aktivna.", 0, 0, 0, string.Empty);

            if (!room.BoardConfirmed)
                return (false, "Soba još nije dostupna. Host prvo mora da potvrdi tablu.", 0, 0, 0, string.Empty);

            var userIsHost = _unitOfWork.Player
                .Find(p => p.UserId == userId && p.GameRoomId == request.GameRoomId && p.isHost)
                .Any();

            var joiningUserId = userIsHost ? request.SenderId : request.RecipientId;
            User joiningUser;
            try { joiningUser = await _unitOfWork.User.GetOne(joiningUserId); }
            catch { return (false, "Korisnik nije pronađen.", 0, 0, 0, string.Empty); }

            var alreadyIn = _unitOfWork.Player
                .Find(p => p.UserId == joiningUserId && p.GameRoomId == request.GameRoomId)
                .FirstOrDefault();

            if (alreadyIn != null && CanReconnect(alreadyIn))
                return (false, "Već si u ovoj sobi.", 0, 0, 0, string.Empty);

            var currentPlayers = _unitOfWork.Player
                .Find(p => p.GameRoomId == request.GameRoomId)
                .ToList();

            var replacementSlot = currentPlayers
                .Where(p => !p.isHost && !CanReconnect(p))
                .OrderBy(p => p.ID)
                .FirstOrDefault();

            if (currentPlayers.Count(CanReconnect) >= room.MinPlayers && replacementSlot == null)
                return (false, "Soba je popunjena.", 0, 0, 0, string.Empty);

            // FIX: ne dozvoljavamo da igrac udje u ovu sobu ako je vec
            // aktivan (host ili igrac) u nekoj drugoj aktivnoj sobi. Ovo je
            // "poslednja linija odbrane" - normalno bi vec trebalo da su mu
            // svi ostali zahtevi povuceni kad se prvi put pridruzio negde,
            // ali proveravamo eksplicitno za svaki slucaj (npr. race
            // condition sa dva brza prihvatanja).
            if (IsUserActiveElsewhere(joiningUserId, request.GameRoomId))
                return (false, "Igrač je već aktivan u drugoj sobi.", 0, 0, 0, string.Empty);

            Player player;
            if (room.IsStarted && replacementSlot != null)
            {
                replacementSlot.UserId = joiningUserId;
                replacementSlot.IsConnected = true;
                replacementSlot.DisconnectedAt = null;
                replacementSlot.isHost = false;
                _unitOfWork.Player.Update(replacementSlot);
                player = replacementSlot;
            }
            else
            {
                player = new Player(joiningUserId, request.GameRoomId, false);
                await _unitOfWork.Player.Add(player);
            }

            request.Accepted = true;
            _unitOfWork.GameRequest.Update(request);

            await _unitOfWork.Save();

            // FIX: kad se igrac pridruzi jednoj sobi, svi njegovi ostali
            // neprihvaceni zahtevi (i poslati zahtevi za ulazak u druge sobe,
            // i primljene pozivnice od drugih hostova) se automatski
            // povlace/brisu, jer ne moze da bude u dve sobe istovremeno.
            var otherPendingRequests = _unitOfWork.GameRequest
                .Find(gr => !gr.Accepted &&
                            gr.ID != request.ID &&
                            (gr.SenderId == joiningUserId || gr.RecipientId == joiningUserId))
                .ToList();

            if (otherPendingRequests.Count > 0)
            {
                foreach (var pending in otherPendingRequests)
                    _unitOfWork.GameRequest.Delete(pending);

                await _unitOfWork.Save();
            }

            return (true, "Pozivnica prihvaćena. Pridružen/a si igri.", player.ID, request.GameRoomId, joiningUserId, joiningUser.Username);
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
