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

        public async Task<(bool Success, string Message, int RoomId)> CreateRoomAsync(string name, int hostUserId, int minPlayers = 2)
        {
            minPlayers = Math.Clamp(minPlayers, 2, 8);

            var room = new GameRoom(name, true, DateTime.UtcNow, minPlayers);
            await _uow.GameRoom.Add(room);
            await _uow.Save();

            var player = new Player(hostUserId, room.ID, true);
            await _uow.Player.Add(player);
            await _uow.Save();

            return (true, "Soba kreirana uspesno. Sada podesi tablu.", room.ID);
        }

        public async Task<(bool Success, string Message, int BoardId)> CreateBoardAsync(int roomId, int hostUserId, int rows, int columns)
        {
            var room = await _uow.GameRoom.GetRoomWithDetails(roomId);
            if (room == null)
                return (false, "Soba nije pronadjena.", 0);

            if (!await IsHostAsync(roomId, hostUserId))
                return (false, "FORBIDDEN", 0);

            if (room.IsStarted)
                return (false, "Partija je vec pocela, tabla se vise ne moze podesiti.", 0);

            if (room.Board != null)
                return (false, "Tabla je vec podesena za ovu sobu.", 0);

            if (rows < 5 || rows > 15 || columns < 5 || columns > 15)
                return (false, "Dimenzije table moraju biti izmedju 5 i 15.", 0);

            var board = new GameBoard(rows, columns);
            room.Board = board;
            _uow.GameRoom.Update(room);
            await _uow.Save();

            return (true, "Tabla je podesena. Sada mozes postavljati zmije i merdevine.", board.ID);
        }

        // FIX: host potvrdjuje raspored zmija/merdevina. Tabla se posle ovoga
        // zakljucava za dalje izmene i tek tada se otkljucava lobi deo
        // (pozivnice, cekanje igraca, dugme za start partije).
        public async Task<(bool Success, string Message)> ConfirmBoardAsync(int roomId, int hostUserId)
        {
            var room = await _uow.GameRoom.GetRoomWithDetails(roomId);
            if (room == null)
                return (false, "Soba nije pronadjena.");

            if (!await IsHostAsync(roomId, hostUserId))
                return (false, "FORBIDDEN");

            if (room.Board == null)
                return (false, "Tabla nije podesena.");

            if (room.IsStarted)
                return (false, "Partija je vec pocela.");

            room.BoardConfirmed = true;
            _uow.GameRoom.Update(room);
            await _uow.Save();

            return (true, "Tabla je potvrdjena. Sada mozes pozivati igrace.");
        }

        // FIX: sad prima userId da bi mogao da vrati IsMember - bez toga
        // frontend nije znao da li je trenutni korisnik vec igrac partije
        // koja je u toku (pa treba da vidi "Udji" umesto "Gledaj").
        public async Task<IEnumerable<object>> GetActiveRoomsAsync(int userId)
        {
            return _uow.GameRoom
                .Find(r => r.isActive)
                .Select(r => new
                {
                    r.ID,
                    r.Name,
                    r.isActive,
                    r.IsStarted,
                    r.MinPlayers,
                    r.BoardConfirmed,
                    r.CreatedAd,
                    PlayerCount = r.Players.Count,
                    HasBoard = r.Board != null,
                    IsMember = r.Players.Any(p => p.UserId == userId)
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
                room.IsStarted,
                room.MinPlayers,
                room.BoardConfirmed,
                room.CreatedAd,
                Players = room.Players.Select(p => new
                {
                    p.ID,
                    p.UserId,
                    p.isHost,
                    p.CurrentPosition,
                    p.IsConnected
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
            catch { return (false, "Soba nije pronadjena.", 0); }

            if (!room.isActive)
                return (false, "Soba nije aktivna.", 0);

            var existing = _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == roomId)
                .FirstOrDefault();

            if (existing != null)
            {
                if (!existing.IsConnected)
                {
                    existing.IsConnected = true;
                    _uow.Player.Update(existing);
                    await _uow.Save();
                    return (true, "Uspesno si se vratio/la u partiju.", existing.ID);
                }
                return (false, "Vec si u ovoj sobi.", existing.ID);
            }

            if (room.IsStarted)
                return (true, "Gledas partiju kao posmatrac.", -1);

            var player = new Player(userId, roomId, false);
            await _uow.Player.Add(player);
            await _uow.Save();

            return (true, "Uspesno si se pridruzio/la sobi.", player.ID);
        }

        public async Task<(bool Success, string Message)> ReconnectAsync(int roomId, int userId)
        {
            var player = _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == roomId)
                .FirstOrDefault();

            if (player == null)
                return (false, "Nisi bio/bila igrac u ovoj sobi.");

            if (!player.IsConnected)
            {
                player.IsConnected = true;
                _uow.Player.Update(player);
                await _uow.Save();
            }

            return (true, "Rekonektovan/a si. Nastavljas partiju.");
        }

        public async Task<(bool Success, string Message)> StartRoomAsync(int roomId, int userId)
        {
            var room = await _uow.GameRoom.GetRoomWithDetails(roomId);
            if (room == null)
                return (false, "Soba nije pronadjena.");

            if (!await IsHostAsync(roomId, userId))
                return (false, "FORBIDDEN");

            if (room.IsStarted)
                return (false, "Partija je vec pokrenuta.");

            if (room.Board == null)
                return (false, "Tabla nije podesena.");

            if (!room.BoardConfirmed)
                return (false, "Prvo potvrdi tablu (dugme 'Kreiraj tablu').");

            if (room.Players.Count < room.MinPlayers)
                return (false, $"Potrebno je najmanje {room.MinPlayers} igraca za pocetak partije.");

            room.IsStarted = true;
            room.isActive = true;
            _uow.GameRoom.Update(room);
            await _uow.Save();

            return (true, "Partija je pokrenuta.");
        }

     public async Task<(bool Success, string Message)> LeaveRoomAsync(int roomId, int userId)
{
    var player = _uow.Player
        .Find(p => p.UserId == userId && p.GameRoomId == roomId)
        .FirstOrDefault();

    if (player == null)
        return (false, "Nisi u ovoj sobi.");

    GameRoom? room = null;
    try { room = await _uow.GameRoom.GetOne(roomId); } catch { }

    if (room != null && room.IsStarted)
    {
        player.IsConnected = false;
        _uow.Player.Update(player);
        await _uow.Save();
        return (true, "Napustio/la si sobu privremeno. Mozes se vratiti i nastaviti partiju.");
    }

    _uow.Player.Delete(player);

    if (player.isHost && room != null)
    {
    
        var nextHost = _uow.Player
            .Find(p => p.GameRoomId == roomId && p.ID != player.ID)
            .OrderBy(p => p.ID)
            .FirstOrDefault();

        if (nextHost != null)
        {
            nextHost.isHost = true;
            _uow.Player.Update(nextHost);
        }
        else
        {
            room.isActive = false;
            _uow.GameRoom.Update(room);
        }
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
            catch { return (false, "Soba nije pronadjena."); }
        }

        public async Task<bool> IsHostAsync(int roomId, int userId)
        {
            return _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == roomId && p.isHost)
                .Any();
        }
    }
}