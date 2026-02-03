using Microsoft.EntityFrameworkCore;
using DAL.Models; 

namespace DAL.DataContext
{
    public class SnakeArchitectContext : DbContext
    {
        public SnakeArchitectContext(DbContextOptions<SnakeArchitectContext> options)
            : base(options)
        {
        }

        
        public DbSet<User> Users { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<GameRoom> GameRooms { get; set; }
        public DbSet<GameBoard> GameBoards { get; set; }
        public DbSet<Snake> Snakes { get; set; }
        public DbSet<Ladder> Ladders { get; set; }
        public DbSet<Move> Moves { get; set; }
        public DbSet<Dice> Dices { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<FriendsList> FriendsLists { get; set; }
        public DbSet<GameRequest> GameRequests { get; set; }
        public DbSet<Winner> Winners { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

           
            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Recipient)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(c => c.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

           
            modelBuilder.Entity<FriendRequest>()
                .HasOne(f => f.Sender)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(f => f.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(f => f.Recipient)
                .WithMany(u => u.ReceivedFriendRequest)
                .HasForeignKey(f => f.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

           
            modelBuilder.Entity<FriendsList>()
                .HasOne(fl => fl.User)
                .WithMany(u => u.SentFriendships)
                .HasForeignKey(fl => fl.UserId);

            modelBuilder.Entity<FriendsList>()
                .HasOne(fl => fl.Friend)
                .WithMany(u => u.ReceivedFriendships)
                .HasForeignKey(fl => fl.FriendId);

           
            modelBuilder.Entity<GameRequest>()
                .HasOne(gr => gr.Sender)
                .WithMany(u => u.SentGameInvitations)
                .HasForeignKey(gr => gr.SenderId);

            modelBuilder.Entity<GameRequest>()
                .HasOne(gr => gr.Recipient)
                .WithMany(u => u.ReceivedGameInvitations)
                .HasForeignKey(gr => gr.RecipientId);
        }
    }
}