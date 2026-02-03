using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    
    public class Ladder
    {
        [Key]
        public int ID { get; set;}

        [Required]
        public int StartPosition { get; set;}

        [Required]
        public int EndPosition { get; set;}

    }
}