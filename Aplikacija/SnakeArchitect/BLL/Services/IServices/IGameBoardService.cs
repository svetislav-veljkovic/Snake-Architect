using DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.IServices
{
    public interface IGameBoardService
    {
        Task<object?> GetBoardAsync(int boardId);
        Task<(bool Success, string Message, int Id)> AddSnakeAsync(int boardId, int hostUserId, SnakeDTO dto);
        Task<(bool Success, string Message)> RemoveSnakeAsync(int boardId, int snakeId, int hostUserId);
        Task<(bool Success, string Message, int Id)> AddLadderAsync(int boardId, int hostUserId, LadderDTO dto);
        Task<(bool Success, string Message)> RemoveLadderAsync(int boardId, int ladderId, int hostUserId);
        Task<(bool Success, string Message)> ClearBoardAsync(int boardId, int hostUserId);
    }
}
