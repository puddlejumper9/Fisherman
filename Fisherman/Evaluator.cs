using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Fisherman
{
    class Evaluator
    {
        static Random rand = new Random();
        static NetworkStream evaluatorStream;
        static StreamWriter input;
        static StreamReader output;

        internal static async Task InitAsync()
        {
            //await Task.Run((Action)Init);
            Init();
        }

        internal static void Init()
        {
            // TODO connect to the evaluator and grab standard in/out

            var hostname = "localhost";
            var port = 65432;

            var evaluatorClient = new TcpClient();
            evaluatorClient.Connect(hostname, port);

            evaluatorStream = evaluatorClient.GetStream();

            input = new StreamWriter(evaluatorStream);
            output = new StreamReader(evaluatorStream);

            
        }

        internal static float[][] Evaluate(ChessPosition[] positions)
        {
            float[][] moveValues = new float[positions.Length][];

            // get list of legal moves
            var moves = positions[0].GetLegalMoves(false);

            // randomly select a legal move
            ChessMove bestMove = moves[rand.Next(moves.Length)];

            // evaluation of moves to indicate selected move
            var placeholderEvaluation = new float[ChessMove.TotalPossible];
            moveValues[0] = placeholderEvaluation;

            for (int i = 0; i < placeholderEvaluation.Length; i++)
            {
                placeholderEvaluation[i] = 0.0f;
            }

            // flag selected move as good evaluation
            placeholderEvaluation[bestMove.binary] = 1.0f;


            /* TODO perform an actual query of the neural network
             * and deserialize the output to the moveValue array
             */

            return moveValues;
        }

        internal static float[] Evaluate(ChessPosition position) => Evaluate(new ChessPosition[] { position })[0];
    }
}
