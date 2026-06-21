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

            try { await _unitOfWork.User.GetOne(dto.RecipientId); }
            catch { return null; } 

            if (string.IsNullOrWhiteSpace(dto.Content))
                return new { error = "Poruka ne može biti prazna." };

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

            return messages
                .GroupBy(c => c.SenderId == userId ? c.RecipientId : c.SenderId)
                .Select(g =>
                {
                    var lastMsg = g.OrderByDescending(m => m.SentAt).First();
                    return (object)new
                    {
                        OtherUserId = g.Key,
                        LastMessage = lastMsg.Content,
                        LastMessageAt = lastMsg.SentAt,
                        IsLastMessageOwn = lastMsg.SenderId == userId
                    };
                })
                .OrderByDescending(c => ((dynamic)c).LastMessageAt)
                .ToList();
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