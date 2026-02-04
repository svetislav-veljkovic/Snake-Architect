using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{

    public class Move
    {
        [Key]
        public int ID { get; set;}
        
        [Required]
        public int PlayerId { get; set;}
        public virtual Player Player { get; set;}

        [Required]
        public int GameBoardId { get; set;}
        public virtual GameBoard GameBoard { get; set;}

        [Required]
        public int FromPosition { get; set;}

        [Required]
        public int ToPosition { get; set;}

        
        public string MoveType { get; set;}

        public DateTime TimeStamp { get; set;}
        public Move()
        {
        }
        public Move(int playerId, int gameBoardId, int fromPos, int toPos, string type,DateTime timestamp)
        {
            PlayerId = playerId;
            GameBoardId = gameBoardId;
            FromPosition = fromPos;
            ToPosition = toPos;
            MoveType = type;
            TimeStamp= timestamp;

        }

    }
}