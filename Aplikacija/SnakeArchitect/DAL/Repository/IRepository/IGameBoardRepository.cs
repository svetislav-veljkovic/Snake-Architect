using DAL.Models;
using System.Threading.Tasks;

namespace DAL.Repository.IRepository
{
    public interface IGameBoardRepository : IRepository<GameBoard>
    {

        Task<GameBoard?> GetBoardWithDetails(int id);
    }
}