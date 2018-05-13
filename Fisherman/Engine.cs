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

            TellGUI("bestmove {0}\n", bestMove);
        }

        private ChessMove Search()
        {
            searching = true;

            var depth = 0;
            var nodes = 1;
            var startTime = DateTime.Now;

            // TODO perform a real search
            var moveValues = Evaluator.Evaluate(CurrentPosition);

            ChessMove bestMove = new ChessMove(0);
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

                    bestMove = new ChessMove(new Tile(fromRank, fromFile), new Tile(toRank, toFile));
                    bestValue = moveValue;
                }
            }

            DebugWrite("{0,9:N4} milliseconds\n", (DateTime.Now - startTime).TotalMilliseconds);

            // an infinite search must wait for the GUI to stop the search
            // a normal search can be terminated by the engine
            while (searching)
            {
                Thread.Sleep(1);
                // normal search we can stop the search
                if (!infinite)
                    searching = false;
            }

            TellGUI("info depth {0} nodes {1} nps {2}\n", depth, nodes, nodes / (DateTime.Now - startTime).TotalSeconds);

            return bestMove;
        }

        private void SetupPosition(string[] args)
        {
            var positionType = args[1];
            ChessPosition position;

            switch (positionType)
            {
                case "startpos": position = ChessPosition.StandardStart; break;
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
                position = position.ApplyMove(args[i]);
            }
        }

        private async void ReadyUp()
        {
            // allow any incomplete tasks to finish
            if (EvalInitT != null) await EvalInitT;
            if (NewGameT != null) await NewGameT;

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

            if (networked)
                Console.WriteLine("G:{0}", message);

            return message;
        }

        private void TellGUI(string format, params object[] args)
        {
            var message = string.Format(format, args);

            output.Write(message);
            output.Flush();

            DebugWrite("E:{0}", message);
        }

        private void DebugWrite(string format, params object[] arg)
        {
            if (networked)
                Console.Write(format, arg);
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
