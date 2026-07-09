using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Player
    {
        [Key]
        public int ID { get; set;}

        [Required]
        public int UserId { get; set;}
        public User? User { get; set;}
        [Required]
        public int GameRoomId { get; set;}
        public GameRoom? GameRoom { get; set;}
        public bool isHost { get; set;}

        public int CurrentPosition { get; set; }

        // FIX: umesto da se igrac obrise iz sobe kad izadje/diskonektuje se
        // usred partije, markiramo ga ovim fleg-om. Tako zadrzava poziciju,
        // ID i host status i moze da se vrati i nastavi partiju umesto da
        // bude sveden na posmatraca.
        public bool IsConnected { get; set; }

        public Player()
        {
        }
        public Player(int userId, int gameId, bool host = false)
        {
            UserId = userId;
            GameRoomId = gameId;
            isHost = host;
            CurrentPosition = 0;
            IsConnected = true;
        }
    }
}
