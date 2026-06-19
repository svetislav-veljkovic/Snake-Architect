using BLL.Services.IServices;
using DAL.Models;
using DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class GameRoomService : IGameRoomService
    {
        private readonly IUnitOfWork _uow;

        public GameRoomService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<(bool Success, string Message, int RoomId, int BoardId)> CreateRoomAsync(
            string name, int rows, int columns, int hostUserId)
        {
            var room = new GameRoom(name, true, DateTime.UtcNow);
            await _uow.GameRoom.Add(room);
            await _uow.Save();

            var board = new GameBoard(rows, columns);
            room.Board = board;
            _uow.GameRoom.Update(room);
            await _uow.Save();

            var player = new Player(hostUserId, room.ID, true);
            await _uow.Player.Add(player);
            await _uow.Save();

            return (true, "Soba kreirana uspješno.", room.ID, board.ID);
        }

        public async Task<IEnumerable<object>> GetActiveRoomsAsync()
        {
            return _uow.GameRoom
                .Find(r => r.isActive)
                .Select(r => new
                {
                    r.ID,
                    r.Name,
                    r.isActive,
                    r.CreatedAd,
                    PlayerCount = r.Players.Count
                })
                .ToList<object>();
        }

        public async Task<object?> GetRoomByIdAsync(int roomId)
        {
            var room = await _uow.GameRoom.GetRoomWithDetails(roomId);
            if (room == null) return null;

            return new
            {
                room.ID,
                room.Name,
                room.isActive,
                room.CreatedAd,
                Players = room.Players.Select(p => new
                {
                    p.ID,
                    p.UserId,
                    p.isHost,
                    p.CurrentPosition
                }),
                Board = room.Board == null ? null : new
                {
                    room.Board.ID,
                    room.Board.Rows,
                    room.Board.Columns,
                    Snakes = room.Board.Snakes.Select(s => new { s.ID, s.StarPosition, s.EndPosition }),
                    Ladders = room.Board.Ladders.Select(l => new { l.ID, l.StartPosition, l.EndPosition })
                }
            };
        }

        public async Task<(bool Success, string Message, int PlayerId)> JoinRoomAsync(int roomId, int userId)
        {
            GameRoom room;
            try { room = await _uow.GameRoom.GetOne(roomId); }
            catch { return (false, "Soba nije pronađena.", 0); }

            if (!room.isActive)
                return (false, "Soba nije aktivna.", 0);

            var alreadyIn = _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == roomId)
                .FirstOrDefault();

            if (alreadyIn != null)
                return (false, "Već si u ovoj sobi.", 0);

            var player = new Player(userId, roomId, false);
            await _uow.Player.Add(player);
            await _uow.Save();

            return (true, "Uspješno si se pridružio/la sobi.", player.ID);
        }

        public async Task<(bool Success, string Message)> LeaveRoomAsync(int roomId, int userId)
        {
            var player = _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == roomId)
                .FirstOrDefault();

            if (player == null)
                return (false, "Nisi u ovoj sobi.");

            _uow.Player.Delete(player);

            if (player.isHost)
            {
                try
                {
                    var room = await _uow.GameRoom.GetOne(roomId);
                    room.isActive = false;
                    _uow.GameRoom.Update(room);
                }
                catch {  }
            }

            await _uow.Save();
            return (true, "Napustio/la si sobu.");
        }

        public async Task<(bool Success, string Message)> DeleteRoomAsync(int roomId, int userId)
        {
            if (!await IsHostAsync(roomId, userId))
                return (false, "FORBIDDEN");

            try
            {
                var room = await _uow.GameRoom.GetOne(roomId);
                _uow.GameRoom.Delete(room);
                await _uow.Save();
                return (true, "Soba obrisana.");
            }
            catch { return (false, "Soba nije pronađena."); }
        }

        public async Task<bool> IsHostAsync(int roomId, int userId)
        {
            return _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == roomId && p.isHost)
                .Any();
        }
    }
}