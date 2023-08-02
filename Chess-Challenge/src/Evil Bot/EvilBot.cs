using System;
using System.Collections.Generic;
using ChessChallenge.API;

public class EvilBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return Think(board, timer, 0);
    }

    private Move Think(Board board, Timer timer, int lookahead)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Get the checkmate if available
        foreach (Move move in allMoves) {
            if (MoveIsCheckmate(board, move)) return move;
        }

        int highestScore = -1000;
        List<Move> bestMoves = new List<Move>();
        for (int i = 0; i < allMoves.Length; i++) {
            int moveScore = 0;
            Move current = allMoves[i];
            moveScore = current.IsCapture ? moveScore + 10 + ((int)current.CapturePieceType) : moveScore;
            moveScore = current.MovePieceType == PieceType.King || current.MovePieceType == PieceType.Queen ? moveScore - 5 : moveScore;
            moveScore = current.IsCastles ? moveScore + 5 : moveScore;
            moveScore = current.IsPromotion ? moveScore + 5 + ((int)current.PromotionPieceType) : moveScore - 5;
            moveScore = current.TargetSquare.Rank == 3 && current.TargetSquare.File == 3 ? moveScore + 10 : moveScore;
            moveScore = current.TargetSquare.Rank == 3 && current.TargetSquare.File == 4 ? moveScore + 10 : moveScore;
            moveScore = current.TargetSquare.Rank == 4 && current.TargetSquare.File == 3 ? moveScore + 10 : moveScore;
            moveScore = current.TargetSquare.Rank == 4 && current.TargetSquare.File == 4 ? moveScore + 10 : moveScore;
            board.MakeMove(current);
            moveScore = board.IsInCheck() ? moveScore + 10 : moveScore;
            if (board.IsDraw()) {
                moveScore = moveScore - 100;
            } else if (board.IsInCheckmate()) {
                moveScore = moveScore + 100;
            } else if (board.IsInsufficientMaterial()) {
                moveScore = moveScore - 100;
            } else if (board.IsRepeatedPosition()) {
                moveScore = moveScore - 100;
            } else if (board.GetLegalMoves().Length == 0) {
                moveScore = moveScore + 100;
            } else if (moveScore > highestScore * .9 && lookahead < 3) {
                Move result = Think(board, timer, lookahead + 1);
                moveScore = MoveIsCheckmate(board, result) ? moveScore - 100 : moveScore;
                moveScore = MoveIsInsufficientMaterial(board, result) ? moveScore - 100 : moveScore;
                moveScore = MoveIsRepeatedPosition(board, result) ? moveScore - 100 : moveScore;
                moveScore = result.IsCapture ? moveScore - 20 - ((int)result.CapturePieceType) : moveScore;
                board.MakeMove(result);
                moveScore = board.IsInCheck() ? moveScore - 10 : moveScore + 5;
                board.UndoMove(result);
            }
            board.UndoMove(current);
            if (bestMoves.Count == 0) {
                bestMoves.Add(current);
                highestScore = moveScore;
            } else if (moveScore > highestScore) {
                bestMoves.Clear();
                bestMoves.Add(current);
                highestScore = moveScore;
            } else if (moveScore == highestScore) {
                bestMoves.Add(current);
            }
        }
        Random random = new Random();
        int randomIndex = random.Next(0, bestMoves.Count);
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

    // Test if this move causes insufficient material
    bool MoveIsInsufficientMaterial(Board board, Move move)
    {
        board.MakeMove(move);
        bool isInsufficient = board.IsInsufficientMaterial();
        board.UndoMove(move);
        return isInsufficient;
    }

    // Test if this move causes repetition
    bool MoveIsRepeatedPosition(Board board, Move move)
    {
        board.MakeMove(move);
        bool isRepeated = board.IsRepeatedPosition();
        board.UndoMove(move);
        return isRepeated;
    }
}