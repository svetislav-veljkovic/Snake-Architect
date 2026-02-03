using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;

namespace DAL.Repository
{
    public class WinnerRepository : Repository<Winner>, IWinnerRepository
    {
        private readonly SnakeArchitectContext _db;
        public WinnerRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }
    }
}