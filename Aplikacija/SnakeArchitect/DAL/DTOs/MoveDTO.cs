using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs
{
    public class MoveDTO
    {
        public int PlayerId { get; set; }
        public int FromPosition { get; set; }
        public int ToPosition { get; set; }
        public string MoveType { get; set; }
    }
}
