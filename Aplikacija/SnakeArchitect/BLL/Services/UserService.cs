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
                return (false, "Korisnicko ime vec postoji.");

            if (_uow.User.Find(u => u.Email == dto.Email).Any())
                return (false, "Email vec postoji.");

            var user = new User(
                dto.Name,
                dto.LastName,
                dto.Username,
                dto.Email,
                BCrypt.Net.BCrypt.HashPassword(dto.Password),
                0, 0
            )
            {
                ProfilePicture = dto.ProfilePicture
            };

            await _uow.User.Add(user);
            await _uow.Save();
            return (true, "Registracija uspesna.");
        }

        public Task<(bool Success, string Token, int UserId, string Username)> LoginAsync(string username, string password)
        {
            var user = _uow.User.Find(u => u.Username == username).FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                return Task.FromResult((false, string.Empty, 0, string.Empty));

            var token = GenerateJwt(user);
            return Task.FromResult((true, token, user.ID, user.Username));
        }

        public async Task<object?> GetUserByIdAsync(int id)
        {
            try
            {
                var u = await _uow.User.GetOne(id);
                return new
                {
                    u.ID,
                    u.Name,
                    u.LastName,
                    u.Username,
                    u.Email,
                    u.ProfilePicture,
                    u.GamesWon,
                    u.GamesLost
                };
            }
            catch { return null; }
        }

        public Task<IEnumerable<object>> SearchUsersAsync(string username)
        {
            var users = _uow.User
                .Find(u => u.Username.Contains(username))
                .Select(u => new { u.ID, u.Username, u.Name, u.LastName, u.ProfilePicture, u.GamesWon, u.GamesLost })
                .ToList<object>();

            return Task.FromResult<IEnumerable<object>>(users);
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
                user.ProfilePicture = dto.ProfilePicture;

                // Napomena: menjanje lozinke ovde ostaje podrzano radi
                // kompatibilnosti, ali je preporuceno koristi namenski
                // endpoint /api/User/{id}/password koji zahteva trenutnu
                // lozinku (vidi ChangePasswordAsync ispod).
                if (!string.IsNullOrWhiteSpace(dto.Password))
                    user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                _uow.User.Update(user);
                await _uow.Save();
                return (true, "Profil azuriran.");
            }
            catch { return (false, "Korisnik nije pronadjen."); }
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int id, int requestingUserId, string currentPassword, string newPassword)
        {
            if (id != requestingUserId)
                return (false, "FORBIDDEN");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Nova lozinka mora imati najmanje 6 znakova.");

            try
            {
                var user = await _uow.User.GetOne(id);

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
                    return (false, "Trenutna lozinka nije tacna.");

                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _uow.User.Update(user);
                await _uow.Save();

                return (true, "Lozinka je uspesno promenjena.");
            }
            catch { return (false, "Korisnik nije pronadjen."); }
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

        // FIX: pomocna klasa za sortiranje istorije partija (izbegavamo
        // sortiranje po "dynamic" propertiju na anonimnom tipu).
        private class MatchHistoryEntry
        {
            public int RoomId { get; set; }
            public string RoomName { get; set; } = string.Empty;
            public DateTime PlayedAt { get; set; }
            public int WinnerUserId { get; set; }
            public string WinnerUsername { get; set; } = string.Empty;
            public bool IsWin { get; set; }
            public List<string> PlayerUsernames { get; set; } = new();
        }

        // FIX: istorija odigranih (zavrsenih) partija izmedju dva korisnika.
        // Trazimo sve sobe u kojima su OBA korisnika bila igraci, pa medju
        // njima uzimamo one koje imaju upisanog Winner-a (znaci da je
        // partija zavrsena), sortirano od najnovije ka najstarijoj.
        public async Task<List<object>> GetMatchHistoryAsync(int userId, int otherUserId, int limit = 5)
        {
            var myRoomIds = _uow.Player.Find(p => p.UserId == userId).Select(p => p.GameRoomId).Distinct().ToList();
            var otherRoomIds = _uow.Player.Find(p => p.UserId == otherUserId).Select(p => p.GameRoomId).Distinct().ToList();
            var commonRoomIds = myRoomIds.Intersect(otherRoomIds).ToList();

            if (commonRoomIds.Count == 0) return new List<object>();

            var playersInRooms = _uow.Player.Find(p => commonRoomIds.Contains(p.GameRoomId)).ToList();
            var playerIdToRoomId = playersInRooms.ToDictionary(p => p.ID, p => p.GameRoomId);
            var playerIds = playersInRooms.Select(p => p.ID).ToList();

            var winners = _uow.Winner.Find(w => playerIds.Contains(w.PlayerId)).ToList();
            var entries = new List<MatchHistoryEntry>();

            foreach (var winner in winners)
            {
                if (!playerIdToRoomId.TryGetValue(winner.PlayerId, out var roomId))
                    continue;

                GameRoom room;
                Player winnerPlayer;
                User winnerUser;

                try { room = await _uow.GameRoom.GetOne(roomId); }
                catch { continue; }

                try { winnerPlayer = await _uow.Player.GetOne(winner.PlayerId); }
                catch { continue; }

                try { winnerUser = await _uow.User.GetOne(winnerPlayer.UserId); }
                catch { continue; }

                entries.Add(new MatchHistoryEntry
                {
                    RoomId = roomId,
                    RoomName = room.Name,
                    PlayedAt = winner.WonAt,
                    WinnerUserId = winnerPlayer.UserId,
                    WinnerUsername = winnerUser.Username,
                    IsWin = winnerPlayer.UserId == userId
                });
            }

            return entries
                .OrderByDescending(e => e.PlayedAt)
                .Take(limit)
                .Select(e => (object)new
                {
                    e.RoomId,
                    e.RoomName,
                    e.PlayedAt,
                    e.WinnerUserId,
                    e.WinnerUsername,
                    e.IsWin
                })
                .ToList();
        }

        public async Task<List<object>> GetRecentMatchHistoryAsync(int userId, int limit = 5)
        {
            var myRoomIds = _uow.Player
                .Find(p => p.UserId == userId)
                .Select(p => p.GameRoomId)
                .Distinct()
                .ToList();

            if (myRoomIds.Count == 0) return new List<object>();

            var playersInRooms = _uow.Player
                .Find(p => myRoomIds.Contains(p.GameRoomId))
                .ToList();

            var playerIdToRoomId = playersInRooms.ToDictionary(p => p.ID, p => p.GameRoomId);
            var playerIds = playersInRooms.Select(p => p.ID).ToList();

            var winners = _uow.Winner
                .Find(w => playerIds.Contains(w.PlayerId))
                .OrderByDescending(w => w.WonAt)
                .Take(Math.Max(limit, 1))
                .ToList();

            if (winners.Count == 0) return new List<object>();

            var userIds = playersInRooms.Select(p => p.UserId).Distinct().ToList();
            var usersById = _uow.User
                .Find(u => userIds.Contains(u.ID))
                .ToDictionary(u => u.ID, u => u.Username);

            var entries = new List<MatchHistoryEntry>();

            foreach (var winner in winners)
            {
                if (!playerIdToRoomId.TryGetValue(winner.PlayerId, out var roomId))
                    continue;

                GameRoom room;
                Player winnerPlayer;

                try { room = await _uow.GameRoom.GetOne(roomId); }
                catch { continue; }

                try { winnerPlayer = await _uow.Player.GetOne(winner.PlayerId); }
                catch { continue; }

                var winnerUsername = usersById.TryGetValue(winnerPlayer.UserId, out var username)
                    ? username
                    : $"Korisnik {winnerPlayer.UserId}";

                var playerUsernames = playersInRooms
                    .Where(p => p.GameRoomId == roomId)
                    .OrderBy(p => p.ID)
                    .Select(p => usersById.TryGetValue(p.UserId, out var playerUsername)
                        ? playerUsername
                        : $"Korisnik {p.UserId}")
                    .Distinct()
                    .ToList();

                entries.Add(new MatchHistoryEntry
                {
                    RoomId = roomId,
                    RoomName = room.Name,
                    PlayedAt = winner.WonAt,
                    WinnerUserId = winnerPlayer.UserId,
                    WinnerUsername = winnerUsername,
                    IsWin = winnerPlayer.UserId == userId,
                    PlayerUsernames = playerUsernames
                });
            }

            return entries
                .OrderByDescending(e => e.PlayedAt)
                .Take(limit)
                .Select(e => (object)new
                {
                    e.RoomId,
                    e.RoomName,
                    e.PlayedAt,
                    e.WinnerUserId,
                    e.WinnerUsername,
                    e.IsWin,
                    e.PlayerUsernames
                })
                .ToList();
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
