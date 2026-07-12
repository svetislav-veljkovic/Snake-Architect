using DAL.Models;

namespace BLL.Domain.Movement
{
    public interface IMovementRule
    {
        bool CanApply(GameBoard board, int fromPosition, int candidatePosition);
        MovementResult Apply(GameBoard board, int fromPosition, int candidatePosition);
    }
}
