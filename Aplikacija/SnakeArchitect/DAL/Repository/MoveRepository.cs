using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;

namespace DAL.Repository
{
    public class MoveRepository : Repository<Move>, IMoveRepository
    {
        private readonly SnakeArchitectContext _db;
        public MoveRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }
    }
}