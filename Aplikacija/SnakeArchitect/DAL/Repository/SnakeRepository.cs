using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;

namespace DAL.Repository
{
    public class SnakeRepository : Repository<Snake>, ISnakeRepository
    {
        private readonly SnakeArchitectContext _db;

        public SnakeRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }
    }
}