using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;

namespace DAL.Repository
{
    public class GameRequestRepository : Repository<GameRequest>, IGameRequestRepository
    {
        private readonly SnakeArchitectContext _db;
        public GameRequestRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }
    }
}