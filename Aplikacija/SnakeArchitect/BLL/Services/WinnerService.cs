using BLL.Services.IServices;
using DAL.Models;
using DAL.UnitOfWork;
using System;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class WinnerService : IWinnerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WinnerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CreateWinner(int playerId)
        {
            Player player;
            try { player = await _unitOfWork.Player.GetOne(playerId); }
            catch { throw new Exception("Igrač ne postoji."); }

            var user = await _unitOfWork.User.GetOne(player.UserId);
            user.GamesWon++;
            _unitOfWork.User.Update(user);

            var winner = new Winner(player.ID);
            await _unitOfWork.Winner.Add(winner);

            await _unitOfWork.Save();
        }
    }
}