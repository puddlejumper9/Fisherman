using System;
using System.Collections.Generic;
using System.Text;

namespace Fisherman
{
    internal struct ChessPosition : ICloneable
    {
        #region static members
        // fields
        internal static readonly ChessPosition StandardStart = new ChessPosition
        {
            blackOO = true,
            blackOOO = true,
            whiteOO = true,
            whiteOOO = true,
            whiteToMove = true,
            enPassantSquare = new Tile(-1, -1),
            board = new Board(new string[]
            {
                "rnbqkbnr",
                "pppppppp",
                "........",
                "........",
                "........",
                "........",
                "PPPPPPPP",
                "RNBQKBNR"
            })
        };

        // methods
        internal static ChessPosition FromFEN(string boardfen, string turn, string castling)
        {
            var position = new ChessPosition
            {
                whiteToMove = (turn == "w"),

                whiteOO = castling.Contains("K"),
                whiteOOO = castling.Contains("Q"),
                blackOO = castling.Contains("k"),
                blackOOO = castling.Contains("q")
            };

            // TODO read board from FEN

            return position;
        }
        #endregion

        // instance members
        // fields
        // value types
        Board board;
        internal bool whiteToMove, whiteOO, whiteOOO, blackOO, blackOOO;
        internal Tile enPassantSquare;

        // methods
        internal char GetPiece(Tile t)
        {
            return board[t];
        }
        internal void SetPiece(Tile t, char p)
        {
            board[t] = p;
        }
        internal ChessPosition ApplyMove(string m)
        {
            var move = ChessMove.Parse(m);

            return this.ApplyMove(move);
        }
        internal ChessPosition ApplyMove(ChessMove move)
        {
            var newPosition = Clone();

            newPosition.SetPiece(move.From, '.');
            newPosition.SetPiece(move.To, GetPiece(move.From));

            newPosition.whiteToMove = !newPosition.whiteToMove;

            return newPosition;
        }

        public ChessPosition Clone()
        {
            var newPosition = this;

            newPosition.board = board.Clone();

            return newPosition;
        }

        internal string RenderMove(ChessMove bestMove)
        {
            // TODO render the move to long algebraic notation

            // TODO special case: pawn promotion for white
            // pa7a8 -> a7a8Q
            // pa7a7 -> a7a8R
            // pa7a6 -> a7a8B
            // pa7a5 -> a7a8N

            // TODO special case: pawn promotion for black

            var move = bestMove.ToString();

            return move;
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
                   EqualityComparer<Tile>.Default.Equals(enPassantSquare, position.enPassantSquare);
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
            hashCode = hashCode * -1521134295 + EqualityComparer<Tile>.Default.GetHashCode(enPassantSquare);
            return hashCode;
        }
        public static bool operator ==(ChessPosition a, ChessPosition b) => a.Equals(b);
        public static bool operator !=(ChessPosition a, ChessPosition b) => !(a == b);
    }

    internal struct Board : ICloneable
    {
        char[][] board;

        public Board(string[] v) : this()
        {
            if (v.Length != 8)
                throw new ArgumentException("Incorrect chessboard format");

            board = new char[v.Length][];
            for(int i = 0; i < v.Length; i++)
            {
                board[i] = v[i].ToCharArray();
            }
        }

        public char this[Tile t]
        {
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
    }
    internal struct Tile
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
                binary |= value << fieldBits;
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
                binary |= value;
            }
        }

        public Tile(int binary)
        {
            this.binary = binary;
        }

        // methods
        internal Tile(int rank, int file)
        {
            binary = rank << fieldBits | file;
        }
        internal static Tile Parse(string t)
        {
            var tile = new Tile();

            if (t[0] >= 'a')
                tile.File = t[0] - 'a';
            else
                tile.File = t[0] - 'A';

            tile.Rank = t[1] - '1';

            return tile;
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
        /// Number of combinations of source and destination tiles
        /// </summary>
        internal static int TotalPossible = 4096;

        // fields
        internal int binary;

        // properties
        internal Tile From
        {
            get
            {
                return new Tile(binary >> Tile.bits);
            }
            set
            {
                binary &= Tile.bitMask;
                binary |= value.binary << Tile.bits;
            }
        }
        internal Tile To
        {
            get
            {
                return new Tile(binary & 0x3F);
            }
            set
            {
                binary &= Tile.bitMask << Tile.bits;
                binary |= value.binary;
            }
        }

        // constructors
        internal ChessMove(int binary)
        {
            this.binary = binary;
        }
        internal ChessMove(Tile from, Tile to)
        {
            binary = (from.binary << Tile.bits) | to.binary;
        }

        // methods
        internal static ChessMove Parse(string move)
        {
            if (move[0] == 'O' || move[0] == 'o')
            {
                // TODO castling
            }

            if (move.Length != 4)
                throw new ArgumentException(string.Format("The move string was invalid: {0}", move));

            var fromTile = Tile.Parse(move.Substring(0, 2));
            var toTile = Tile.Parse(move.Substring(2, 2));

            var chessMove = new ChessMove(fromTile, toTile);

            return chessMove;
        }

        // overrides
        public override string ToString()
        {
            return From.ToString() + To.ToString();
        }
    }
}
