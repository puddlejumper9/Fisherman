using System;
using System.Collections.Generic;
using System.Text;

namespace Fisherman
{
    internal struct ChessPosition : ICloneable
    {
        internal static ChessPosition FromFEN(string boardfen, string turn, string castling)
        {
            var position = new ChessPosition
            {
                whiteToMove = (turn == "w"),
                board = Board.FromFen(boardfen),
                enPassantSquare = new Square(64),
            };

            foreach(char c in castling)
            {
                switch (c)
                {
                    case 'K':
                    case 'Q':
                        for (Square ws = new Square(0); ws.binary < 8; ws.binary++)
                            if (position.GetPiece(ws) == 'R')
                                if (position.whiteOOO == null)
                                    position.whiteOOO = ws.File;
                                else
                                    position.whiteOO = ws.File;
                        break;

                    case 'k':
                    case 'q':
                        for (Square bs = new Square(64 - 8); bs.binary < 64; bs.binary++)
                            if (position.GetPiece(bs) == 'r')
                                if (position.blackOOO == null)
                                    position.blackOOO = bs.File;
                                else
                                    position.blackOO = bs.File;
                        break;

                    default:
                        if (c >= 'A' && c <= 'H')
                            if (position.whiteOOO == null)
                                position.whiteOOO = c - 'A';
                            else
                                position.whiteOO = c - 'A';

                        else if (c >= 'a' && c <= 'h')
                            if (position.blackOOO == null)
                                position.blackOOO = c - 'a';
                            else
                                position.blackOO = c - 'a';

                        break;
                }
            }

            return position;
        }
        internal static ChessPosition FromFEN(string fen)
        {
            string[] args = fen.Split(' ');

            return FromFEN(args[0], args[1], args[2]);
        }

        // instance members
        // for easy viewing in debugger
        internal string tostring
        {
            get => ToString();
        }
        Board board;
        internal bool whiteToMove;
        internal int? whiteOO, whiteOOO, blackOO, blackOOO;
        internal Square enPassantSquare;

