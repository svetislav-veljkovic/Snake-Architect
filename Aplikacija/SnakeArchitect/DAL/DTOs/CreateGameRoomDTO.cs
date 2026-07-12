using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DAL.DTOs
{
    public class CreateGameRoomDTO
    {
        public string Name { get; set; } = string.Empty;
        public int MinPlayers { get; set; } = 2;
    }
}
