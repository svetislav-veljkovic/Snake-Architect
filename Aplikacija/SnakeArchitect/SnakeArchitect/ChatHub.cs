using BLL.IServices;
using BLL.Services.IServices;
using DAL.DTOs;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SnakeArchitectApi
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _messageService;
        private readonly IUserService _userService;
        private readonly IWinnerService _winnerService;
        private readonly IGameRequestService _gameRequestService;
        private readonly IFriendService _friendService;
        private readonly IUnitOfWork _unitOfWork;

        public ChatHub(
            IChatService messageService,
            IUserService userService,
            IWinnerService winnerService,
            IGameRequestService gameRequestService,
            IFriendService friendService,
            IUnitOfWork unitOfWork)
        {
            _messageService = messageService;
            _userService = userService;
            _winnerService = winnerService;
            _gameRequestService = gameRequestService;
            _friendService = friendService;
            _unitOfWork = unitOfWork;
        }

   
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SendMessageToGroup(string groupName, string message)
        {
            await Clients.Group(groupName).SendAsync("ReceiveMessage", Context.User?.Identity?.Name, message);
        }

   
        public async Task SendMessageToUser(string recipientUsername, ChatDTO message)
        {
            await _messageService.SendMessageAsync(message, message.SenderId);
            await Clients.Group(recipientUsername).SendAsync("ReceiveMessage", message);
        }

       
        public async Task SendFriendRequest(int senderId, int recipientId, string recipientUsername)
        {
            await _friendService.SendFriendRequestAsync(senderId, recipientId);
            await Clients.Group(recipientUsername).SendAsync("FriendRequestSent", senderId);
        }

        public async Task AcceptFriendRequest(int requestId, int userId, string senderUsername)
        {
            await _friendService.AcceptRequestAsync(requestId, userId);
            await Clients.Group(senderUsername).SendAsync("FriendRequestAccepted", userId);
            await Clients.Caller.SendAsync("RefetchFriends");
            await Clients.Group(senderUsername).SendAsync("RefetchFriends");
        }

        public async Task DeclineFriendRequest(int requestId, int userId)
        {
            await _friendService.DeclineOrCancelRequestAsync(requestId, userId);
            await Clients.Caller.SendAsync("FetchFriendRequests");
        }

  
        public async Task SendGameInviteToUser(int senderId, int recipientId, int gameRoomId, string recipientUsername)
        {
            var result = await _gameRequestService.SendGameRequestAsync(senderId, recipientId, gameRoomId);
            if (result.Success)
            {
                await Clients.Group(recipientUsername).SendAsync("ReceiveGameInvite", senderId, result.RequestId, gameRoomId);
            }
        }

        public async Task AcceptGameInviteToUser(int requestId, int userId, string senderUsername)
        {
            var result = await _gameRequestService.AcceptGameRequestAsync(requestId, userId);
            if (result.Success)
            {
                await Clients.Group(senderUsername).SendAsync("GameInviteAccepted", userId);
                await Clients.Caller.SendAsync("JoinedGameRoom", result.RoomId, result.PlayerId);
            }
        }

        public async Task DeclineGameInviteToUser(int requestId, int userId)
        {
            await _gameRequestService.DeclineGameRequestAsync(requestId, userId);
        }

    
        public async Task StartGame(int gameRoomId)
        {
            await Clients.Group("game:" + gameRoomId).SendAsync("GameStarted");
        }

        public async Task SendTurn(int gameRoomId, int playerId)
        {
            await Clients.Group("game:" + gameRoomId).SendAsync("ReceiveTurn", playerId);
        }

    
        public async Task SendMove(int gameRoomId, int playerId, int fromPosition, int toPosition, string moveType)
        {
            await Clients.Group("game:" + gameRoomId).SendAsync("ReceiveMove", playerId, fromPosition, toPosition, moveType);
        }

        public async Task SendWinner(int gameRoomId, int playerId)
        {
            await _winnerService.CreateWinner(playerId);
            await Clients.Group("game:" + gameRoomId).SendAsync("ReceiveWinner", playerId);
        }
    }
}