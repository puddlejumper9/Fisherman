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

            var bestMove = ChessMove.Parse("b7b8");

            var placeholderEvaluation = new float[ChessMove.TotalPossible];
            moveValues[0] = placeholderEvaluation;

            for (int i = 0; i < placeholderEvaluation.Length; i++)
            {
                var moveValue = 0.0f;

                if (i == bestMove.binary)
                    moveValue = 1.0f;

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
