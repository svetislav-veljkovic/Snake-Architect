using BLL.IServices;
using DAL.UnitOfWork;
using DAL.Models;
using DAL.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class GameRequestService : IGameRequestService
    {
        private readonly IUnitOfWork _unitOfWork;

        public GameRequestService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> SendGameRequestAsync(int senderId, int receiverId)
        {
            // Provera da li već postoji aktivan/pending zahtev
            var exists = await _unitOfWork.GameRequestRepository.GetQueryable()
                .AnyAsync(r => r.SenderId == senderId && r.ReceiverId == receiverId && r.Status == "Pending");

            if (exists) return false;

            var request = new GameRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GameRequestRepository.AddAsync(request);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> RespondToGameRequestAsync(int requestId, bool accept)
        {
            var request = await _unitOfWork.GameRequestRepository.GetByIdAsync(requestId);
            if (request == null || request.Status != "Pending") return false;

            if (!accept)
            {
                request.Status = "Rejected";
                _unitOfWork.GameRequestRepository.Update(request);
                return await _unitOfWork.SaveChangesAsync() > 0;
            }

            request.Status = "Accepted";
            _unitOfWork.GameRequestRepository.Update(request);

            // 1. Kreiramo GameRoom za meč
            var room = new GameRoom
            {
                Name = $"Meč #{request.SenderId} vs #{request.ReceiverId}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.GameRoomRepository.AddAsync(room);
            await _unitOfWork.SaveChangesAsync(); // Generiše Room ID

            // 2. Kreiramo GameBoard za tu sobu
            var board = new GameBoard
            {
                GameRoomId = room.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.GameBoardRepository.AddAsync(board);
            await _unitOfWork.SaveChangesAsync();

            // 3. Ubacujemo oba igrača na početnu poziciju (polje 1)
            var player1 = new Player { UserId = request.SenderId, GameRoomId = room.Id, CurrentPosition = 1 };
            var player2 = new Player { UserId = request.ReceiverId, GameRoomId = room.Id, CurrentPosition = 1 };

            await _unitOfWork.PlayerRepository.AddAsync(player1);
            await _unitOfWork.PlayerRepository.AddAsync(player2);

            return await _unitOfWork.SaveChangesAsync() > 0;
        }
    }
}