using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{

    public class FriendsList
    {
        [Key]
        public int ID { get; set; }
        public int UserId { get; set; }
        public virtual User? User { get; set; }

        public int FriendId { get; set; }
        public virtual User? Friend { get; set; }

        public FriendsList()
        {
        }
        public FriendsList(int userId, int friendId)
        {
            UserId = userId;
            FriendId = friendId;
        }
    }
    
}