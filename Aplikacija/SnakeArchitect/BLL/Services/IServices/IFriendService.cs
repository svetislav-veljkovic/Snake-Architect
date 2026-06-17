using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface IFriendService
    {
        Task<object?> SendFriendRequestAsync(int senderId, int recipientId);
        Task<List<object>> GetIncomingRequestsAsync(int userId);
        Task<List<object>> GetSentRequestsAsync(int userId);
        Task<object?> AcceptRequestAsync(int requestId, int userId);
        Task<bool> DeclineOrCancelRequestAsync(int requestId, int userId);
        Task<List<object>> GetFriendsListAsync(int userId);
        Task<bool> RemoveFriendAsync(int userId, int friendId);
    }
}