        // methods
        internal char GetPiece(Square t)
        {
            return board[t];
        }
        internal void SetPiece(Square t, char p)
        {
            board[t] = p;
        }
        internal ChessPosition ApplyMove(ChessMove move)
        {
            // TODO ChessPosition.ApplyMove add support for castling
            // TODO ChessPositoin.ApplyMove update enPassant square

            var newPosition = Clone();

            char piece;
            if (move.promotion == '\0')
                piece = GetPiece(move.From);
            else
            {
                piece = move.promotion;
                if (whiteToMove && piece >= 'a')
                    piece -= ' ';
            }

            // clear source square
            newPosition.SetPiece(move.From, '.');
            // set destination square to piece
            newPosition.SetPiece(move.To, piece);

            newPosition.whiteToMove = !newPosition.whiteToMove;

            return newPosition;
        }
        public ChessPosition Clone()
        {
            var newPosition = this;

            newPosition.board = board.Clone();

            return newPosition;
        }
        internal string RenderMove(ChessMove move)
        {
            var promotedTo = '\0';

            // special case: pawn promotion for white
            if (move.From.Rank == 6 &&
                move.To.Rank >= 4 && move.To.Rank <= 7 &&
                GetPiece(move.From) == 'P')
            {
                // Pa7a8 -> a7a8Q
                // Pa7a7 -> a7a8R
                // Pa7a6 -> a7a8B
                // Pa7a5 -> a7a8N
                // rank value from 4 to 7

                //                5678
                var promotions = "NBRQ";
                promotedTo = promotions[move.To.Rank - 4];
            }

            // special case: pawn promotion for black
            if (move.From.Rank == 1 &&
                move.To.Rank >= 0 && move.To.Rank <= 3 &&
                GetPiece(move.From) == 'p')
            {
                // pa2a1 -> a2a1q
                // pa2a2 -> a2a1r
                // pa2a3 -> a2a1b
                // pa2a4 -> a2a1n
                // rank value from 0 to 3

                //                1234
                var promotions = "qrbn";
                promotedTo = promotions[move.To.Rank];
            }

            return move.ToString() + promotedTo;
        }
        #region legality checking
        internal bool IsLegal(ChessMove move)
        {
            // move = ChessMove.Parse("a7a5");
            // ensure source and destination squares are actually on the board
            if (!move.IsLegal())
                return false;

            // you can only move your own pieces
            if (!IsOwnPiece(move.From))
                return false;

            // TODO castling is represented as the king taking the rook so we need to check for that

            if (IsOwnPiece(move.To))
                return false;

            return CheckPieceRules(GetPiece(move.From), move);
        }
        private bool CheckPieceRules(char piece, ChessMove move)
        {
            // convert black pieces to white
            if (piece >= 'a' && piece <= 'z')
                piece -= ' ';

            var rankDistance = move.To.Rank - move.From.Rank;

            // we don't care about moving left vs right so normalize to simplify checking
            var fileDistance = move.To.File - move.From.File;
            if (fileDistance < 0) fileDistance = -fileDistance;

            // forward/backward is only necessary for pawns
            if (piece == 'P')
            {
                // legal direction depends on who's move it is
                if (whiteToMove) {
                    // white's pawn moving backward is illegal
                    if (rankDistance <= 0)
                        return false;
                } else
                // black's pawn moving backward is illegal
                if (rankDistance >= 0)
                    return false;

                // normalize distance to positive now that we've verified the direction
                if (rankDistance < 0)
                    rankDistance = -rankDistance;

                // capturing move
                // the en passant square will always be behind an enemy pawn so it can't be directly in front of us
                if (GetPiece(move.To) != '.' || enPassantSquare == move.To)
                {
                    // must move left or right and forward exactly one square
                    if (fileDistance != 1 || rankDistance != 1)
                        return false;
                    else return true;
                }
                // non-capturing move
                else
                {
                    // can only move straight forward
                    if (fileDistance != 0)
                        return false;

                    // moving one square is always allowed since we're not capturing
                    if (rankDistance == 1)
                        return true;
                    
                    // moving two squares is the last option
                    if (rankDistance != 2)
                        return false;

                    // moving two squares is only allowed from 2nd rank for white
                    if (whiteToMove)
                    {
                        if (move.From.Rank != 1)
                            return false;

                        // with the 3rd rank empty
                        if (GetPiece(new Square(2, move.From.File)) == '.')
                            return true;
                        else return false;
                    }
                    // and 7th rank for black
                    else
                    {
                        if (move.From.Rank != 6)
                            return false;

                        // with the 6th rank empty
                        if (GetPiece(new Square(5, move.From.File)) == '.')
                            return true;
                        else return false;
                    }
                }
            }

            // all other pieces
            else
            {
                // normalize rank distance to positive since we only care for pawns
                // and it simplifies checking
                if (rankDistance < 0)
                    rankDistance = -rankDistance;

                switch (piece)
                {
                    case 'R':
                        // it can't move along both a rank and file simultaneously
                        if (rankDistance == 0 || fileDistance == 0)
                            // check path is clear
                            break;
                        else
                            return false;

                    case 'N':
                        // knight must move 2 squares in either direction
                        if ((rankDistance == 2 || fileDistance == 2) &&
                            // and then one square in either direction
                            (rankDistance == 1 || fileDistance == 1))
                            // no path checking
                            return true;
                        else
                            return false;

                    case 'B':
                        // bishop can only move diagonally
                        if (fileDistance != rankDistance)
                            return false;
                        else
                            // check path clear
                            break;

                    case 'Q':
                        // can't move along rank or file or they must be the same
                        if (rankDistance == 0 || fileDistance == 0 || rankDistance == fileDistance)
                            // check path clear
                            break;
                        else
                            return false;

                    case 'K':
                        // we don't check castling here because it must be done while checking for capturing your own pieces
                        // king can move up to one square along both rank and file
                        if ((fileDistance <= 1 && rankDistance <= 1))
                            return !this.ApplyMove(move).InCheck;
                        else
                            return false;

                    default:
                        // assume unkown piece's moves are legal
                        return true;
                }

                return IsPathClear(move);
            }
        }
        private bool IsPathClear(ChessMove move)
        {
            int rankDirection = Direction(move.From.Rank, move.To.Rank);
            int fileDirection = Direction(move.From.File, move.To.File);

            var direction = new Square(rankDirection, fileDirection);

            for (var currentSquare = move.From + direction; currentSquare != move.To; currentSquare += direction)
                if (GetPiece(currentSquare) != '.')
                    return false;

            return true;
        }
        private int Direction(int from, int to)
        {
            if (to > from)
                return 1;
            else if (to == from)
                return 0;
            else
                return -1;
        }
        internal bool InCheck
        {
            get
            {
                // get the square of the enemy king to see if he's in check
                Square kingSquare;
                for(kingSquare = new Square(0); kingSquare.binary < Square.MaxValue; kingSquare.binary++)
                {
                    var piece = GetPiece(kingSquare);

                    if (piece == (whiteToMove ? 'k' : 'K'))
                        break;
                }

                var moves = new List<ChessMove>();
                GetKnightMoves(kingSquare, moves, true);
                for (int i = 0; i < moves.Count; i++)
                    if (GetPiece(moves[i].To) == (whiteToMove ? 'N' : 'n'))
                        return true;
                moves.Clear();

                GetBishopMoves(kingSquare, moves, true);
                for (int i = 0; i < moves.Count; i++)
                {
                    var piece = GetPiece(moves[i].To);
                    if (piece == (whiteToMove ? 'B' : 'b') || piece == (whiteToMove ? 'Q' : 'q'))
                        return true;
                }
                moves.Clear();

                GetRookMoves(kingSquare, moves, true);
                for (int i = 0; i < moves.Count; i++)
                {
                    var piece = GetPiece(moves[i].To);
                    if (piece == (whiteToMove ? 'R' : 'r') || piece == (whiteToMove ? 'Q' : 'q'))
                        return true;
                }
                moves.Clear();

                GetKingMoves(kingSquare, moves, true);
                for (int i = 0; i < moves.Count; i++)
                {
                    var piece = GetPiece(moves[i].To);
                    if (piece == (whiteToMove ? 'K' : 'k'))
                        return true;
                }

                var kingFile = kingSquare.File;
                var attackingPawn = (whiteToMove ? 'P' : 'p');
                var attackingPawnRank = kingSquare.Rank + (whiteToMove ? -1 : 1);
                if (attackingPawnRank >= 8 || attackingPawnRank < 0)
                    return false;

                var leftPawnAttacker = kingFile - 1;
                if (leftPawnAttacker >= 0 && GetPiece(new Square(attackingPawnRank, leftPawnAttacker)) == attackingPawn)
                    return true;

                var rightPawnAttacker = kingFile + 1;
                if (rightPawnAttacker < 8 && GetPiece(new Square(attackingPawnRank, rightPawnAttacker)) == attackingPawn)
                    return true;

                return false;
            }
        }
        #endregion
        #region move list generation
        internal ChessMove[] GetLegalMoves(bool guarding)
        {
            var moves = new List<ChessMove>();

            for (var from = new Square(0); from.binary < Square.MaxValue; from.binary++)
            {
                var piece = GetPiece(from);

                if (!IsOwnPiece(piece))
                    continue;

                GetPieceMoves(piece, from, moves, guarding);
            }

            return moves.ToArray();
        }
        private void GetPieceMoves(char piece, Square from, List<ChessMove> moves, bool guarding)
        {
            switch (piece)
            {
                case 'P':
                case 'p':
                    GetPawnMoves(from, moves, guarding);
                    break;

                case 'R':
                case 'r':
                    GetRookMoves(from, moves, guarding);
                    break;

                case 'N':
                case 'n':
                    GetKnightMoves(from, moves, guarding);
                    break;

                case 'B':
                case 'b':
                    GetBishopMoves(from, moves, guarding);
                    break;

                case 'Q':
                case 'q':
                    GetQueenMoves(from, moves, guarding);
                    break;

                case 'K':
                case 'k':
                    GetKingMoves(from, moves, guarding);
                    break;
            }
        }
        private void GetPawnMoves(Square from, List<ChessMove> moves, bool guarding)
        {
            int forward = whiteToMove ? 1 : -1;
            var oneForward = from;
            oneForward.Rank += forward;

            if(GetPiece(oneForward)=='.' && !guarding)
            {
                TryAddMove(new ChessMove(from, oneForward), moves, guarding);

                if (from.Rank == (whiteToMove ? 1 : 6))
                {
                    var twoForward = oneForward;
                    twoForward.Rank += forward;

                    if (GetPiece(twoForward) == '.')
                        TryAddMove(new ChessMove(from, twoForward), moves, guarding);
                }
            }

            var takeLeft = oneForward;
            var file = takeLeft.File-1;
            takeLeft.File = file;
            if (file >= 0 && (IsEnemyPiece(takeLeft) || takeLeft == enPassantSquare))
                TryAddMove(new ChessMove(from, takeLeft), moves, guarding);

            var takeRight = oneForward;
            file = takeRight.File+1;
            takeRight.File = file;
            if (file < 8 && (IsEnemyPiece(takeRight) || takeRight == enPassantSquare))
                TryAddMove(new ChessMove(from, takeRight), moves, guarding);
        }
        private void GetKingMoves(Square from, List<ChessMove> moves, bool guarding)
        {
            for (var rankOffset = -1; rankOffset <= 1; rankOffset++)
            {
                var rank = from.Rank + rankOffset;
                if (rank < 0 || rank >= 8)
                    continue;

                for (var fileOffset = -1; fileOffset <= 1; fileOffset++)
                {
                    if (rankOffset == 0 && fileOffset == 0)
                        continue;

                    var file = from.File + fileOffset;
                    if (file < 0 || file >= 8)
                        continue;

                    var toSquare = new Square(rank, file);
                    var takenPiece = GetPiece(toSquare);

                    if (!TakingOwn(toSquare, guarding))
                    {
                        ChessMove move = new ChessMove(from, toSquare);
                        TryAddMove(move, moves, guarding);
                    }
                }
            }
        }
        private void GetQueenMoves(Square from, List<ChessMove> moves, bool guarding)
        {
            GetRookMoves(from, moves, guarding);
            GetBishopMoves(from, moves, guarding);
        }
        private void GetRookMoves(Square from, List<ChessMove> moves, bool guarding)
        {
            for (var offset = 1; ; offset++)
            {
                var rank = from.Rank + offset;

                if (rank < 0 || rank >= 8)
                    break;

                var toSquare = new Square(rank, from.File);
                var takenPiece = GetPiece(toSquare);

                if (takenPiece == '.')
                    TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                else
                {
                    if (!TakingOwn(toSquare, guarding))
                        TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                    break;
                }
            }
            for (var offset = 1; ; offset++)
            {
                var file = from.File + offset;

                if (file < 0 || file >= 8)
                    break;

                var toSquare = new Square(from.Rank, file);
                var takenPiece = GetPiece(toSquare);

                if (takenPiece == '.')
                    TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                else
                {
                    if (!TakingOwn(toSquare, guarding))
                        TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                    break;
                }
            }
            for (var offset = 1; ; offset++)
            {
                var rank = from.Rank - offset;

                if (rank < 0 || rank >= 8)
                    break;

                var toSquare = new Square(rank, from.File);
                var takenPiece = GetPiece(toSquare);

                if (takenPiece == '.')
                    TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                else
                {
                    if (!TakingOwn(toSquare, guarding))
                        TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                    break;
                }
            }
            for (var offset = 1; ; offset++)
            {
                var file = from.File - offset;

                if (file < 0 || file >= 8)
                    break;

                var toSquare = new Square(from.Rank, file);
                var takenPiece = GetPiece(toSquare);

                if (takenPiece == '.')
                    TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                else
                {
                    if (!TakingOwn(toSquare, guarding))
                        TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                    break;
                }
            }
        }
        private void GetBishopMoves(Square from, List<ChessMove> moves, bool guarding)
        {
            for (var offset = 1; ; offset++)
            {
                var rank = from.Rank + offset;
                var file = from.File + offset;

                if (rank < 0 || file < 0 || rank >= 8 || file >= 8)
                    break;

                var toSquare = new Square(rank, file);
                var takenPiece = GetPiece(toSquare);

                if (takenPiece == '.')
                    TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                else
                {
                    if (!TakingOwn(toSquare, guarding))
                        TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                    break;
                }
            }
            for (var offset = 1; ; offset++)
            {
                var rank = from.Rank - offset;
                var file = from.File - offset;

                if (rank < 0 || file < 0 || rank >= 8 || file >= 8)
                    break;

                var toSquare = new Square(rank, file);
                var takenPiece = GetPiece(toSquare);

                if (takenPiece == '.')
                    TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                else
                {
                    if (!TakingOwn(toSquare, guarding))
                        TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                    break;
                }
            }
            for (var offset = 1; ; offset++)
            {
                var rank = from.Rank + offset;
                var file = from.File - offset;

                if (rank < 0 || file < 0 || rank >= 8 || file >= 8)
                    break;

                var toSquare = new Square(rank, file);
                var takenPiece = GetPiece(toSquare);

                if (takenPiece == '.')
                    TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                else
                {
                    if (!TakingOwn(toSquare, guarding))
                        TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                    break;
                }
            }
            for (var offset = 1; ; offset++)
            {
                var rank = from.Rank - offset;
                var file = from.File + offset;

                if (rank < 0 || file < 0 || rank >= 8 || file >= 8)
                    break;

                var toSquare = new Square(rank, file);
                var takenPiece = GetPiece(toSquare);

                if (takenPiece == '.')
                    TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                else
                {
                    if (!TakingOwn(toSquare, guarding))
                        TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                    break;
                }
            }
        }
        private void GetKnightMoves(Square from, List<ChessMove> moves, bool guarding)
        {
            for (var rankOffset = -2; rankOffset <= 2; rankOffset++)
            {
                var rank = from.Rank + rankOffset;
                if (rank < 0 || rank >= 8)
                    continue;

                for (var fileOffset = -2; fileOffset <= 2; fileOffset++)
                {
                    var file = from.File + fileOffset;
                    if (file < 0 || file >= 8)
                        continue;

                    // skip invalid horsey moves
                    if (Math.Abs(rankOffset) + Math.Abs(fileOffset) != 3)
                        continue;

                    var toSquare = new Square(rank, file);
                    var takenPiece = GetPiece(toSquare);

                    if (!TakingOwn(toSquare, guarding))
                        TryAddMove(new ChessMove(from, toSquare), moves, guarding);
                }
            }
        }
        private void TryAddMove(ChessMove move, List<ChessMove> moves, bool guarding)
        {
            //if(move.binary == ChessMove.Parse("b2a1").binary) { }

            if (guarding || !ApplyMove(move).InCheck)
                moves.Add(move);
        }
        #endregion
        private bool IsWhitePiece(char piece) => piece >= 'A' && piece <= 'Z';
        private bool IsBlackPiece(char piece) => piece >= 'a' && piece <= 'z';
        private bool TakingOwn(Square toSquare, bool guarding)
        {
            if (guarding)
                return false;

            return IsOwnPiece(toSquare);
        }
        private bool IsOwnPiece(Square square)
        {
            var piece = GetPiece(square);

            return IsOwnPiece(piece);
        }
        private bool IsOwnPiece(char piece)
        {
            if (piece != '.')
                if (whiteToMove)
                {
                    if (IsWhitePiece(piece))
                        return true;
                }
                else
                {
                    if (IsBlackPiece(piece))
                        return true;
                }
            return false;
        }
        private bool IsEnemyPiece(Square square)
        {
            var piece = GetPiece(square);

            return IsEnemyPiece(piece);
        }
        private bool IsEnemyPiece(char piece)
        {
            return piece != '.' && !IsOwnPiece(piece);
        }

