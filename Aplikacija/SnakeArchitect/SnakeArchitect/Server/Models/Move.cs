using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Move
    {
        [Key]
        public int ID { get; set;}
        
        [Required]
        public int PlayerId { get; set;}
        public virtual Player Player { get; set;}

        [Required]
        public int GameRoomId { get; set;}
        public virtual GameRoom GameRoom { get; set;}

        [Required]
        public int FromPosition { get; set;}

        [Required]
        public int ToPosition { get; set;}

        // Tip poteza: norman/snake/ladder
        public string MoveType { get; set;}

        public DateTime TimeStamp { get; set;}

        public Move(int playerId, int gameRoomId, int fromPos, int toPosition, string type)
        {
            PlayerId = playerId;
            GameRoomId = gameRoomId;
            FromPosition = fromPos;
            ToPosition = ToPosition;
            MoveType = type;

        }

    }
}