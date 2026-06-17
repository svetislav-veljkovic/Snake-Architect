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
        Task<(bool Success, string Message)> UpdateUserAsync(int id, int requestingUserId, UserDTO dto);
        Task<object?> GetStatsAsync(int id);
    }
}
