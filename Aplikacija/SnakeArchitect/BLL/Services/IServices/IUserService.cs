using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.DTOs;
namespace BLL.Services.IServices
{
    public interface IUserService
    {
        Task<(bool Success, string Message)> RegisterAsync(UserDTO dto);
        Task<(bool Success, string Token, int UserId, string Username)> LoginAsync(string username, string password);
        Task<object?> GetUserByIdAsync(int id);
        Task<IEnumerable<object>> SearchUsersAsync(string username);
        Task<(bool Success, string Message, string Token, string Username)> UpdateUserAsync(int id, int requestingUserId, UserDTO dto);
        Task<object?> GetStatsAsync(int id);
        Task<(bool Success, string Message)> ChangePasswordAsync(int id, int requestingUserId, string currentPassword, string newPassword);
        Task<List<object>> GetMatchHistoryAsync(int userId, int otherUserId, int limit = 5);
        Task<List<object>> GetRecentMatchHistoryAsync(int userId, int limit = 5);
    }
}
