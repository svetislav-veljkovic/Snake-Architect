using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DAL.Models
{
    public class Snake
    {
        [Key]
        public int ID { get; set; }
        [Required]
        [Column("StarPosition")]
        public int StarPosition { get; set; }
        [Required]
        [Column("EndPosition")]
        public int EndPosition { get; set; }
        public int GameBoardId { get; set; }
        public virtual GameBoard? GameBoard { get; set; }
        public Snake() { }
        public Snake(int startPosition, int endPosition)
        {
            StarPosition = startPosition;
            EndPosition = endPosition;
        }
    }
}
