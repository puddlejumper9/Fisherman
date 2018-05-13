using System.Collections.Generic;

namespace Fisherman
{
    internal class GameState
    {
        internal ChessPosition Position;
        internal ChessMove Move;

        internal GameState(ChessPosition p, ChessMove m)
        {
            Position = p;
            Move = m;
        }
    }
}