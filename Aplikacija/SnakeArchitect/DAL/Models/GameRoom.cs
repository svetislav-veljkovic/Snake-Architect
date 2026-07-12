using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace DAL.Models
{
    public class GameRoom
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<Player> Players { get; set; } = new List<Player>();
        public virtual GameBoard Board { get; set; } = null!;
        public bool isActive { get; set; }
        public bool IsStarted { get; set; }
        public int MinPlayers { get; set; } = 2;
        public bool BoardConfirmed { get; set; }
        public DateTime CreatedAd { get; set; }
        public GameRoom()
        {
        }
        public GameRoom(string name, bool isactive, DateTime createdAd, int minPlayers = 2)
        {
            Name = name;
            isActive = isactive;
            IsStarted = false;
            MinPlayers = minPlayers;
            BoardConfirmed = false;
            CreatedAd = createdAd;
        }
    }
}
