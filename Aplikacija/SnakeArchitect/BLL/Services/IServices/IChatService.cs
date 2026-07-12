using System.Collections.Generic;
using System.Threading.Tasks;
using DAL.DTOs;
namespace BLL.IServices
{
    public interface IChatService
    {
        Task<object?> SendMessageAsync(ChatDTO dto, int senderId);
        Task<List<object>> GetConversationAsync(int userId, int otherUserId, int page, int pageSize);
        Task<List<object>> GetInboxAsync(int userId);
        Task<bool> DeleteMessageAsync(int messageId, int userId);
    }
}