        // overrides
        public override string ToString()
        {
            /* * OOO OO
             * rnbqkbnr
             * pppppppp
             * ........
             * ........
             */
            
            StringBuilder sb = new StringBuilder();

            var playerInfoFormat = "{0,1} {1} {2}\n";
            
            // render black info
            var blacksMove = !whiteToMove ? "*" : "";
            var blackCastleLong = blackOOO ?? ' ';
            var blackCastleShort = blackOO ?? ' ';

            sb.AppendFormat(playerInfoFormat, blacksMove, blackCastleLong, blackCastleShort);

            // render board
            sb.Append(board.ToString());

            // render white info
            var whitesMove = whiteToMove ? "*" : "";
            var whiteCastleLong = whiteOOO ?? ' ';
            var whiteCastleShort = whiteOO ?? ' ';

            sb.AppendFormat(playerInfoFormat, whitesMove, whiteCastleLong, whiteCastleShort);

            return sb.ToString();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
        public override bool Equals(object obj)
        {
            if (!(obj is ChessPosition))
            {
                return false;
            }

            var position = (ChessPosition)obj;
            return EqualityComparer<Board>.Default.Equals(board, position.board) &&
                   whiteToMove == position.whiteToMove &&
                   whiteOO == position.whiteOO &&
                   whiteOOO == position.whiteOOO &&
                   blackOO == position.blackOO &&
                   blackOOO == position.blackOOO &&
                   EqualityComparer<Square>.Default.Equals(enPassantSquare, position.enPassantSquare);
        }
        public override int GetHashCode()
        {
            var hashCode = 1944320102;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Board>.Default.GetHashCode(board);
            hashCode = hashCode * -1521134295 + whiteToMove.GetHashCode();
            hashCode = hashCode * -1521134295 + whiteOO.GetHashCode();
            hashCode = hashCode * -1521134295 + whiteOOO.GetHashCode();
            hashCode = hashCode * -1521134295 + blackOO.GetHashCode();
            hashCode = hashCode * -1521134295 + blackOOO.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Square>.Default.GetHashCode(enPassantSquare);
            return hashCode;
        }
        public static bool operator ==(ChessPosition a, ChessPosition b) => a.Equals(b);
        public static bool operator !=(ChessPosition a, ChessPosition b) => !(a == b);
    }
    internal struct Board : ICloneable
    {
        char[][] board;

