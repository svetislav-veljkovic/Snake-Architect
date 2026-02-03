using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;

namespace DAL.Repository
{
    public class FriendRequestRepository : Repository<FriendRequest>, IFriendRequestRepository
    {
        private readonly SnakeArchitectContext _db;
        public FriendRequestRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }
    }
}