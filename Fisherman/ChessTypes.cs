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

                whiteOO = castling.Contains("K"),
                whiteOOO = castling.Contains("Q"),
                blackOO = castling.Contains("k"),
                blackOOO = castling.Contains("q"),
                board = Board.FromFen(boardfen),
            };

            return position;
        }
        internal static ChessPosition FromFEN(string fen)
        {
            string[] args = fen.Split(' ');

            return FromFEN(args[0], args[1], args[2]);
        }

        // instance members
        Board board;
        internal bool whiteToMove, whiteOO, whiteOOO, blackOO, blackOOO;
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
            // TODO ChessPositoin.ApplyMove flag enPassant square

            var newPosition = Clone();

            char piece;
            if (move.promotion == '\0')
                piece = GetPiece(move.From);
            else
                piece = move.promotion;

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

        // overrides
        public override string ToString()
        {
            /* * OOO OO
             * RNBQKBNR
             * PPPPPPPP
             * ........
             * ........
             */

            StringBuilder sb = new StringBuilder();

            var playerInfoFormat = "{0,1} {1,3} {2,2}\n";
            
            // render black info
            var blacksMove = !whiteToMove ? "*" : "";
            var blackCastleLong = blackOOO ? "OOO" : "";
            var blackCastleShort = blackOO ? "OO" : "";

            sb.AppendFormat(playerInfoFormat, blacksMove, blackCastleLong, blackCastleShort);
            sb.AppendLine();

            // render board
            sb.AppendLine(board.ToString());
            sb.AppendLine();

            // render white info
            var whitesMove = whiteToMove ? "*" : "";
            var whiteCastleLong = whiteOOO ? "OOO" : "";
            var whiteCastleShort = whiteOO ? "OO" : "";

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

        public static Board EmptyBoard()
        {
            var board = new char[8][];
            for (int i = 0; i < board.Length; i++)
                board[i] = "........".ToCharArray();

            return new Board { board = board };
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
            var board = EmptyBoard();

            var currentSquare = Square.Parse("a8");

            for(int i = 0; i < boardfen.Length; i++)
            {
                var fenChar = boardfen[i];

                // next rank
                if (fenChar == '/')
                {
                    currentSquare.Rank--;
                    currentSquare.File = 0;
                }

                // empty squares
                else if (fenChar >= '0' && fenChar <= '9')
                {
                    var emptySquareCount = fenChar - '0';
                    currentSquare.File += emptySquareCount;
                }

                // AN ACTUAL PIECE!!! (we support fairy chess pieces by not doing any checks,
                // but take no responsibility for the outcome)
                else
                    try
                    {
                        board[currentSquare] = fenChar;
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw new ArgumentException(string.Format("Bad FEN string {0}", boardfen), e);
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
            binary = rank << fieldBits | file;
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
