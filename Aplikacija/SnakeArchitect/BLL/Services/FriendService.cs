using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.IServices;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class FriendService : IFriendService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FriendService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<object?> SendFriendRequestAsync(int senderId, int recipientId)
        {
            if (senderId == recipientId)
                return new { error = "Ne možeš poslati zahtev samom/samoj sebi." };

            try { await _unitOfWork.User.GetOne(recipientId); }
            catch { return null; } 

            var existing = _unitOfWork.FriendRequest
                .Find(fr => fr.SenderId == senderId && fr.RecipientId == recipientId)
                .FirstOrDefault();

            if (existing != null)
                return new { error = "Zahtev već postoji." };

            var alreadyFriends = _unitOfWork.FriendsList
                .Find(fl => (fl.UserId == senderId && fl.FriendId == recipientId) ||
                            (fl.UserId == recipientId && fl.FriendId == senderId))
                .FirstOrDefault();

            if (alreadyFriends != null)
                return new { error = "Već ste prijatelji." };

            var request = new FriendRequest(senderId, recipientId, false);
            await _unitOfWork.FriendRequest.Add(request);
            await _unitOfWork.Save();

            return new { success = true, message = "Zahtev za prijateljstvo poslan." };
        }

        public async Task<List<object>> GetIncomingRequestsAsync(int userId)
        {
            return _unitOfWork.FriendRequest
                .Find(fr => fr.RecipientId == userId && !fr.Accepted)
                .Select(fr => (object)new
                {
                    fr.ID,
                    fr.SenderId,
                    SenderUsername = fr.Sender != null ? fr.Sender.Username : string.Empty
                })
                .ToList();
        }

        public async Task<List<object>> GetSentRequestsAsync(int userId)
        {
            return _unitOfWork.FriendRequest
                .Find(fr => fr.SenderId == userId && !fr.Accepted)
                .Select(fr => (object)new
                {
                    fr.ID,
                    fr.RecipientId,
                    RecipientUsername = fr.Recipient != null ? fr.Recipient.Username : string.Empty
                })
                .ToList();
        }

        public async Task<object?> AcceptRequestAsync(int requestId, int userId)
        {
            FriendRequest request;
            try { request = await _unitOfWork.FriendRequest.GetOne(requestId); }
            catch { return null; }

            if (request.RecipientId != userId)
                return new { error = "Forbid" };

            request.Accepted = true;
            _unitOfWork.FriendRequest.Update(request);

            var fl1 = new FriendsList(request.SenderId, request.RecipientId);
            var fl2 = new FriendsList(request.RecipientId, request.SenderId);

            await _unitOfWork.FriendsList.Add(fl1);
            await _unitOfWork.FriendsList.Add(fl2);
            await _unitOfWork.Save();

            return new { success = true, message = "Zahtev prihvaćen. Sada ste prijatelji." };
        }

        public async Task<bool> DeclineOrCancelRequestAsync(int requestId, int userId)
        {
            FriendRequest request;
            try { request = await _unitOfWork.FriendRequest.GetOne(requestId); }
            catch { return false; }

            if (request.RecipientId != userId && request.SenderId != userId)
                return false;

            _unitOfWork.FriendRequest.Delete(request);
            await _unitOfWork.Save();
            return true;
        }

        public async Task<List<object>> GetFriendsListAsync(int userId)
        {
            return _unitOfWork.FriendsList
                .Find(fl => fl.UserId == userId)
                .Select(fl => (object)new
                {
                    fl.FriendId,
                    FriendUsername = fl.Friend != null ? fl.Friend.Username : string.Empty,
                    FriendName = fl.Friend != null ? fl.Friend.Name : string.Empty,
                    GamesWon = fl.Friend != null ? fl.Friend.GamesWon : 0
                })
                .ToList();
        }

        public async Task<bool> RemoveFriendAsync(int userId, int friendId)
        {
            var fl1 = _unitOfWork.FriendsList.Find(fl => fl.UserId == userId && fl.FriendId == friendId).FirstOrDefault();
            var fl2 = _unitOfWork.FriendsList.Find(fl => fl.UserId == friendId && fl.FriendId == userId).FirstOrDefault();

            if (fl1 == null && fl2 == null) return false;

            if (fl1 != null) _unitOfWork.FriendsList.Delete(fl1);
            if (fl2 != null) _unitOfWork.FriendsList.Delete(fl2);


            var oldRequest = _unitOfWork.FriendRequest.Find(fr => (fr.SenderId == userId && fr.RecipientId == friendId) || (fr.SenderId == friendId && fr.RecipientId == userId)).FirstOrDefault();

            if(oldRequest != null)
            {
                _unitOfWork.FriendRequest.Delete(oldRequest);
            }

            await _unitOfWork.Save();
            return true;
        }
    }
}