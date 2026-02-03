using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public class GameBoardRepository : Repository<GameBoard>, IGameBoardRepository
    {
        private readonly SnakeArchitectContext _db;
        public GameBoardRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }

       
    }
}