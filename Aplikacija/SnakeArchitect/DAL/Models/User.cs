using System.Collections.Generic;
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


        // FIX: kolekcije su ranije bile bez inicijalne vrednosti. Bez
        // UseLazyLoadingProxies() u Program.cs i bez eager Include-a, svaki
        // pristup ovim listama (npr. fr.Sender.SentMessages.Count) bi mogao
        // da baci NullReferenceException.
        [JsonIgnore]
        public virtual ICollection<Chat> SentMessages { get; set; } = new List<Chat>();
        [JsonIgnore]
        public virtual ICollection<Chat> ReceivedMessages { get; set; } = new List<Chat>();

        [JsonIgnore]
        public virtual ICollection<GameRequest> SentGameInvitations { get; set; } = new List<GameRequest>();
        [JsonIgnore]
        public virtual ICollection<GameRequest> ReceivedGameInvitations { get; set; } = new List<GameRequest>();
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }

        public User()
        {
        }
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
