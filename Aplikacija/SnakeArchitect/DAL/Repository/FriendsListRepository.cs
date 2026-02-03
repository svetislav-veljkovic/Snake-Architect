using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public class FriendsListRepository : Repository<FriendsList>, IFriendsListRepository
    {
        private readonly SnakeArchitectContext _db;

        public FriendsListRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }

       
    }
}