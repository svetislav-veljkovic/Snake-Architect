using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface IGameRequestService
    {
        Task<bool> SendGameRequestAsync(int senderId, int receiverId);
        Task<bool> RespondToGameRequestAsync(int requestId, bool accept);
    }
}