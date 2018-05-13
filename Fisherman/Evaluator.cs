using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fisherman
{
    class Evaluator
    {
        internal static async Task InitAsync()
        {
            await Task.Run((Action)Init);
        }

        static void Init()
        {
            // TODO start the evaluator and grab standard in/out
        }

        internal static float[][] Evaluate(ChessPosition[] positions)
        {
            float[][] moveValues = new float[positions.Length][];

            var bestMove = ChessMove.Parse("e2e4");
            var bestMoveHex = bestMove.From.Rank << 9 | bestMove.From.File << 6 | bestMove.To.Rank << 3 | bestMove.To.File;

            var placeholderEvaluation = new float[ChessMove.TotalPossible];
            moveValues[0] = placeholderEvaluation;

            for (int i = 0; i < moveValues.Length; i++)
            {
                var moveValue = i == bestMoveHex ? 1.0f : 0.0f;

                placeholderEvaluation[i] = moveValue;
            }

            /* TODO perform an actual query of the neural network
             * and deserialize the output to the moveValue array
             */

            return moveValues;
        }

        internal static float[] Evaluate(ChessPosition position) => Evaluate(new ChessPosition[] { position })[0];
    }
}
