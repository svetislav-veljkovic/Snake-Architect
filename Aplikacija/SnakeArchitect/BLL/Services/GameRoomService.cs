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
        private static readonly TimeSpan ReconnectGracePeriod = TimeSpan.FromMinutes(5);
        private readonly IUnitOfWork _uow;

        public GameRoomService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // FIX: centralna provera - korisnik (host ili igrac) ne sme biti
        // aktivan (isActive == true) u vise od jedne sobe istovremeno.
        // Koristi se i pri kreiranju sobe (host) i pri direktnom ulasku
        // (JoinRoomAsync), a analogna provera postoji i u
        // GameRequestService.AcceptGameRequestAsync za ulazak preko
        // pozivnice/zahteva.
        private Task<bool> IsUserActiveElsewhereAsync(int userId, int? excludeRoomId = null)
        {
            var roomIds = _uow.Player
                .Find(p => p.UserId == userId &&
                           p.IsConnected &&
                           (excludeRoomId == null || p.GameRoomId != excludeRoomId.Value))
                .Select(p => p.GameRoomId)
                .ToList();

            if (roomIds.Count == 0) return Task.FromResult(false);

            var isActiveElsewhere = _uow.GameRoom
                .Find(r => roomIds.Contains(r.ID) && r.isActive)
                .Any();

            return Task.FromResult(isActiveElsewhere);
        }

        // FIX: brise sve ostale neprihvacene GameRequest zapise (poslate i
        // primljene) datog korisnika, jer se posle ulaska u jednu sobu vise
        // ne moze pridruziti nijednoj drugoj dok je ova aktivna.
        private async Task CancelOtherPendingRequestsAsync(int userId, int keepRoomId)
        {
            var otherPending = _uow.GameRequest
                .Find(gr => !gr.Accepted &&
                            gr.GameRoomId != keepRoomId &&
                            (gr.SenderId == userId || gr.RecipientId == userId))
                .ToList();

            if (otherPending.Count == 0) return;

            foreach (var pending in otherPending)
                _uow.GameRequest.Delete(pending);

            await _uow.Save();
        }

        private static bool CanReconnect(Player player)
        {
            return player.IsConnected ||
                   !player.DisconnectedAt.HasValue ||
                   DateTime.UtcNow - player.DisconnectedAt.Value <= ReconnectGracePeriod;
        }

        private static DateTime PermanentLeaveTimestamp()
        {
            return DateTime.UtcNow - ReconnectGracePeriod - TimeSpan.FromSeconds(1);
        }

        private static IEnumerable<Player> VisiblePlayers(IEnumerable<Player> players)
        {
            return players.Where(CanReconnect);
        }

        public async Task<(bool Success, string Message, int RoomId)> CreateRoomAsync(string name, int hostUserId, int minPlayers = 2)
        {
            minPlayers = Math.Clamp(minPlayers, 2, 8);

            if (await IsUserActiveElsewhereAsync(hostUserId))
                return (false, "Vec imas aktivnu sobu. Zavrsi ili napusti trenutnu partiju pre kreiranja nove.", 0);

            var room = new GameRoom(name, true, DateTime.UtcNow, minPlayers);
            await _uow.GameRoom.Add(room);
            await _uow.Save();

            var player = new Player(hostUserId, room.ID, true);
            await _uow.Player.Add(player);
            await _uow.Save();

            await CancelOtherPendingRequestsAsync(hostUserId, room.ID);

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

        public Task<IEnumerable<object>> GetActiveRoomsAsync(int userId)
        {
            var reconnectCutoff = DateTime.UtcNow - ReconnectGracePeriod;
            var roomRows = _uow.GameRoom
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
                    BoardId = r.Board != null ? (int?)r.Board.ID : null
                })
                .ToList();

            var roomIds = roomRows.Select(r => r.ID).ToList();
            var boardIds = roomRows.Where(r => r.BoardId.HasValue).Select(r => r.BoardId!.Value).ToList();

            var playersByRoom = _uow.Player
                .Find(p => roomIds.Contains(p.GameRoomId))
                .ToList()
                .GroupBy(p => p.GameRoomId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var moveCountsByBoard = _uow.Move
                .Find(m => boardIds.Contains(m.GameBoardId))
                .ToList()
                .GroupBy(m => m.GameBoardId)
                .ToDictionary(g => g.Key, g => g.Count());

            var rooms = roomRows.Select(room =>
            {
                var allPlayers = playersByRoom.TryGetValue(room.ID, out var list) ? list : new List<Player>();
                var visiblePlayers = allPlayers
                    .Where(p => p.IsConnected ||
                                !p.DisconnectedAt.HasValue ||
                                p.DisconnectedAt.Value >= reconnectCutoff)
                    .OrderBy(p => p.ID)
                    .ToList();
                var myPlayer = allPlayers.FirstOrDefault(p => p.UserId == userId);

                var isMyTurn = false;
                if (room.IsStarted && room.BoardId.HasValue && visiblePlayers.Count > 0 && myPlayer != null)
                {
                    var movesCount = moveCountsByBoard.TryGetValue(room.BoardId.Value, out var count) ? count : 0;
                    var expectedPlayer = visiblePlayers[movesCount % visiblePlayers.Count];
                    isMyTurn = expectedPlayer.ID == myPlayer.ID;
                }

                return (object)new
                {
                    room.ID,
                    room.Name,
                    room.isActive,
                    room.IsStarted,
                    room.MinPlayers,
                    room.BoardConfirmed,
                    room.CreatedAd,
                    PlayerCount = visiblePlayers.Count,
                    HasBoard = room.BoardId.HasValue,
                    IsMember = myPlayer != null && CanReconnect(myPlayer),
                    MyPlayerIsConnected = myPlayer?.IsConnected,
                    IsMyTurn = isMyTurn
                };
            }).ToList();

            return Task.FromResult<IEnumerable<object>>(rooms);
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
                Players = VisiblePlayers(room.Players).Select(p => new
                {
                    p.ID,
                    p.UserId,
                    p.isHost,
                    p.CurrentPosition,
                    p.IsConnected,
                    p.DisconnectedAt
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
                    if (!CanReconnect(existing))
                        return (false, "Isteklo je vreme za povratak u partiju. Potreban je novi zahtev za pristup.", 0);

                    existing.IsConnected = true;
                    existing.DisconnectedAt = null;
                    _uow.Player.Update(existing);
                    await _uow.Save();
                    return (true, "Uspesno si se vratio/la u partiju.", existing.ID);
                }
                return (false, "Vec si u ovoj sobi.", existing.ID);
            }

            if (room.IsStarted)
                return (true, "Gledas partiju kao posmatrac.", -1);

            if (!room.BoardConfirmed)
                return (false, "Soba jos nije dostupna. Host prvo mora da potvrdi tablu.", 0);

            var currentPlayers = _uow.Player
                .Find(p => p.GameRoomId == roomId)
                .ToList();

            if (VisiblePlayers(currentPlayers).Count() >= room.MinPlayers)
                return (false, "Soba je popunjena.", 0);

            if (await IsUserActiveElsewhereAsync(userId))
                return (false, "Vec si aktivan u drugoj sobi. Napusti je pre pridruzivanja novoj.", 0);

            var player = new Player(userId, roomId, false);
            await _uow.Player.Add(player);
            await _uow.Save();

            await CancelOtherPendingRequestsAsync(userId, roomId);

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
                if (!CanReconnect(player))
                    return (false, "Isteklo je vreme za povratak u partiju. Potreban je novi zahtev za pristup.");

                player.IsConnected = true;
                player.DisconnectedAt = null;
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

            if (VisiblePlayers(room.Players).Count() != room.MinPlayers)
                return (false, $"Potrebno je tacno {room.MinPlayers} igraca za pocetak partije.");

            room.IsStarted = true;
            room.isActive = true;
            _uow.GameRoom.Update(room);
            await _uow.Save();

            return (true, "Partija je pokrenuta.");
        }

        // FIX (BUG REGRESIJA): prethodna verzija ove metode je UVEK trajno
        // brisala Player red kad korisnik klikne "Izadji", bez obzira da li
        // je partija u toku. To je slomilo povratak u partiju (i za hosta i
        // za obicnog igraca) - kad bi covek pokusao ponovo da udje, njegov
        // stari Player red vise nije postojao, pa ga je JoinRoomAsync
        // tretirao kao potpuno novog korisnika i, posto je partija vec
        // pokrenuta, vracao mu je samo ulogu posmatraca ("Gledas partiju
        // kao posmatrac."), a ne stvarni povratak sa sacuvanom pozicijom.
        //
        // Sada se razdvajaju dva slucaja:
        //   1) Partija JOS NIJE pocela (lobi/dizajn table faza) - "Izadji"
        //      i dalje TRAJNO uklanja igraca iz sobe (kao i ranije), host
        //      se po potrebi prebacuje na sledeceg igraca, a prazna soba
        //      se automatski brise.
        //   2) Partija JE u toku - "Izadji" sada samo markira igraca kao
        //      diskonektovanog (IsConnected = false), zadrzavajuci njegovu
        //      poziciju i host status. Time GameRoomPage.jsx (koji vec ima
        //      automatski poziv na /reconnect kad primeti isConnected ===
        //      false) i JoinRoomAsync mogu ispravno da ga vrate u partiju.
        //      Host i dalje moze eksplicitno da PREKINE/OBRISE celu sobu
        //      preko posebne akcije (DeleteRoomAsync / dugme "Prekini
        //      partiju" u Lobby.jsx), tako da se izbegava i stari problem
        //      sa "zaglavljenim" sobama.
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
                if (player.IsConnected)
                {
                    player.IsConnected = false;
                    player.DisconnectedAt = DateTime.UtcNow;
                    _uow.Player.Update(player);
                    await _uow.Save();
                }
                return (true, "Napustio/la si partiju. Mozes da se vratis dok je aktivna.");
            }

            bool wasHost = player.isHost;

            _uow.Player.Delete(player);
            await _uow.Save();

            if (room == null)
                return (true, "Napustio/la si sobu.");

            var remainingPlayers = _uow.Player
                .Find(p => p.GameRoomId == roomId)
                .ToList();

            if (remainingPlayers.Count == 0)
            {
                _uow.GameRoom.Delete(room);
                await _uow.Save();
                return (true, "Napustio/la si sobu. Soba je automatski obrisana jer je ostala bez igraca.");
            }

            if (wasHost)
            {
                var nextHost = remainingPlayers
                    .OrderBy(p => p.ID)
                    .First();

                nextHost.isHost = true;
                _uow.Player.Update(nextHost);
                await _uow.Save();
            }

            return (true, "Napustio/la si sobu.");
        }

        public async Task<(bool Success, string Message)> PermanentlyLeaveRoomAsync(int roomId, int userId)
        {
            var player = _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == roomId)
                .FirstOrDefault();

            if (player == null)
                return (false, "Nisi u ovoj sobi.");

            if (player.isHost)
                return (false, "Host ne napusta partiju ovim dugmetom. Koristi prekid partije.");

            GameRoom? room = null;
            try { room = await _uow.GameRoom.GetOne(roomId); } catch { }

            if (room == null)
                return (false, "Soba nije pronadjena.");

            if (!room.IsStarted)
                return await LeaveRoomAsync(roomId, userId);

            player.IsConnected = false;
            player.DisconnectedAt = PermanentLeaveTimestamp();
            _uow.Player.Update(player);
            await _uow.Save();

            return (true, "Trajno si napustio/la partiju. Host moze da primi zamenu ili prekine partiju.");
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

        public Task<bool> IsHostAsync(int roomId, int userId)
        {
            var isHost = _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == roomId && p.isHost)
                .Any();

            return Task.FromResult(isHost);
        }
    }
}
