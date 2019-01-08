using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Fisherman
{
    internal class Engine : IDisposable
    {
        readonly static string Name = "Fisherman 0.1";
        readonly static string Author = "Nolan Shettle";

        StreamReader input;
        StreamWriter output;
        TcpListener listener;

        bool networked = false;
        bool infinite = true;
        bool searching = false;

        Task EvalInitT;
        Task NewGameT;

        List<GameState> game = new List<GameState>();
        ChessPosition CurrentPosition { get { return game.Last().Position; } }

        internal void Start(string[] args)
        {
            // initialize the engine
            EvalInitT = Evaluator.InitAsync();

            while (true)
            {
                // connect to front end
                OpenStreams(args);

                // message loop
                while (input!=null)
                {
                    var command = ReadGUI();

                    ProcessCommand(command.Split(' '));
                }

                if (!networked)
                    break;

                Console.WriteLine("\n\n");

                output.Dispose();
                output = null;
            }
        }

        private void ProcessCommand(string[] args)
        {
            var command = args[0];

            switch (command)
            {
                case "uci": UciInit(); break;
                case "quit": input = null; break;
                case "isready": ReadyUp(); break;
                case "ucinewgame": NewGameT = NewGame(); break;
                case "position": SetupPosition(args); break;
                case "go": SearchStartAsync(args); break;
                case "stop": searching = false; break;

                default: break;
            }
        }

        private async void SearchStartAsync(string[] args)
        {
            infinite = false;

            if (args[1] == "infinite")
                infinite = true;

            ChessMove bestMove = await Task.Run(new Func<ChessMove>(Search));

            TellGUI("bestmove {0}\n", CurrentPosition.RenderMove(bestMove));
        }

        private ChessMove Search()
        {
            searching = true;

            var depth = 0;
            var nodes = 1;
            var startTime = DateTime.Now;

            var bestMove = new ChessMove(0);

            // TODO perform a real search
            var moveValues = Evaluator.Evaluate(CurrentPosition);

            var bestValue = float.NegativeInfinity;

            for (int i = 0; i < moveValues.Length; i++)
            {
                var moveValue = moveValues[i];
                if (moveValue > bestValue)
                {
                    var fromRank = i >> 9;
                    var fromFile = i >> 6 & 7;
                    var toRank = i >> 3 & 7;
                    var toFile = i & 7;

                    bestMove = new ChessMove(new Square(fromRank, fromFile), new Square(toRank, toFile));
                    bestValue = moveValue;
                }
            }

            var timems = (DateTime.Now - startTime).TotalMilliseconds;
            var time = timems / 1000.0;
            var nps = nodes / time;
            var multipv = 1;
            var scoreMode = "cp";
            var score = 0;
            var mainline = bestMove;
            TellGUI("info depth {0} multipv {1} score {2} {3} nodes {4} nps {5} time {6} pv {7}\n", depth, multipv, scoreMode, score, nodes, (int)nps, (int)timems, mainline);

            // a normal search can be terminated by the engine
            if (infinite)
                // an infinite search must wait for a stop command from the GUI
                while (searching)
                    Thread.Sleep(10);

            return bestMove;
        }

        private void SetupPosition(string[] args)
        {
            var positionType = args[1];
            ChessPosition position;

            switch (positionType)
            {
                case "startpos": position = ChessPosition.FromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 "); break;
                case "fen": position = ChessPosition.FromFEN(args[2], args[3], args[4]); break;
                default: return;
            }

            game.Add(new GameState(position, new ChessMove(0)));

            int movesIndex;
            for (movesIndex = 2; movesIndex < args.Length; movesIndex++)
                if (args[movesIndex] == "moves")
                    break;

            for (int i = movesIndex + 1; i < args.Length; i++)
            {
                ChessMove move = ChessMove.Parse(args[i]);

                position = position.ApplyMove(move);
                game.Add(new GameState(position, move));
            }
        }

        private void ReadyUp()
        {
            // allow any incomplete tasks to finish
            EvalInitT?.Wait();
            NewGameT?.Wait();

            TellGUI("readyok\n");
        }

        private async Task NewGame()
        {
            // clear hashtable etc...
            await Task.Run((Action)game.Clear);
        }

        private void UciInit()
        {
            // id
            TellGUI(
                "id name {0}\n" +
                "id author {1}\n", Engine.Name, Engine.Author);

            // options

            TellGUI("uciok\n");
        }

        private string ReadGUI()
        {
            var message = "";
            try { message = input.ReadLine(); }
            catch (IOException e)
            {
                input = null;
                message = e.Message;
            }

            DebugWrite("G:{0}", message);

            return message;
        }

        private void TellGUI(string format, params object[] args)
        {
            var message = string.Format(format, args);

            output.Write(message);
            output.Flush();

            DebugWrite("E:{0}", message);
        }

        private void DebugWrite(string format, string message)
        {
            if (networked)
                Console.Write(format, message);
        }

        private void OpenStreams(string[] args)
        {
            Stream instream, outstream;

            // get remote connection
            if (args.Length >= 1 && args[0] == "--host")
            {
                networked = true;
                listener = new TcpListener(IPAddress.Loopback, 24377);
                instream = outstream = GetConnectorStream();
            }
            // use standard in/out
            else
            {
                instream = Console.OpenStandardInput();
                outstream = Console.OpenStandardOutput();
            }

            input = new StreamReader(instream);
            output = new StreamWriter(outstream);
        }

        private Stream GetConnectorStream()
        {
            Console.WriteLine("Waiting for connector...");

            listener.Start(1);
            var client = listener.AcceptTcpClient();
            listener.Stop();

            Console.WriteLine("{0} connected\n", client.Client.RemoteEndPoint);

            return client.GetStream();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    input.Dispose();
                    output.Dispose();
                }

                // set large fields to null.
                listener.Stop();
                listener = null;
                game = null;

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
