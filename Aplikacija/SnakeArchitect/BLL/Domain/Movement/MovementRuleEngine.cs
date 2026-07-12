using DAL.Models;
using System.Collections.Generic;
using System.Linq;
namespace BLL.Domain.Movement
{
    public class MovementRuleEngine
    {
        private readonly IReadOnlyList<IMovementRule> _rules;
        public MovementRuleEngine()
        {
            _rules = new IMovementRule[]
            {
                new BlockedMovementRule(),
                new SnakeMovementRule(),
                new LadderMovementRule(),
                new NormalMovementRule()
            };
        }
        public MovementResult Resolve(GameBoard board, int fromPosition, int diceValue)
        {
            var candidatePosition = fromPosition + diceValue;
            var rule = _rules.First(r => r.CanApply(board, fromPosition, candidatePosition));
            return rule.Apply(board, fromPosition, candidatePosition);
        }
    }
}
