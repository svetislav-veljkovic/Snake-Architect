using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    
    public class Winner
    {
        [Key]
        public int ID { get; set;}
        [Required]
        public int PlayerId { get; set;}
        public Player? Player { get; set;}

        public Winner(int playerId)
        {
            PlayerId = playerId;
        }
    }
}