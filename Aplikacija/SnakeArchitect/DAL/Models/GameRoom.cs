using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DAL.Models
{
    public class GameRoom
    {
        [Key]
        public int ID { get; set;}

        [Required]
        public string Name { get; set;}
        public virtual ICollection<Player> Players { get; set; } = new List<Player>();

        public virtual GameBoard Board { get; set;}
        public bool isActive { get; set;}

        public DateTime CreatedAd { get; set;}


        public GameRoom()
        {
            
        }

        public GameRoom(string name,bool isactive,DateTime createdAd) 
        {
            Name = name;
            isActive = isactive;
            CreatedAd= createdAd;
        }

    }
}