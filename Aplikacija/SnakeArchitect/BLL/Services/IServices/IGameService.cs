using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Services.IServices
{
    public class RollDiceResult
    {
        public int PlayerId { get; set; }
        public int DiceValue { get; set; }
        public int FromPosition { get; set; }
        public int ToPosition { get; set; }
        public string MoveType { get; set; } = string.Empty;
        public bool IsWinner { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public interface IGameService
    {
        Task<(bool Success, string Message, RollDiceResult? Result)> RollDiceAsync(int roomId, int userId);
        Task<object?> GetGameStateAsync(int roomId);
        Task<List<object>?> GetMovesAsync(int roomId);
        Task<object?> GetWinnerAsync(int roomId);
    }
}
