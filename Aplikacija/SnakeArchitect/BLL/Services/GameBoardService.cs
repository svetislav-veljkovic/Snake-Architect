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
            var board = await _uow.GameBoard.GetBoardWithDetails(boardId);
            if (board == null) return null;

            return new
            {
                board.ID,
                board.Rows,
                board.Columns,
                Snakes = board.Snakes.Select(s => new { s.ID, s.StarPosition, s.EndPosition }),
                Ladders = board.Ladders.Select(l => new { l.ID, l.StartPosition, l.EndPosition })
            };
        }

        public async Task<(bool Success, string Message, int Id)> AddSnakeAsync(int boardId, int hostUserId, SnakeDTO dto)
        {
            var board = await _uow.GameBoard.GetBoardWithDetails(boardId);
            if (board == null)
                return (false, "Tabla nije pronaÄ‘ena.", 0);

            if (!IsHost(board, hostUserId))
                return (false, "FORBIDDEN", 0);

            if (board.GameRoom?.IsStarted == true)
                return (false, "Tabla se ne moÅ¾e menjati nakon poÄetka partije.", 0);

            if (dto.StarPosition <= dto.EndPosition)
                return (false, "Glava zmije mora biti na viÅ¡oj poziciji od repa.", 0);

            var maxPos = board.Rows * board.Columns;
            if (dto.StarPosition > maxPos || dto.EndPosition < 1)
                return (false, "Pozicija van granica table.", 0);

            if (board.Snakes.Any(s => s.StarPosition == dto.StarPosition))
                return (false, "Na toj poziciji veÄ‡ postoji glava zmije.", 0);

            if (board.Ladders.Any(l => l.StartPosition == dto.StarPosition))
                return (false, "Na toj poziciji veÄ‡ postoje merdevine.", 0);

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
            var board = await _uow.GameBoard.GetBoardWithDetails(boardId);
            if (board == null)
                return (false, "Tabla nije pronaÄ‘ena.");

            if (!IsHost(board, hostUserId))
                return (false, "FORBIDDEN");

            if (board.GameRoom?.IsStarted == true)
                return (false, "Tabla se ne moÅ¾e menjati nakon poÄetka partije.");

            try
            {
                var snake = await _uow.Snake.GetOne(snakeId);
                if (snake.GameBoardId != boardId)
                    return (false, "Zmija ne pripada ovoj tabli.");

                _uow.Snake.Delete(snake);
                await _uow.Save();
                return (true, "Zmija uklonjena.");
            }
            catch { return (false, "Zmija nije pronaÄ‘ena."); }
        }

        public async Task<(bool Success, string Message, int Id)> AddLadderAsync(int boardId, int hostUserId, LadderDTO dto)
        {
            var board = await _uow.GameBoard.GetBoardWithDetails(boardId);
            if (board == null)
                return (false, "Tabla nije pronaÄ‘ena.", 0);

            if (!IsHost(board, hostUserId))
                return (false, "FORBIDDEN", 0);

            if (board.GameRoom?.IsStarted == true)
                return (false, "Tabla se ne moÅ¾e menjati nakon poÄetka partije.", 0);

            if (dto.StartPosition >= dto.EndPosition)
                return (false, "Dno merdevina mora biti na niÅ¾oj poziciji od vrha.", 0);

            var maxPos = board.Rows * board.Columns;
            if (dto.EndPosition > maxPos || dto.StartPosition < 1)
                return (false, "Pozicija van granica table.", 0);

            if (board.Snakes.Any(s => s.StarPosition == dto.StartPosition))
                return (false, "Na toj poziciji veÄ‡ postoji zmija.", 0);

            if (board.Ladders.Any(l => l.StartPosition == dto.StartPosition))
                return (false, "Na toj poziciji veÄ‡ postoje merdevine.", 0);

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
            var board = await _uow.GameBoard.GetBoardWithDetails(boardId);
            if (board == null)
                return (false, "Tabla nije pronaÄ‘ena.");

            if (!IsHost(board, hostUserId))
                return (false, "FORBIDDEN");

            if (board.GameRoom?.IsStarted == true)
                return (false, "Tabla se ne moÅ¾e menjati nakon poÄetka partije.");

            try
            {
                var ladder = await _uow.Ladder.GetOne(ladderId);
                if (ladder.GameBoardId != boardId)
                    return (false, "Merdevine ne pripadaju ovoj tabli.");

                _uow.Ladder.Delete(ladder);
                await _uow.Save();
                return (true, "Merdevine uklonjene.");
            }
            catch { return (false, "Merdevine nisu pronaÄ‘ene."); }
        }

        public async Task<(bool Success, string Message)> ClearBoardAsync(int boardId, int hostUserId)
        {
            var board = await _uow.GameBoard.GetBoardWithDetails(boardId);
            if (board == null)
                return (false, "Tabla nije pronaÄ‘ena.");

            if (!IsHost(board, hostUserId))
                return (false, "FORBIDDEN");

            if (board.GameRoom?.IsStarted == true)
                return (false, "Tabla se ne moÅ¾e menjati nakon poÄetka partije.");

            var snakes = _uow.Snake.Find(s => s.GameBoardId == boardId).ToList();
            foreach (var s in snakes)
                _uow.Snake.Delete(s);

            var ladders = _uow.Ladder.Find(l => l.GameBoardId == boardId).ToList();
            foreach (var l in ladders)
                _uow.Ladder.Delete(l);

            await _uow.Save();
            return (true, "Tabla oÄiÅ¡Ä‡ena.");
        }

        private bool IsHost(GameBoard board, int userId)
        {
            if (board.GameRoom == null) return false;

            return _uow.Player
                .Find(p => p.UserId == userId && p.GameRoomId == board.GameRoom.ID && p.isHost)
                .Any();
        }
    }
}
