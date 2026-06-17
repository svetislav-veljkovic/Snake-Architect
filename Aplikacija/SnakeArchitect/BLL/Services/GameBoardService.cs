
using BLL.Services.IServices;
using DAL.DTOs;
using DAL.Models;
using DAL.UnitOfWork;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class GameBoardService : IGameBoardService
    {
        private readonly IUnitOfWork _uow;

        public GameBoardService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<object?> GetBoardAsync(int boardId)
        {
            try
            {
                var board = await _uow.GameBoard.GetOne(boardId);
                return new
                {
                    board.ID,
                    board.Rows,
                    board.Columns,
                    Snakes = board.Snakes.Select(s => new { s.ID, s.StarPosition, s.EndPosition }),
                    Ladders = board.Ladders.Select(l => new { l.ID, l.StartPosition, l.EndPosition })
                };
            }
            catch { return null; }
        }

        public async Task<(bool Success, string Message, int Id)> AddSnakeAsync(int boardId, int hostUserId, SnakeDTO dto)
        {
            GameBoard board;
            try { board = await _uow.GameBoard.GetOne(boardId); }
            catch { return (false, "Tabla nije pronađena.", 0); }

            if (!await IsHostOfBoard(boardId, hostUserId))
                return (false, "FORBIDDEN", 0);

            if (dto.StarPosition <= dto.EndPosition)
                return (false, "Glava zmije mora biti na višoj poziciji od repa.", 0);

            var maxPos = board.Rows * board.Columns;
            if (dto.StarPosition > maxPos || dto.EndPosition < 1)
                return (false, "Pozicija van granica table.", 0);

            if (board.Snakes.Any(s => s.StarPosition == dto.StarPosition))
                return (false, "Na toj poziciji već postoji glava zmije.", 0);

            if (board.Ladders.Any(l => l.StartPosition == dto.StarPosition))
                return (false, "Na toj poziciji već postoje merdevine.", 0);

            var snake = new Snake(dto.StarPosition, dto.EndPosition)
            {
                GameBoardId = boardId
            };
            await _uow.Snake.Add(snake);
            await _uow.Save();

            return (true, "Zmija dodana.", snake.ID);
        }

        public async Task<(bool Success, string Message)> RemoveSnakeAsync(int boardId, int snakeId, int hostUserId)
        {
            if (!await IsHostOfBoard(boardId, hostUserId))
                return (false, "FORBIDDEN");

            try
            {
                var snake = await _uow.Snake.GetOne(snakeId);
                if (snake.GameBoardId != boardId)
                    return (false, "Zmija ne pripada ovoj tabli.");

                _uow.Snake.Delete(snake);
                await _uow.Save();
                return (true, "Zmija uklonjena.");
            }
            catch { return (false, "Zmija nije pronađena."); }
        }

        public async Task<(bool Success, string Message, int Id)> AddLadderAsync(int boardId, int hostUserId, LadderDTO dto)
        {
            GameBoard board;
            try { board = await _uow.GameBoard.GetOne(boardId); }
            catch { return (false, "Tabla nije pronađena.", 0); }

            if (!await IsHostOfBoard(boardId, hostUserId))
                return (false, "FORBIDDEN", 0);

            if (dto.StartPosition >= dto.EndPosition)
                return (false, "Dno merdevina mora biti na nižoj poziciji od vrha.", 0);

            var maxPos = board.Rows * board.Columns;
            if (dto.EndPosition > maxPos || dto.StartPosition < 1)
                return (false, "Pozicija van granica table.", 0);

            if (board.Snakes.Any(s => s.StarPosition == dto.StartPosition))
                return (false, "Na toj poziciji već postoji zmija.", 0);

            if (board.Ladders.Any(l => l.StartPosition == dto.StartPosition))
                return (false, "Na toj poziciji već postoje merdevine.", 0);

            var ladder = new Ladder(dto.StartPosition, dto.EndPosition)
            {
                GameBoardId = boardId
            };
            await _uow.Ladder.Add(ladder);
            await _uow.Save();

            return (true, "Merdevine dodane.", ladder.ID);
        }

        public async Task<(bool Success, string Message)> RemoveLadderAsync(int boardId, int ladderId, int hostUserId)
        {
            if (!await IsHostOfBoard(boardId, hostUserId))
                return (false, "FORBIDDEN");

            try
            {
                var ladder = await _uow.Ladder.GetOne(ladderId);
                if (ladder.GameBoardId != boardId)
                    return (false, "Merdevine ne pripadaju ovoj tabli.");

                _uow.Ladder.Delete(ladder);
                await _uow.Save();
                return (true, "Merdevine uklonjene.");
            }
            catch { return (false, "Merdevine nisu pronađene."); }
        }

        public async Task<(bool Success, string Message)> ClearBoardAsync(int boardId, int hostUserId)
        {
            if (!await IsHostOfBoard(boardId, hostUserId))
                return (false, "FORBIDDEN");

            var snakes = _uow.Snake.Find(s => s.GameBoardId == boardId).ToList();
            foreach (var s in snakes)
                _uow.Snake.Delete(s);

            var ladders = _uow.Ladder.Find(l => l.GameBoardId == boardId).ToList();
            foreach (var l in ladders)
                _uow.Ladder.Delete(l);

            await _uow.Save();
            return (true, "Tabla očišćena.");
        }

        private async Task<bool> IsHostOfBoard(int boardId, int userId)
        {
            GameBoard board;
            try { board = await _uow.GameBoard.GetOne(boardId); }
            catch { return false; }

            return _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == board.GameRoom.ID && p.isHost)
                .Any();
        }
    }
}