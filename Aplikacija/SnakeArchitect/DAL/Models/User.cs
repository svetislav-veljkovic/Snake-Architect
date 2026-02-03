using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DAL.Models
{
    public class User
    {
        [Key]
        public int ID { get; set;}
        [Required]
        public string Name { get; set;}
        [Required]
        public string LastName { get; set;}
        [Required]
        public string Username { get; set;}
        public string Email { get; set;}

        [JsonIgnore]
        [Required]
        public string Password { get; set;}
        
        [JsonIgnore]
        public virtual ICollection<FriendsList> SentFriendships { get; set; }
        [JsonIgnore]
        public virtual ICollection<FriendsList> ReceivedFriendships { get; set; }
        [JsonIgnore]
        public virtual ICollection<Chat> SentMessages { get; set; }
        [JsonIgnore]
        public virtual ICollection<Chat> ReceivedMessages { get; set; }
        [JsonIgnore]
        public virtual ICollection<FriendRequest> SentFriendRequests { get; set; }
        [JsonIgnore]
        public virtual ICollection<FriendRequest> ReceivedFriendRequest { get; set; }
        [JsonIgnore]
        public virtual ICollection<GameRequest> SentGameInvitations { get; set; }
        [JsonIgnore]
        public virtual ICollection<GameRequest> ReceivedGameInvitations { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }

        public User(string name, string lastName, string username, string email, string password, int gamesWon, int gamesLost)
        {
            Name = name;
            LastName = lastName;
            Username = username;
            Email = email;
            Password = password;
            GamesWon = gamesWon;
            GamesLost = gamesLost;
        }

        

    }
}