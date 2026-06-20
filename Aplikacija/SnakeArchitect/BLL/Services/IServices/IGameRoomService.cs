using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.IServices
{
    public interface IGameRoomService
    {
        Task<(bool Success, string Message, int RoomId, int BoardId)> CreateRoomAsync(string name, int rows, int columns, int hostUserId);
        Task<IEnumerable<object>> GetActiveRoomsAsync();
        Task<object?> GetRoomByIdAsync(int roomId);
        Task<(bool Success, string Message, int PlayerId)> JoinRoomAsync(int roomId, int userId);
        Task<(bool Success, string Message)> StartRoomAsync(int roomId, int userId);
        Task<(bool Success, string Message)> LeaveRoomAsync(int roomId, int userId);
        Task<(bool Success, string Message)> DeleteRoomAsync(int roomId, int userId);
        Task<bool> IsHostAsync(int roomId, int userId);
    }
}
