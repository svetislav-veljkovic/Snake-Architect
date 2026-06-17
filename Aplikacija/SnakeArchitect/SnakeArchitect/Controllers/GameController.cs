using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DAL.Models;
using DAL.DTOs;
using DAL.UnitOfWork;

namespace SnakeArchitectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public GameController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // POST api/game/roll/{roomId}
        // Igrač baca kockicu i odigrava potez
        [HttpPost("roll/{roomId}")]
        public async Task<IActionResult> RollDice(int roomId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Pronađi igrača u sobi
            var player = _unitOfWork.Player
                .Find(p => p.UserId == userId && p.GameRoomId == roomId)
                .FirstOrDefault();

            if (player == null)
                return BadRequest(new { message = "Nisi u ovoj sobi." });

            // Pronađi sobu i tablu
            GameRoom room;
            try { room = await _unitOfWork.GameRoom.GetOne(roomId); }
            catch { return NotFound(new { message = "Soba nije pronađena." }); }

            if (!room.isActive)
                return BadRequest(new { message = "Igra nije aktivna." });

            var board = room.Board;
            if (board == null)
                return BadRequest(new { message = "Tabla nije kreirana." });

            // Baci kockicu (1-6)
            var rng = new Random();
            var diceValue = rng.Next(1, 7);

            // Sačuvaj bacanje kockice
            var dice = new Dice(player.ID, board.ID, diceValue, DateTime.UtcNow);
            await _unitOfWork.Dice.Add(dice);

            // Izračunaj novu poziciju
            var fromPosition = player.CurrentPosition;
            var maxPosition = board.Rows * board.Columns;
            var newPosition = fromPosition + diceValue;

            string moveType = "normal";

            // Provjeri da li je prešao kraj table (ne može ići dalje od 100)
            if (newPosition > maxPosition)
            {
                newPosition = fromPosition; // ostaje na istom mestu
                moveType = "blocked";
            }
            else
            {
                // Provjeri zmije (glava zmije = pao si na nju)
                var snake = board.Snakes.FirstOrDefault(s => s.StarPosition == newPosition);
                if (snake != null)
                {
                    newPosition = snake.EndPosition;
                    moveType = "snake";
                }

                // Provjeri merdevine
                var ladder = board.Ladders.FirstOrDefault(l => l.StartPosition == newPosition);
                if (ladder != null)
                {
                    newPosition = ladder.EndPosition;
                    moveType = "ladder";
                }
            }

            // Sačuvaj potez
            var move = new Move(player.ID, board.ID, fromPosition, newPosition, moveType, DateTime.UtcNow);
            await _unitOfWork.Move.Add(move);

            // Ažuriraj poziciju igrača
            player.CurrentPosition = newPosition;
            _unitOfWork.Player.Update(player);

            // Provjeri pobjedu (dostigao zadnje polje)
            bool isWinner = newPosition == maxPosition;
            if (isWinner)
            {
                var winner = new Winner(player.ID);
                await _unitOfWork.Winner.Add(winner);

                // Ažuriraj statistiku
                var user = await _unitOfWork.User.GetOne(userId);
                user.GamesWon++;
                _unitOfWork.User.Update(user);

                // Ažuriraj statistiku gubitnika
                var losers = _unitOfWork.Player
                    .Find(p => p.GameRoomId == roomId && p.ID != player.ID)
                    .ToList();

                foreach (var loser in losers)
                {
                    var loserUser = await _unitOfWork.User.GetOne(loser.UserId);
                    loserUser.GamesLost++;
                    _unitOfWork.User.Update(loserUser);
                }

                // Zatvori sobu
                room.isActive = false;
                _unitOfWork.GameRoom.Update(room);
            }

            await _unitOfWork.Save();

            return Ok(new
            {
                diceValue,
                fromPosition,
                toPosition = newPosition,
                moveType,
                isWinner,
                message = GetMoveMessage(moveType, diceValue, fromPosition, newPosition)
            });
        }

        // GET api/game/{roomId}/state
        // Trenutno stanje igre - pozicije svih igrača
        [HttpGet("{roomId}/state")]
        public async Task<IActionResult> GetGameState(int roomId)
        {
            try
            {
                var room = await _unitOfWork.GameRoom.GetOne(roomId);
                return Ok(new
                {
                    room.ID,
                    room.isActive,
                    Players = room.Players.Select(p => new
                    {
                        p.ID,
                        p.UserId,
                        p.isHost,
                        p.CurrentPosition
                    }),
                    MaxPosition = room.Board != null ? room.Board.Rows * room.Board.Columns : 0
                });
            }
            catch
            {
                return NotFound(new { message = "Soba nije pronađena." });
            }
        }

        // GET api/game/{roomId}/moves
        // Historija poteza u sobi
        [HttpGet("{roomId}/moves")]
        public async Task<IActionResult> GetMoves(int roomId)
        {
            GameRoom room;
            try { room = await _unitOfWork.GameRoom.GetOne(roomId); }
            catch { return NotFound(new { message = "Soba nije pronađena." }); }

            if (room.Board == null)
                return Ok(new List<object>());

            var moves = _unitOfWork.Move
                .Find(m => m.GameBoardId == room.Board.ID)
                .OrderByDescending(m => m.TimeStamp)
                .Select(m => new
                {
                    m.ID,
                    m.PlayerId,
                    m.FromPosition,
                    m.ToPosition,
                    m.MoveType,
                    m.TimeStamp
                })
                .ToList();

            return Ok(moves);
        }

        // GET api/game/{roomId}/winner
        // Pobjednik sobe
        [HttpGet("{roomId}/winner")]
        public async Task<IActionResult> GetWinner(int roomId)
        {
            var room = _unitOfWork.GameRoom.Find(r => r.ID == roomId).FirstOrDefault();
            if (room == null)
                return NotFound(new { message = "Soba nije pronađena." });

            var playerIds = _unitOfWork.Player
                .Find(p => p.GameRoomId == roomId)
                .Select(p => p.ID)
                .ToList();

            var winner = _unitOfWork.Winner
                .Find(w => playerIds.Contains(w.PlayerId))
                .FirstOrDefault();

            if (winner == null)
                return Ok(new { hasWinner = false });

            var winnerPlayer = await _unitOfWork.Player.GetOne(winner.PlayerId);
            var winnerUser = await _unitOfWork.User.GetOne(winnerPlayer.UserId);

            return Ok(new
            {
                hasWinner = true,
                playerId = winner.PlayerId,
                userId = winnerPlayer.UserId,
                username = winnerUser.Username,
                wonAt = winner.WonAt
            });
        }

        private static string GetMoveMessage(string moveType, int diceValue, int from, int to)
        {
            return moveType switch
            {
                "snake" => $"Bacio/la si {diceValue}. Pao/pala si na zmiju! {from + diceValue} → {to}",
                "ladder" => $"Bacio/la si {diceValue}. Pronašao/la si merdevine! {from + diceValue} → {to}",
                "blocked" => $"Bacio/la si {diceValue}. Ne možeš ići dalje od kraja table.",
                _ => $"Bacio/la si {diceValue}. Pomjeren/a sa {from} na {to}."
            };
        }
    }
}
