using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using board.engine;
using board.engine.Board;
using board.engine.Movement;
using chess.engine.Entities;
using chess.engine.Extensions;
using chess.engine.Game;
using chess.engine.SAN;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace chess.functions
{
    public class ChessGameTrigger
    {
        private readonly IBoardEngineProvider<ChessPieceEntity> _boardEngineProvider;
        private readonly ICheckDetectionService _checkDetectionService;
        private readonly IBoardSetup<ChessPieceEntity> _boardSetup;

        public ChessGameTrigger(
            IBoardEngineProvider<ChessPieceEntity> boardEngineProvider, 
            IBoardSetup<ChessPieceEntity> boardSetup,
            ICheckDetectionService checkDetectionService
            )
        {
            _boardSetup = boardSetup;
            _boardEngineProvider = boardEngineProvider;
            _checkDetectionService = checkDetectionService;
        }
        [FunctionName("ChessGame")]
        public async Task<IActionResult> Run(
                [HttpTrigger(AuthorizationLevel.Function, "get", 
                Route = "ChessGame/{board?}/{move?}")] HttpRequest req,
                string board, string move,
                ILogger log)
        {
            var game = await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(board))
                {
                    return new ChessGame(_boardEngineProvider, _checkDetectionService, _boardSetup, Colours.White);
                }

                return ChessGameConvert.Deserialise(board);
            });

            string moveResult = string.Empty;

            if (!string.IsNullOrEmpty(move))
            {
                moveResult = game.Move(move);
            }

            var items = game.BoardState.GetItems((int) game.CurrentPlayer).ToArray();
            return new JsonResult(new ChessWebApiResult(game, game.CurrentPlayer, moveResult ?? "", items));
        }
    }

    public class ChessWebApiResult {

        public string Message { get; set; }
        public string Board { get; set; }
        public string BoardText { get; set; }
        public Move[] AvailableMoves { get; set; }
        public string WhoseTurn { get; set; }
        [JsonIgnore]

        public ChessGame Game { get; }

        [JsonIgnore]
        public IEnumerable<BoardMove> Moves { get; }

        public ChessWebApiResult(
            engine.Game.ChessGame game,
            Colours toMove,
            string message,
            params LocatedItem<ChessPieceEntity>[] items
        )
        {
            Game = game;
            Board = ChessGameConvert.Serialise(game);
            BoardText = new ChessBoardBuilder().FromChessGame(game).ToTextBoard();
            Moves = items.SelectMany(i => i.Paths.FlattenMoves());
            AvailableMoves = ToMoveList(items);
            WhoseTurn = toMove.ToString();
            Message = message;
        }

        public Move[] ToMoveList(params LocatedItem<ChessPieceEntity>[] locatedItems)
        {
            return locatedItems
                .SelectMany(i => i.Paths.FlattenMoves())
                .Select(m => new Move
                {
                    Coord = $"{m.ToChessCoords()}",
                    SAN = StandardAlgebraicNotation.ParseFromGameMove(Game.BoardState, m, true).ToNotation()
                }).ToArray();
        }
    }

    public class Move
    {
        public string SAN { get; set; }
        public string Coord { get; set; }
    }
}
