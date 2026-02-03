using DAL.DataContext;
using DAL.Repository;
using DAL.Repository.IRepository;

namespace DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SnakeArchitectContext _context;

        public UnitOfWork(SnakeArchitectContext context)
        {
            _context = context;
            User = new UserRepository(_context);
            Player = new PlayerRepository(_context);
            GameRoom = new GameRoomRepository(_context);
            GameBoard = new GameBoardRepository(_context);
            Snake = new SnakeRepository(_context);
            Ladder = new LadderRepository(_context);
            Move = new MoveRepository(_context);
            Dice = new DiceRepository(_context);
            Chat = new ChatRepository(_context);
            FriendRequest = new FriendRequestRepository(_context);
            FriendsList = new FriendsListRepository(_context);
            GameRequest = new GameRequestRepository(_context);
            Winner = new WinnerRepository(_context);
        }

        public IUserRepository User { get; private set; }
        public IPlayerRepository Player { get; private set; }
        public IGameRoomRepository GameRoom { get; private set; }
        public IGameBoardRepository GameBoard { get; private set; }
        public ISnakeRepository Snake { get; private set; }
        public ILadderRepository Ladder { get; private set; }
        public IMoveRepository Move { get; private set; }
        public IDiceRepository Dice { get; private set; }
        public IChatRepository Chat { get; private set; }
        public IFriendRequestRepository FriendRequest { get; private set; }
        public IFriendsListRepository FriendsList { get; private set; }
        public IGameRequestRepository GameRequest { get; private set; }
        public IWinnerRepository Winner { get; private set; }

        public async Task Save()
        {
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}