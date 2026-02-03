using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly SnakeArchitectContext _db;

        public UserRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }

       
    }
}