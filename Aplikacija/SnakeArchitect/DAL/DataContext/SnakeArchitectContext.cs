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
        public DbSet<GameRequest> GameRequests { get; set; }
        public DbSet<Winner> Winners { get; set; }

        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<FriendsList> FriendsLists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GameRoom>()
                .HasOne(gr => gr.Board)
                .WithOne(gb => gb.GameRoom)
                .HasForeignKey<GameBoard>("GameRoomId");

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

            modelBuilder.Entity<GameRequest>()
                .HasOne(gr => gr.Sender)
                .WithMany(u => u.SentGameInvitations)
                .HasForeignKey(gr => gr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GameRequest>()
                .HasOne(gr => gr.Recipient)
                .WithMany(u => u.ReceivedGameInvitations)
                .HasForeignKey(gr => gr.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany()
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Recipient)
                .WithMany()
                .HasForeignKey(fr => fr.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendsList>()
                .HasOne(fl => fl.User)
                .WithMany()
                .HasForeignKey(fl => fl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendsList>()
                .HasOne(fl => fl.Friend)
                .WithMany()
                .HasForeignKey(fl => fl.FriendId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
