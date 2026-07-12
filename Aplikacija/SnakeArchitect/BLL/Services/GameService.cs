using BLL.Domain.Movement;
using BLL.Services.IServices;
using DAL.Models;
using DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class GameService : IGameService
    {
        private static readonly TimeSpan ReconnectGracePeriod = TimeSpan.FromMinutes(5);
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWinnerService _winnerService;
        private readonly MovementRuleEngine _movementRuleEngine;

        public GameService(IUnitOfWork unitOfWork, IWinnerService winnerService)
        {
            _unitOfWork = unitOfWork;
            _winnerService = winnerService;
            _movementRuleEngine = new MovementRuleEngine();
        }

        public async Task<(bool Success, string Message, RollDiceResult? Result)> RollDiceAsync(int roomId, int userId)
        {
            var room = await _unitOfWork.GameRoom.GetRoomWithDetails(roomId);
            if (room == null)
                return (false, "Soba nije pronadjena.", null);

            var player = room.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null)
                return (false, "Nisi u ovoj sobi.", null);

            if (!player.IsConnected)
            {
                if (player.DisconnectedAt.HasValue &&
                    DateTime.UtcNow - player.DisconnectedAt.Value > ReconnectGracePeriod)
                    return (false, "Isteklo je vreme za povratak u partiju.", null);

                return (false, "Nisi povezan/a u partiju. Prvo se rekonektuj.", null);
            }

            if (!room.isActive)
                return (false, "Igra nije aktivna.", null);

            if (!room.IsStarted)
                return (false, "Igra jos nije pocela.", null);

            var board = room.Board;
            if (board == null)
                return (false, "Tabla nije kreirana.", null);

            var orderedPlayers = room.Players
                .Where(p => p.IsConnected ||
                            !p.DisconnectedAt.HasValue ||
                            DateTime.UtcNow - p.DisconnectedAt.Value <= ReconnectGracePeriod)
                .OrderBy(p => p.ID)
                .ToList();
            if (orderedPlayers.Count == 0)
                return (false, "U sobi nema igraca.", null);

            var movesSoFar = _unitOfWork.Move.Find(m => m.GameBoardId == board.ID).Count();
            var expectedPlayer = orderedPlayers[movesSoFar % orderedPlayers.Count];

            if (expectedPlayer.ID != player.ID)
                return (false, "Nije tvoj red.", null);

            var diceValue = Random.Shared.Next(1, 7);
            var dice = new Dice(player.ID, board.ID, diceValue, DateTime.UtcNow);
            await _unitOfWork.Dice.Add(dice);

            var fromPosition = player.CurrentPosition;
            var maxPosition = board.Rows * board.Columns;
            var movement = _movementRuleEngine.Resolve(board, fromPosition, diceValue);
            var newPosition = movement.FinalPosition;
            var moveType = movement.MoveType;

            var move = new Move(player.ID, board.ID, fromPosition, newPosition, moveType, DateTime.UtcNow);
            await _unitOfWork.Move.Add(move);

            player.CurrentPosition = newPosition;
            _unitOfWork.Player.Update(player);

            var isWinner = newPosition == maxPosition;
            if (isWinner)
            {
                await _winnerService.CreateWinner(player.ID);

                foreach (var loser in room.Players.Where(p => p.ID != player.ID))
                {
                    var loserUser = await _unitOfWork.User.GetOne(loser.UserId);
                    loserUser.GamesLost++;
                    _unitOfWork.User.Update(loserUser);
                }

                room.isActive = false;
                _unitOfWork.GameRoom.Update(room);
            }

            await _unitOfWork.Save();

            var result = new RollDiceResult
            {
                PlayerId = player.ID,
                DiceValue = diceValue,
                FromPosition = fromPosition,
                ToPosition = newPosition,
                MoveType = moveType,
                IsWinner = isWinner,
                Message = GetMoveMessage(moveType, diceValue, fromPosition, newPosition)
            };

            return (true, result.Message, result);
        }

        public async Task<object?> GetGameStateAsync(int roomId)
        {
            var room = await _unitOfWork.GameRoom.GetRoomWithDetails(roomId);
            if (room == null) return null;

            return new
            {
                room.ID,
                room.isActive,
                Players = room.Players
                    .Where(p => p.IsConnected ||
                                !p.DisconnectedAt.HasValue ||
                                DateTime.UtcNow - p.DisconnectedAt.Value <= ReconnectGracePeriod)
                    .Select(p => new
                {
                    p.ID,
                    p.UserId,
                    p.isHost,
                    p.CurrentPosition
                }),
                MaxPosition = room.Board != null ? room.Board.Rows * room.Board.Columns : 0
            };
        }

        public async Task<List<object>?> GetMovesAsync(int roomId)
        {
            var room = await _unitOfWork.GameRoom.GetRoomWithDetails(roomId);
            if (room == null) return null;
            if (room.Board == null) return new List<object>();

            return _unitOfWork.Move
                .Find(m => m.GameBoardId == room.Board.ID)
                .OrderByDescending(m => m.TimeStamp)
                .Select(m => (object)new
                {
                    m.ID,
                    m.PlayerId,
                    m.FromPosition,
                    m.ToPosition,
                    m.MoveType,
                    m.TimeStamp
                })
                .ToList();
        }

        public async Task<object?> GetWinnerAsync(int roomId)
        {
            var room = _unitOfWork.GameRoom.Find(r => r.ID == roomId).FirstOrDefault();
            if (room == null) return null;

            var playerIds = _unitOfWork.Player
                .Find(p => p.GameRoomId == roomId)
                .Select(p => p.ID)
                .ToList();

            var winner = _unitOfWork.Winner
                .Find(w => playerIds.Contains(w.PlayerId))
                .FirstOrDefault();

            if (winner == null)
                return new { hasWinner = false };

            var winnerPlayer = await _unitOfWork.Player.GetOne(winner.PlayerId);
            var winnerUser = await _unitOfWork.User.GetOne(winnerPlayer.UserId);

            return new
            {
                hasWinner = true,
                playerId = winner.PlayerId,
                userId = winnerPlayer.UserId,
                username = winnerUser.Username,
                wonAt = winner.WonAt
            };
        }

        private static string GetMoveMessage(string moveType, int diceValue, int from, int to)
        {
            return moveType switch
            {
                "snake" => $"Bacio/la si {diceValue}. Pao/pala si na zmiju! {from + diceValue} -> {to}",
                "ladder" => $"Bacio/la si {diceValue}. Pronasao/la si merdevine! {from + diceValue} -> {to}",
                "blocked" => $"Bacio/la si {diceValue}. Ne mozes ici dalje od kraja table.",
                _ => $"Bacio/la si {diceValue}. Pomeren/a sa {from} na {to}."
            };
        }
    }
}
