using DAL.Models;
using System.Linq;

namespace BLL.Domain.Movement
{
    public class SnakeMovementRule : IMovementRule
    {
        public bool CanApply(GameBoard board, int fromPosition, int candidatePosition)
        {
            return board.Snakes.Any(s => s.StarPosition == candidatePosition);
        }

        public MovementResult Apply(GameBoard board, int fromPosition, int candidatePosition)
        {
            var snake = board.Snakes.First(s => s.StarPosition == candidatePosition);
            return new MovementResult
            {
                FinalPosition = snake.EndPosition,
                MoveType = "snake"
            };
        }
    }
}
