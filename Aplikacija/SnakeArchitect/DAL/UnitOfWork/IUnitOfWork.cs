using DAL.Repository.IRepository;

namespace DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository User { get; }
        IPlayerRepository Player { get; }
        IGameRoomRepository GameRoom { get; }
        IGameBoardRepository GameBoard { get; }
        ISnakeRepository Snake { get; }
        ILadderRepository Ladder { get; }
        IMoveRepository Move { get; }
        IDiceRepository Dice { get; }
        IChatRepository Chat { get; }
        IFriendRequestRepository FriendRequest { get; }
        IFriendsListRepository FriendsList { get; }
        IGameRequestRepository GameRequest { get; }
        IWinnerRepository Winner { get; }

        Task Save();
    }
}