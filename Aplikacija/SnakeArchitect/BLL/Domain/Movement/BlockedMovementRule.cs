using DAL.Models;
namespace BLL.Domain.Movement
{
    public class BlockedMovementRule : IMovementRule
    {
        public bool CanApply(GameBoard board, int fromPosition, int candidatePosition)
        {
            return candidatePosition > board.Rows * board.Columns;
        }
        public MovementResult Apply(GameBoard board, int fromPosition, int candidatePosition)
        {
            return new MovementResult
            {
                FinalPosition = fromPosition,
                MoveType = "blocked"
            };
        }
    }
}
