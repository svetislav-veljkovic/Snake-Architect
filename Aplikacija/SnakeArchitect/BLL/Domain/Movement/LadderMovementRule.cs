using DAL.Models;
using System.Linq;
namespace BLL.Domain.Movement
{
    public class LadderMovementRule : IMovementRule
    {
        public bool CanApply(GameBoard board, int fromPosition, int candidatePosition)
        {
            return board.Ladders.Any(l => l.StartPosition == candidatePosition);
        }
        public MovementResult Apply(GameBoard board, int fromPosition, int candidatePosition)
        {
            var ladder = board.Ladders.First(l => l.StartPosition == candidatePosition);
            return new MovementResult
            {
                FinalPosition = ladder.EndPosition,
                MoveType = "ladder"
            };
        }
    }
}
