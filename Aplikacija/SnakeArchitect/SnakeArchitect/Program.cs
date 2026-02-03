using DAL.DataContext;
using DAL.Repository;
using DAL.Repository.IRepository;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<SnakeArchitectContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IGameRoomRepository, GameRoomRepository>();
builder.Services.AddScoped<IGameBoardRepository, GameBoardRepository>();
builder.Services.AddScoped<ISnakeRepository, SnakeRepository>();
builder.Services.AddScoped<ILadderRepository, LadderRepository>();
builder.Services.AddScoped<IMoveRepository, MoveRepository>();
builder.Services.AddScoped<IDiceRepository, DiceRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
builder.Services.AddScoped<IFriendsListRepository, FriendsListRepository>();
builder.Services.AddScoped<IGameRequestRepository, GameRequestRepository>();
builder.Services.AddScoped<IWinnerRepository, WinnerRepository>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();