        public static Board EmptyBoard
        {
            get
            {
                var board = new char[8][];
                for (int i = 0; i < board.Length; i++)
                    board[i] = "........".ToCharArray();

                return new Board { board = board };
            }
        }
        public Board(string[] v) : this()
        {
            if (v.Length != 8)
                throw new ArgumentException("Incorrect chessboard format");

            board = new char[v.Length][];
            for(int i = 0; i < v.Length; i++)
            {
                if(v[i].Length != 8)
                    throw new ArgumentException("Incorrect chessboard format");

                board[i] = v[i].ToCharArray();
            }
        }

        public char this[Square t]
        {
            // TODO use Square.Binary to index
            get => board[t.Rank][t.File];
            set => board[t.Rank][t.File] = value;
        }
        internal Board Clone()
        {
            var newBoard = this;

            newBoard.board = new char[board.Length][];
            for(int i = 0; i < board.Length; i++)
            {
                newBoard.board[i] = (char[])board[i].Clone();
            }

            return newBoard;
        }
        public static bool operator ==(Board a, Board b) => a.Equals(b);
        public static bool operator !=(Board a, Board b) => !(a == b);
        public override bool Equals(object obj)
        {
            if (!(obj is Board))
            {
                return false;
            }

            var board = (Board)obj;
            return EqualityComparer<char[][]>.Default.Equals(this.board, board.board);
        }
        public override int GetHashCode()
        {
            var hashCode = -275579349;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<char[][]>.Default.GetHashCode(board);
            return hashCode;
        }
        object ICloneable.Clone()
        {
            return Clone();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = board.Length-1; i >= 0; i--)
            {
                sb.Append(board[i]);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        internal static Board FromFen(string boardfen)
        {
            Board board = EmptyBoard;

            Square currentSquare = Square.Parse("a8");

            for(int i = 0; i < boardfen.Length; i++)
            {
                char fenChar = boardfen[i];

                // next rank
                if (fenChar == '/')
                {
                    currentSquare.Rank--;
                    currentSquare.File = 0;
                }

                // empty squares
                else if (fenChar >= '0' && fenChar <= '9')
                {
                    int emptySquareCount = fenChar - '0';
                    currentSquare.File += emptySquareCount;
                }

                // AN ACTUAL PIECE!!! (we support fairy chess pieces by not doing any checks,
                // but take no responsibility for the outcome)
                else
                {
                    board[currentSquare] = fenChar;
                    currentSquare.File++;
                }
            }

            return board;
        }
    }
    internal struct Square
    {
        // static members
        internal static readonly string files = "abcdefgh";
        private static readonly int fieldBits = 3;
        private static readonly int fieldBitMask = 7;
        internal static readonly int bits = fieldBits * 2;
        internal static readonly int bitMask = 0x3F;
        internal static readonly int MaxValue = 1 << bits;

