using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

namespace ChessChallenge.Example {
    public class EvilBot : IChessBot
    {
        readonly bool stockfish = false;
        readonly int depth = 4;
        public Move Think(Board board, Timer timer)
        {
            if (stockfish) {
                return Stockfish(board);
            }

            Move[] allMoves = board.GetLegalMoves();

            // Get the checkmate if available
            foreach (Move move in allMoves) {
                if (MoveIsCheckmate(board, move)) return move;
            }

            // Order for better AlphaBeta
            allMoves = allMoves.OrderBy(move => 8 - (int)move.CapturePieceType).ToArray();
            
            int bestEval = board.IsWhiteToMove ? int.MinValue : int.MaxValue;
            List<Move> bestMoves = new();
            for (int i = 0; i < allMoves.Length; i++) {
                int eval = 0;
                Move current = allMoves[i];
                board.MakeMove(current);
                eval += AlphaBeta(board, this.depth, int.MinValue, int.MaxValue, board.IsWhiteToMove);
                board.UndoMove(current);
                if (bestMoves.Count() == 0) {
                    bestMoves.Add(current);
                    bestEval = eval;
                } else if ((board.IsWhiteToMove && eval > bestEval) || (!board.IsWhiteToMove && eval < bestEval)) {
                    bestMoves.Clear();
                    bestMoves.Add(current);
                    bestEval = eval;
                } else if (eval == bestEval) {
                    bestMoves.Add(current);
                }
            }
            Random random = new();
            int randomIndex = random.Next(0, bestMoves.Count());
            Move randomBestMove = bestMoves[randomIndex];
            return randomBestMove;
        }

        // Test if this move gives checkmate
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }

        // Position evaluation algorithm
        int EvaluatePosition(Board board) {
            // Piece values: null, pawn, knight, bishop, rook, queen, king
            int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

            int eval = 0;
            eval += pieceValues[(int)PieceType.Bishop]* Convert.ToString((long)board.GetPieceBitboard(PieceType.Bishop, true), 2).Count(c => c == '1');
            eval += pieceValues[(int)PieceType.Knight] * Convert.ToString((long)board.GetPieceBitboard(PieceType.Knight, true), 2).Count(c => c == '1');
            eval += pieceValues[(int)PieceType.Pawn] * Convert.ToString((long)board.GetPieceBitboard(PieceType.Pawn, true), 2).Count(c => c == '1');
            eval += pieceValues[(int)PieceType.Queen] * Convert.ToString((long)board.GetPieceBitboard(PieceType.Queen, true), 2).Count(c => c == '1');
            eval += pieceValues[(int)PieceType.Rook] * Convert.ToString((long)board.GetPieceBitboard(PieceType.Rook, true), 2).Count(c => c == '1');
            eval -= pieceValues[(int)PieceType.Bishop]* Convert.ToString((long)board.GetPieceBitboard(PieceType.Bishop, false), 2).Count(c => c == '1');
            eval -= pieceValues[(int)PieceType.Knight] * Convert.ToString((long)board.GetPieceBitboard(PieceType.Knight, false), 2).Count(c => c == '1');
            eval -= pieceValues[(int)PieceType.Pawn] * Convert.ToString((long)board.GetPieceBitboard(PieceType.Pawn, false), 2).Count(c => c == '1');
            eval -= pieceValues[(int)PieceType.Queen] * Convert.ToString((long)board.GetPieceBitboard(PieceType.Queen, false), 2).Count(c => c == '1');
            eval -= pieceValues[(int)PieceType.Rook] * Convert.ToString((long)board.GetPieceBitboard(PieceType.Rook, false), 2).Count(c => c == '1');

            return eval;
        }

        // Alpha-beta pruning algorithm
        public int AlphaBeta(Board node, int depth, int alpha, int beta, bool maximizingPlayer)
        {
            Move[] childNodes = node.GetLegalMoves();
            childNodes = childNodes.OrderBy(move => 8 - (int)move.CapturePieceType).ToArray();
            if (depth == 0 || childNodes.Length == 0)
            {
                return EvaluatePosition(node);
            }
            if (maximizingPlayer) {
                int value = int.MinValue;
                foreach (Move child in childNodes) {
                    node.MakeMove(child);
                    value = Math.Max(value, AlphaBeta(node, depth - 1, alpha, beta, false));
                    node.UndoMove(child);
                    alpha = Math.Max(alpha, value);
                    if (beta <= alpha) {
                        break;
                    }
                }
                return value;
            } else {
                int value = int.MaxValue;
                foreach (Move child in childNodes) {
                    node.MakeMove(child);
                    value = Math.Min(value, AlphaBeta(node, depth - 1, alpha, beta, true));
                    node.UndoMove(child);
                    beta = Math.Min(beta, value);
                    if (beta <= alpha) {
                        break;
                    }
                }
                return value;
            }
        }

        private Move Stockfish(Board board)
        {
            string stockfishPath = "C:\\Users\\david\\Documents\\github\\stockfish\\stockfish-windows-x86-64-avx2.exe"; // Set the correct path to the Stockfish binary.

            // Start the Stockfish process.
            Process stockfishProcess = new();
            stockfishProcess.StartInfo.FileName = stockfishPath;
            stockfishProcess.StartInfo.UseShellExecute = false;
            stockfishProcess.StartInfo.RedirectStandardInput = true;
            stockfishProcess.StartInfo.RedirectStandardOutput = true;
            stockfishProcess.Start();

            // Send commands to Stockfish and read its responses.
            string fen = board.GetFenString();
            stockfishProcess.StandardInput.WriteLine("isready");
            stockfishProcess.StandardInput.WriteLine("ucinewgame");
            //option name UCI_LimitStrength type check default false
            stockfishProcess.StandardInput.WriteLine("setoption name UCI_LimitStrength value true");
            //option name UCI_Elo type spin default 1320 min 1320 max 3190
            stockfishProcess.StandardInput.WriteLine("setoption name UCI_Elo value 1320");
            stockfishProcess.StandardInput.WriteLine("isready");
            stockfishProcess.StandardInput.WriteLine("position fen " + fen);
            stockfishProcess.StandardInput.WriteLine("go movetime 1000");

            // Process the output to get the suggested move.
            string bestMove = "";
            string outputLine;
            while ((outputLine = stockfishProcess.StandardOutput.ReadLine()) != null)
            {
                if (outputLine.StartsWith("bestmove")) {
                    bestMove = outputLine.Split(' ')[1];
                    break;
                }
            }

            // Close the process.
            stockfishProcess.StandardInput.WriteLine("quit");
            stockfishProcess.WaitForExit();
            stockfishProcess.Close();

            Move move = new(bestMove, board);

            return move;
        }
    }
}