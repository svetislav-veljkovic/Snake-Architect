using BLL.IServices;
using BLL.Services.IServices;
using DAL.DTOs;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeArchitectApi
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _messageService;
        private readonly IGameRequestService _gameRequestService;
        private readonly IFriendService _friendService;
        private readonly IUnitOfWork _unitOfWork;

        // FIX: prati koliko konekcija (tabova/uredjaja) trenutno ima svaki
        // korisnik. Kad broj prvi put predje sa 0 na 1, korisnik je "presao
        // online"; kad padne na 0, "presao offline". Svim klijentima se
        // salje obavestenje da azuriraju zelenu/sivu tackicu pored imena.
        private static readonly ConcurrentDictionary<int, int> _onlineUsers = new();

        public ChatHub(
            IChatService messageService,
            IGameRequestService gameRequestService,
            IFriendService friendService,
            IUnitOfWork unitOfWork)
        {
            _messageService = messageService;
            _gameRequestService = gameRequestService;
            _friendService = friendService;
            _unitOfWork = unitOfWork;
        }

        private int? GetUserId()
        {
            var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                var newCount = _onlineUsers.AddOrUpdate(userId.Value, 1, (_, count) => count + 1);
                if (newCount == 1)
                {
                    await Clients.All.SendAsync("UserStatusChanged", userId.Value, true);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                var newCount = _onlineUsers.AddOrUpdate(userId.Value, 0, (_, count) => count > 0 ? count - 1 : 0);
                if (newCount == 0)
                {
                    _onlineUsers.TryRemove(userId.Value, out _);
                    await Clients.All.SendAsync("UserStatusChanged", userId.Value, false);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // FIX: klijent poziva ovo odmah posle uspostavljanja konekcije da bi
        // dobio pocetni snapshot online korisnika (buduce promene stizu kroz
        // "UserStatusChanged" event).
        public Task<List<int>> GetOnlineUsers()
        {
            return Task.FromResult(_onlineUsers.Keys.ToList());
        }

        public async Task JoinGroup(string groupName)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                throw new HubException("Korisnik nije autentifikovan.");

            if (groupName == Context.User?.Identity?.Name)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                return;
            }

            if (groupName.StartsWith("game:", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(groupName.Substring("game:".Length), out var roomId))
            {
                var isRoomMember = _unitOfWork.Player
                    .Find(p => p.UserId == userId.Value && p.GameRoomId == roomId)
                    .Any();

                var canSpectateStartedRoom = _unitOfWork.GameRoom
                    .Find(r => r.ID == roomId && r.IsStarted && r.isActive)
                    .Any();

                if (isRoomMember || canSpectateStartedRoom)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                    return;
                }
            }

            throw new HubException("Nemate pravo pristupa ovoj SignalR grupi.");
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SendMessageToGroup(string groupName, string message)
        {
            if (string.IsNullOrWhiteSpace(groupName) || !groupName.StartsWith("game:", StringComparison.OrdinalIgnoreCase))
                throw new HubException("Grupne poruke su dozvoljene samo za game grupe.");

            var userId = GetUserId();
            if (!userId.HasValue)
                throw new HubException("Korisnik nije autentifikovan.");

            var roomIdText = groupName.Substring("game:".Length);
            if (!int.TryParse(roomIdText, out var roomId))
                throw new HubException("Neispravna game grupa.");

            var isRoomMember = _unitOfWork.Player
                .Find(p => p.UserId == userId.Value && p.GameRoomId == roomId)
                .Any();

            if (!isRoomMember)
                throw new HubException("Nemate pravo slanja poruke u ovu grupu.");

            await Clients.Group(groupName).SendAsync("ReceiveMessage", Context.User?.Identity?.Name, message);
        }

        public async Task SendMessageToUser(string recipientUsername, ChatDTO message)
        {
            var senderId = GetUserId();
            if (!senderId.HasValue)
                throw new HubException("Korisnik nije autentifikovan.");

            var result = await _messageService.SendMessageAsync(message, senderId.Value);
            if (result == null || result.GetType().GetProperty("error") != null)
                throw new HubException("Poruka nije poslata.");

            var recipient = await _unitOfWork.User.GetOne(message.RecipientId);
            await Clients.Group(recipient.Username).SendAsync("ReceiveMessage", message);
        }

        public async Task SendFriendRequest(int senderId, int recipientId, string recipientUsername)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || currentUserId.Value != senderId)
                throw new HubException("Ne mozete slati zahtev u ime drugog korisnika.");

            await _friendService.SendFriendRequestAsync(currentUserId.Value, recipientId);
            await Clients.Group(recipientUsername).SendAsync("FriendRequestSent", senderId);
        }

        public async Task AcceptFriendRequest(int requestId, int userId, string senderUsername)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || currentUserId.Value != userId)
                throw new HubException("Ne mozete prihvatiti zahtev u ime drugog korisnika.");

            await _friendService.AcceptRequestAsync(requestId, currentUserId.Value);
            await Clients.Group(senderUsername).SendAsync("FriendRequestAccepted", currentUserId.Value);
            await Clients.Caller.SendAsync("RefetchFriends");
            await Clients.Group(senderUsername).SendAsync("RefetchFriends");
        }

        public async Task DeclineFriendRequest(int requestId, int userId)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || currentUserId.Value != userId)
                throw new HubException("Ne mozete odbiti zahtev u ime drugog korisnika.");

            await _friendService.DeclineOrCancelRequestAsync(requestId, currentUserId.Value);
            await Clients.Caller.SendAsync("FetchFriendRequests");
        }

        public async Task SendGameInviteToUser(int senderId, int recipientId, int gameRoomId, string recipientUsername)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || currentUserId.Value != senderId)
                throw new HubException("Ne mozete slati pozivnicu u ime drugog korisnika.");

            var result = await _gameRequestService.SendGameRequestAsync(currentUserId.Value, recipientId, gameRoomId);
            if (result.Success)
            {
                await Clients.Group(recipientUsername).SendAsync("ReceiveGameInvite", currentUserId.Value, result.RequestId, gameRoomId);
            }
        }

        public async Task AcceptGameInviteToUser(int requestId, int userId, string senderUsername)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || currentUserId.Value != userId)
                throw new HubException("Ne mozete prihvatiti pozivnicu u ime drugog korisnika.");

            var result = await _gameRequestService.AcceptGameRequestAsync(requestId, currentUserId.Value);
            if (result.Success)
            {
                await Clients.Group(senderUsername).SendAsync("GameInviteAccepted", currentUserId.Value);
                await Clients.Caller.SendAsync("JoinedGameRoom", result.RoomId, result.PlayerId);
            }
        }

        public async Task DeclineGameInviteToUser(int requestId, int userId)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || currentUserId.Value != userId)
                throw new HubException("Ne mozete odbiti pozivnicu u ime drugog korisnika.");

            await _gameRequestService.DeclineGameRequestAsync(requestId, currentUserId.Value);
        }

        public Task StartGame(int gameRoomId)
        {
            throw new HubException("Igra se pokrece preko REST endpointa /api/GameRoom/{id}/start.");
        }

        public Task SendTurn(int gameRoomId, int playerId)
        {
            throw new HubException("Red igre odredjuje server nakon upisanog poteza.");
        }

        public Task SendMove(int gameRoomId, int playerId, int fromPosition, int toPosition, string moveType)
        {
            throw new HubException("Potezi se odigravaju preko REST endpointa /api/Game/roll/{roomId}.");
        }

        public Task SendWinner(int gameRoomId, int playerId)
        {
            throw new HubException("Pobednika proglasava server nakon validnog poteza.");
        }

        // FIX: brze emoji-reakcije tokom partije, bez otvaranja cata. Klijent
        // salje emoji + svoj userId; svi u grupi "game:{roomId}" (ukljucujuci
        // posiljaoca) dobijaju obavestenje i prikazuju kratku animaciju iznad
        // odgovarajuceg igraca na tabli. Reakcije se ne cuvaju u bazi - one
        // su prolazne, samo za trenutni utisak tokom partije.
        public async Task SendReaction(int gameRoomId, int userId, string emoji)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || currentUserId.Value != userId)
                throw new HubException("Ne mozete slati reakciju u ime drugog korisnika.");

            var isRoomMember = _unitOfWork.Player
                .Find(p => p.UserId == currentUserId.Value && p.GameRoomId == gameRoomId)
                .Any();

            if (!isRoomMember)
                throw new HubException("Samo igraci u sobi mogu slati reakcije.");

            if (string.IsNullOrWhiteSpace(emoji)) return;

            // Ogranicavamo duzinu da neko ne posalje ceo string kao "emoji".
            if (emoji.Length > 8) emoji = emoji.Substring(0, 8);

            await Clients.Group("game:" + gameRoomId).SendAsync("ReceiveReaction", currentUserId.Value, emoji);
        }
    }
}
