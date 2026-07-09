using BLL.Services.IServices;
using DAL.Models;
using DAL.UnitOfWork;
using System;
using System.Linq;
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
            catch { throw new Exception("Igrac ne postoji."); }

            // FIX: idempotentna provera. Ranije je i GameController.RollDice
            // i ChatHub.SendWinner mogao da zavrsi pobedu za istu partiju
            // (klijent moze direktno da pozove SendWinner preko SignalR-a),
            // sto bi duplo uvecalo GamesWon. Sad se pobeda upisuje samo
            // jednom po sobi.
            var roomPlayerIds = _unitOfWork.Player
                .Find(p => p.GameRoomId == player.GameRoomId)
                .Select(p => p.ID)
                .ToList();

            var alreadyHasWinner = _unitOfWork.Winner
                .Find(w => roomPlayerIds.Contains(w.PlayerId))
                .Any();

            if (alreadyHasWinner)
                return;

            var user = await _unitOfWork.User.GetOne(player.UserId);
            user.GamesWon++;
            _unitOfWork.User.Update(user);

            var winner = new Winner(player.ID);
            await _unitOfWork.Winner.Add(winner);

            await _unitOfWork.Save();
        }
    }
}
