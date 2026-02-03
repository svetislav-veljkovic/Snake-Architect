using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class GameBoard
    {
        [Key]
        public int ID { get; set;}
        [Required]
        public int Row { get; set;}
        [Required]
        public int Columns { get; set;}
        [NotMapped]
        public virtual ICollection<Snake> Snakes { get; set;}
        [NotMapped]
        public virtual ICollection<Ladder> Ladders { get; set;}

        public virtual GameRoom GameRoom { get; set;}


        public GameBoard() { }

       
        public GameBoard(int row, int columns)
        {
            Row = row;
            Columns = columns;
        }

    }
}