using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Dice
    {
        [Key]
        public int ID { get; set;}

        [Required]
        public int PlayerId { get; set;}
        public virtual Player Player { get; set;}
        
        [Required]
        public int GameBoardId { get; set;}
        public GameBoard GameBoard { get; set;}

        [Required]
        [Range(1,6)]
        public int Value  { get; set;}

        public DateTime Timestamp { get; set;}
        public Dice()
        {
        }
        public Dice(int playerId, int gameBoardId, int value)
        {
            PlayerId = playerId;
            GameBoardId = gameBoardId;
            Value = value;
        }

    }
}