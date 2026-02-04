using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs
{
    public class LadderDTO
    {
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public int GameBoardId { get; set; }
    }
}
