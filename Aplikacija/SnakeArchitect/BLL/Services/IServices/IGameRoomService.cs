using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.IServices
{
    public interface IGameRoomService
    {
        Task<(bool Success, string Message, int RoomId)> CreateRoomAsync(string name, int hostUserId, int minPlayers = 2);
        Task<(bool Success, string Message, int BoardId)> CreateBoardAsync(int roomId, int hostUserId, int rows, int columns);
        Task<(bool Success, string Message)> ConfirmBoardAsync(int roomId, int hostUserId);
        Task<IEnumerable<object>> GetActiveRoomsAsync(int userId);
        Task<object?> GetRoomByIdAsync(int roomId);
        Task<(bool Success, string Message, int PlayerId)> JoinRoomAsync(int roomId, int userId);
        Task<(bool Success, string Message)> ReconnectAsync(int roomId, int userId);
        Task<(bool Success, string Message)> StartRoomAsync(int roomId, int userId);
        Task<(bool Success, string Message)> LeaveRoomAsync(int roomId, int userId);
        Task<(bool Success, string Message)> DeleteRoomAsync(int roomId, int userId);
        Task<bool> IsHostAsync(int roomId, int userId);
    }
}