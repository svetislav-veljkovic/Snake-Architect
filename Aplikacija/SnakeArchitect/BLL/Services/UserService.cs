using BLL.Services.IServices;
using BLL.Services.IServices;
using DAL.DTOs;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;

        public UserService(IUnitOfWork uow, IConfiguration config)
        {
            _uow = uow;
            _config = config;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(UserDTO dto)
        {
            if (_uow.User.Find(u => u.Username == dto.Username).Any())
                return (false, "Korisničko ime već postoji.");

            if (_uow.User.Find(u => u.Email == dto.Email).Any())
                return (false, "Email već postoji.");

            var user = new User(
                dto.Name,
                dto.LastName,
                dto.Username,
                dto.Email,
                BCrypt.Net.BCrypt.HashPassword(dto.Password),
                0, 0
            );

            await _uow.User.Add(user);
            await _uow.Save();
            return (true, "Registracija uspešna.");
        }

        public async Task<(bool Success, string Token, int UserId, string Username)> LoginAsync(string username, string password)
        {
            var user = _uow.User.Find(u => u.Username == username).FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                return (false, string.Empty, 0, string.Empty);

            var token = GenerateJwt(user);
            return (true, token, user.ID, user.Username);
        }

        public async Task<object?> GetUserByIdAsync(int id)
        {
            try
            {
                var u = await _uow.User.GetOne(id);
                return new { u.ID, u.Name, u.LastName, u.Username, u.Email, u.GamesWon, u.GamesLost };
            }
            catch { return null; }
        }

        public async Task<IEnumerable<object>> SearchUsersAsync(string username)
        {
            return _uow.User
                .Find(u => u.Username.Contains(username))
                .Select(u => new { u.ID, u.Username, u.Name, u.LastName, u.GamesWon, u.GamesLost })
                .ToList<object>();
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(int id, int requestingUserId, UserDTO dto)
        {
            if (id != requestingUserId)
                return (false, "FORBIDDEN");

            try
            {
                var user = await _uow.User.GetOne(id);
                user.Name = dto.Name;
                user.LastName = dto.LastName;
                user.Email = dto.Email;

                if (!string.IsNullOrWhiteSpace(dto.Password))
                    user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                _uow.User.Update(user);
                await _uow.Save();
                return (true, "Profil ažuriran.");
            }
            catch { return (false, "Korisnik nije pronađen."); }
        }

        public async Task<object?> GetStatsAsync(int id)
        {
            try
            {
                var u = await _uow.User.GetOne(id);
                int total = u.GamesWon + u.GamesLost;
                double winRate = total > 0 ? Math.Round((double)u.GamesWon / total * 100, 2) : 0;
                return new { u.GamesWon, u.GamesLost, TotalGames = total, WinRate = winRate };
            }
            catch { return null; }
        }

        private string GenerateJwt(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}