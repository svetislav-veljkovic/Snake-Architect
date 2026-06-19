using DAL.DataContext;
using DAL.Models;
using DAL.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public class GameRoomRepository : Repository<GameRoom>, IGameRoomRepository
    {
        private readonly SnakeArchitectContext _db;
        public GameRoomRepository(SnakeArchitectContext db) : base(db)
        {
            _db = db;
        }

        public async Task<GameRoom?> GetRoomWithDetails(int id)
        {
            return await _db.GameRooms
                .Include(r => r.Players)
                .Include(r => r.Board)
                    .ThenInclude(b => b.Snakes)
                .Include(r => r.Board)
                    .ThenInclude(b => b.Ladders)
                .FirstOrDefaultAsync(r => r.ID == id);
        }
    }
}