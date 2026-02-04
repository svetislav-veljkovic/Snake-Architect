using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs
{
    public class PlayerDTO
    {
        public int UserId { get; set; }
        public int GameRoomId { get; set; }
        public bool IsHost { get; set; }
    }
}
