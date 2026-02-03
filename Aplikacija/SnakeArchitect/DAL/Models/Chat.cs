using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    
    public class Chat
    {
        [Key]
        public int ID { get; set;}
        public int SenderId { get; set;}
        public virtual User? Sender {get; set;}
        public int RecipientId { get; set;}
        public virtual User? Recipient { get; set;}
        public string Content {get; set;}
        public DateTime SentAt { get; set;}

        public Chat(int senderId, int recipientId, string content, DateTime sentAt)
        {
            SenderId = senderId;
            RecipientId = recipientId;
            Content = content;
            SentAt = sentAt;
        }
    }
}