using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.IServices;
using DAL.DTOs;
using DAL.Models;
using DAL.UnitOfWork;

namespace BLL.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<object?> SendMessageAsync(ChatDTO dto, int senderId)
        {
            if (senderId != dto.SenderId)
                return new { error = "Forbid" };

            // FIX: dodata provera da korisnik ne moze da posalje poruku
            // samom sebi.
            if (dto.RecipientId == senderId)
                return new { error = "Ne mozes poslati poruku samom/samoj sebi." };

            try { await _unitOfWork.User.GetOne(dto.RecipientId); }
            catch { return null; }

            if (string.IsNullOrWhiteSpace(dto.Content))
                return new { error = "Poruka ne moze biti prazna." };

            var chat = new Chat(senderId, dto.RecipientId, dto.Content, DateTime.UtcNow);
            await _unitOfWork.Chat.Add(chat);
            await _unitOfWork.Save();

            return new
            {
                messageId = chat.ID,
                sentAt = chat.SentAt,
                message = "Poruka poslata."
            };
        }

        public async Task<List<object>> GetConversationAsync(int userId, int otherUserId, int page, int pageSize)
        {
            var messages = _unitOfWork.Chat
                .Find(c => (c.SenderId == userId && c.RecipientId == otherUserId) ||
                           (c.SenderId == otherUserId && c.RecipientId == userId))
                .OrderByDescending(c => c.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.ID,
                    c.SenderId,
                    c.RecipientId,
                    c.Content,
                    c.SentAt,
                    IsOwn = c.SenderId == userId
                })
                .Cast<object>()
                .ToList();

            messages.Reverse();
            return messages;
        }

        public async Task<List<object>> GetInboxAsync(int userId)
        {
            var messages = _unitOfWork.Chat
                .Find(c => c.SenderId == userId || c.RecipientId == userId)
                .ToList();

            // FIX: strogo tipizirana projekcija umesto (dynamic)c u
            // OrderByDescending. Stari kod je radio "slucajno" (dynamic cast
            // se kompajlira ali je krhak i sporiji) - ovo je ekvivalentno,
            // ali sigurno i brzo.
            var inbox = messages
                .GroupBy(c => c.SenderId == userId ? c.RecipientId : c.SenderId)
                .Select(g =>
                {
                    var lastMsg = g.OrderByDescending(m => m.SentAt).First();
                    return new
                    {
                        OtherUserId = g.Key,
                        LastMessage = lastMsg.Content,
                        LastMessageAt = lastMsg.SentAt,
                        IsLastMessageOwn = lastMsg.SenderId == userId
                    };
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();

            return inbox.Cast<object>().ToList();
        }

        public async Task<bool> DeleteMessageAsync(int messageId, int userId)
        {
            Chat message;
            try { message = await _unitOfWork.Chat.GetOne(messageId); }
            catch { return false; }

            if (message.SenderId != userId)
                return false;

            _unitOfWork.Chat.Delete(message);
            await _unitOfWork.Save();
            return true;
        }
    }
}
