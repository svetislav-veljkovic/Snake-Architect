using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    
    public class FriendRequest
    {
        [Key]
        public int ID { get; set;}
        public int SenderId { get; set;}
        public virtual User? Sender { get; set; }
        public int RecipientId { get; set;}
        public virtual User? Recipient { get; set;}
        public bool Accepted { get; set;}
        public FriendRequest(int senderId, int recipientId, bool accepted)
        {
            SenderId = senderId;
            RecipientId = recipientId;
            Accepted = accepted;
            
        }
    }
}