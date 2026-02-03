using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;

namespace DAL.Repository
{
    public class LadderRepository : Repository<Ladder>, ILadderRepository
    {
        private readonly SnakeArchitectContext _db;
        public LadderRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }
    }
}