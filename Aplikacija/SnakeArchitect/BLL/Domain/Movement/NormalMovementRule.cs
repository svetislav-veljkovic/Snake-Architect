using DAL.Models;

namespace BLL.Domain.Movement
{
    public class NormalMovementRule : IMovementRule
    {
        public bool CanApply(GameBoard board, int fromPosition, int candidatePosition)
        {
            return true;
        }

        public MovementResult Apply(GameBoard board, int fromPosition, int candidatePosition)
        {
            return new MovementResult
            {
                FinalPosition = candidatePosition,
                MoveType = "normal"
            };
        }
    }
}
