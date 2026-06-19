using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface IGameRequestService
    {
        Task<(bool Success, string Message, int RequestId)> SendGameRequestAsync(int senderId, int recipientId, int gameRoomId);
        Task<List<object>> GetIncomingRequestsAsync(int userId);
        Task<(bool Success, string Message, int PlayerId, int RoomId)> AcceptGameRequestAsync(int requestId, int userId);
        Task<(bool Success, string Message)> DeclineGameRequestAsync(int requestId, int userId);
    }
}