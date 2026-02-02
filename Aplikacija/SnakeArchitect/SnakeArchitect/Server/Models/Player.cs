using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Player
    {
        [Key]
        public int ID { get; set;}

        [Required]
        public int UserId { get; set;}
        public User? User { get; set;}
        [Required]
        public int GameRoomId { get; set;}
        public GameRoom? GameRoom { get; set;}
        public bool isHost { get; set;}

        public Player(int userId, int gameId, bool host = false)
        {
            UserId = userId;
            GameRoomId = gameId;
            isHost = host;
        }
    }
}