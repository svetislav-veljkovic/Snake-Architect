using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DAL.Models
{
    public class GameBoard
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public int Rows { get; set; }
        [Required]
        public int Columns { get; set; }
        public virtual ICollection<Snake> Snakes { get; set; } = new List<Snake>();
        public virtual ICollection<Ladder> Ladders { get; set; } = new List<Ladder>();
        public virtual GameRoom GameRoom { get; set; } = null!;
        public GameBoard() { }
        public GameBoard(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
        }
    }
}