        // instance members
        internal int binary;

        // properties
        // high bits
        internal int Rank
        {
            get
            {
                return binary >> fieldBits;
            }
            set
            {
                // keep the low bits
                binary &= fieldBitMask;
                // add in the high bits
                binary |= (value & fieldBitMask) << fieldBits;
            }
        }
        // low bits
        internal int File
        {
            get
            {
                return binary & fieldBitMask;
            }
            set
            {
                // keep the high bits
                binary &= fieldBitMask << fieldBits;
                // add in the low bits
                binary |= value & fieldBitMask;
            }
        }

        public Square(int binary)
        {
            this.binary = binary;
        }

        // methods
        internal Square(int rank, int file)
        {
            binary = (rank << fieldBits) | (file & fieldBitMask);
        }
        internal static Square Parse(string t)
        {
            var square = new Square();

            if (t[0] >= 'a')
                square.File = t[0] - 'a';
            else
                square.File = t[0] - 'A';

            square.Rank = t[1] - '1';

            return square;
        }

        // overrides
        public override string ToString()
        {
            return files[File] + (Rank + 1).ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Square))
            {
                return false;
            }

            var square = (Square)obj;
            return binary == square.binary;
        }

        public override int GetHashCode()
        {
            return 1664753756 + binary.GetHashCode();
        }

