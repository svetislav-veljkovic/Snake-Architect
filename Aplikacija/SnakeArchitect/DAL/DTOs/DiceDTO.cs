using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs
{
    public class DiceDTO
    {
        public int PlayerId { get; set; }
        public int GameBoardId { get; set; }
        public int Value { get; set; }
    }
}
