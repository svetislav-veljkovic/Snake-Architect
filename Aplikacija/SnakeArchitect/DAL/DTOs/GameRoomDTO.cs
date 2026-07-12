using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DAL.DTOs
{
    public class GameRoomDTO
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