        // operators
        public static bool operator ==(Square a, Square b) => a.binary == b.binary;
        public static bool operator !=(Square a, Square b) => !(a == b);
        public static Square operator +(Square a, Square b)
        {
            a.Rank += b.Rank;
            a.File += b.File;
            return a;
        }
    }
    internal struct ChessMove
    {
        /// <summary>
        /// Number of combinations of source and destination squares
        /// </summary>
        internal static int TotalPossible = 4096;

        // fields
        internal int binary;
        internal char promotion;

        // properties
        internal Square From
        {
            get
            {
                return new Square(binary >> Square.bits);
            }
            set
            {
                binary &= Square.bitMask;
                binary |= value.binary << Square.bits;
            }
        }
        internal Square To
        {
            get
            {
                return new Square(binary & 0x3F);
            }
            set
            {
                binary &= Square.bitMask << Square.bits;
                binary |= value.binary;
            }
        }

        // constructors
        internal ChessMove(int binary)
        {
            this.binary = binary;
            promotion = '\0';
        }
        internal ChessMove(Square from, Square to)
        {
            binary = (from.binary << Square.bits) | to.binary;
            promotion = '\0';
        }

        // methods
        internal static ChessMove Parse(string move)
        {
            try
            {
                if (move[0] == 'O' || move[0] == 'o')
                {
                    throw new NotImplementedException();
                    // return;
                }

                var fromSquare = Square.Parse(move.Substring(0, 2));
                var toSquare = Square.Parse(move.Substring(2, 2));

                var chessMove = new ChessMove(fromSquare, toSquare);

                if (move.Length == 5)
                    chessMove.promotion = move[4];

                return chessMove;
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("The move string was invalid: {0}", move), e);
            }
        }
        internal bool IsLegal() => binary < 4096;

        // overrides
        public override string ToString()
        {
            var s = From.ToString() + To.ToString();

            if (promotion != '\0')
                s += promotion;

            return s;
        }
    }
}
