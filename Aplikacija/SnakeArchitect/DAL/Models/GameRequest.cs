using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DAL.Models
{
    public class GameRequest
    {
        [Key]
        public int ID { get; set;}
        public int SenderId { get; set;}
        public virtual User Sender { get; set;}
        public int RecipientId  { get; set;}
        public virtual User Recipient { get; set;}
        public int GameRoomId { get; set;}
        public virtual GameRoom GameRoom { get; set;}
        public bool Accepted { get; set;}

        public GameRequest()
        {
        }
        public GameRequest(int senderId, int recipientId, int gameRoomId, bool accepted)
        {
            SenderId = senderId;
            RecipientId = recipientId;
            GameRoomId = gameRoomId;
            Accepted = accepted;
        }
        
    }
}