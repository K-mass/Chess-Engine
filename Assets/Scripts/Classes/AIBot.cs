using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;

public class AIBot : MonoBehaviour {
    private static Dictionary<PieceType, float> pieceValues = new Dictionary<PieceType, float>() {
        {PieceType.Pawn, 1},
        {PieceType.Horse, 3},
        {PieceType.Bishop, 3},
        {PieceType.Tower, 5},
        {PieceType.Queen, 9},
        {PieceType.King, 0}
    };

    private static float positionalMultiplier = 0.4f;
    private static float centreSquareMultiplier = 3;
    private static float semiCentreSquareMultiplier = 2;
    private static float doubledPawnPenalty = 0.3f;
    private static float backwardsPawnPenalty = 0.2f;
    private static float tarraschSupportingRuleValue = 0.3f;
    private static float tarraschBlockingRuleValue = 0.2f;
    private static float passedPawnBonus = 0.2f;
    private static float isolatedPawnPenalty = 0.3f;
    private static float knightBonusPerPawn = 0.04f;
    private static float knightOutpostBonus = 0.1f;
    private static float badBishopPenalty = 0.2f;
    private static float bishopPairBonus = 0.5f;
    private static float fianchettoBonus = 0.1f;
    private static float rookPawnPenalty = 0.05f;
    private static float rookSeventhRankBonus = 0.3f;
    private static float connectedRookBonus = 0.1f;
    private static float rookOpenFileBonus = 0.1f;
    private static float rookSemiOpenFileBonus = 0.05f;
    private static float queenEarlyDevelopMentPenalty = 2;
    private static float valuePerSquareControlled = 0.01f;
    private static int knightAttackingPower = 20;
    private static int bishopAttackingPower = 20;
    private static int rookAttackingPower = 40;
    private static int queenAttackingPower = 80;
    private static float pawnAttackValue = 0.2f;
    private static float noPawnInFrontOfKingPenalty = 0.3f;
    private static float minorPieceDevelopmentPenalty = 0.08f;

    private static int[] attackingWeight = {
        0,
        0,
        50,
        75,
        88,
        94,
        97,
        99
    };

    public static Evaluation maxEvaluation(Board position, float alpha, float beta, int depth) {
        if (depth == 0 || position.winner != Winner.Nobody) {
            return new Evaluation(null, evaluatePosition(position));
        }

        Evaluation maxEvaluation = new Evaluation(null, float.NegativeInfinity);

        List<AIMove> moves = findLegalMoves(position);

        for (int i = 0; i < moves.Count; i++) {
            Board working_position = ObjectExtensions.Copy(position);

            AIMove move = moves[i];

            working_position.getSquareFromCoordinate(move.start).holding_piece.movePiece(move);

            Evaluation evaluation = minEvaluation(working_position, alpha, beta, depth - 1);

            if (evaluation.value > maxEvaluation.value) {
                maxEvaluation = new Evaluation(move, evaluation.value);
            }
            alpha = Math.Max(alpha, evaluation.value);
            if (alpha >= beta) {
                break;
            }
        }
        return maxEvaluation;
    }

    public static Evaluation minEvaluation(Board position, float alpha, float beta, int depth) {
        if (depth == 0 || position.winner != Winner.Nobody) {
            return new Evaluation(null, evaluatePosition(position));
        }   

        Evaluation minEvaluation = new Evaluation(null, float.PositiveInfinity);

        List<AIMove> moves = findLegalMoves(position);

        for (int i = 0; i < moves.Count; i++) {
            Board working_position = ObjectExtensions.Copy(position);

            AIMove move = moves[i];

            working_position.getSquareFromCoordinate(move.start).holding_piece.movePiece(move);

            Evaluation evaluation = maxEvaluation(working_position, alpha, beta, depth - 1);

            if (evaluation.value < minEvaluation.value) {
                minEvaluation = new Evaluation(move, evaluation.value);
            }
            beta = Math.Min(beta, evaluation.value);
            if (alpha >= beta) {
                break;
            }
        }
        return minEvaluation;
    }

    private static List<AIMove> findLegalMoves(Board position) {
        List<AIMove> moves = new List<AIMove>();
        for (int i = 0; i < position.pieces[position.cur_turn].Count; i++) {
            Piece cur_piece = position.pieces[position.cur_turn][i];
            for (int j = 0; j < cur_piece.allowed_moves.Count; j++) {
                Coordinate end = new Coordinate(cur_piece.cur_square.coor.x + cur_piece.allowed_moves[j].x * cur_piece.team, cur_piece.cur_square.coor.y + cur_piece.allowed_moves[j].y * cur_piece.team);
                Square ending_square = position.getSquareFromCoordinate(end);
                if (ending_square != null && cur_piece.checkValidMove(ending_square)) {
                    if (cur_piece.piece_type == PieceType.Pawn && (end.y == 0 || end.y == 7)) {
                        if (ending_square.holding_piece != null) {
                            moves.Insert(0, new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.Horse, cur_piece.team));
                            moves.Insert(0, new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.Bishop, cur_piece.team));
                            moves.Insert(0, new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.Tower, cur_piece.team));
                            moves.Insert(0, new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.Queen, cur_piece.team));
                        } else {
                            moves.Add(new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.Horse, cur_piece.team));
                            moves.Add(new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.Bishop, cur_piece.team));
                            moves.Add(new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.Tower, cur_piece.team));
                            moves.Add(new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.Queen, cur_piece.team));
                        }
                    } else {
                        if (ending_square.holding_piece != null) {
                            moves.Insert(0, new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.None, cur_piece.team));
                        } else {
                            moves.Add(new AIMove(cur_piece.cur_square.coor, end, cur_piece.piece_type, PieceType.None, cur_piece.team));
                        }
                    }
                }
            }
        }
        return moves;
    }

    private static float evaluatePosition(Board position) {
        List<Piece> white_pawns = new List<Piece>();
        List<Piece> black_pawns = new List<Piece>();
        List<Piece> all_pawns = new List<Piece>();

        List<Piece> white_pieces = position.pieces[-1];
        List<Piece> black_pieces = position.pieces[1];
        for (int i = 0; i < white_pieces.Count; i++) {
            if (white_pieces[i].piece_type == PieceType.Pawn) {
                white_pawns.Add(white_pieces[i]);
                all_pawns.Add(white_pieces[i]);
            }
        }
        for (int i = 0; i < black_pieces.Count; i++) {
            if (black_pieces[i].piece_type == PieceType.Pawn) {
                black_pawns.Add(black_pieces[i]);
                all_pawns.Add(black_pieces[i]);
            }
        }

        float position_value = 0;
        float materialValue = 0;
        materialValue += materialCount(position, -1);
        materialValue -= materialCount(position, 1);

        float positional_value = 0;
        positional_value -= kingSafety(position, position.getKingPiece(-1));
        positional_value += kingSafety(position, position.getKingPiece(1));
        positional_value += mobility(-1, position);
        positional_value -= mobility(1, position);
        positional_value += spaceControl(position);
        positional_value += piecePower(-1, position, white_pawns, black_pawns);
        positional_value -= piecePower(1, position, white_pawns, black_pawns);
        positional_value += evaluatePawnStructure(position, all_pawns, -1);
        positional_value -= evaluatePawnStructure(position, all_pawns, 1);

        position_value += materialValue;
        position_value += positional_value * positionalMultiplier;
        return position_value;
    }

    private static float materialCount(Board position, int team) {
        float materialValue = 0;
        for (int i = 0; i < position.pieces[team].Count; i++) {
            materialValue += pieceValues[position.pieces[team][i].piece_type];
        }
        return materialValue;
    }

    private static float kingSafety(Board working_board, Piece king) {
        float kingSafety = 0;
        int attackValue = 0;
        int attackingPiecesCount = 0;
        int valueOfAttacks = 0;
        List<Piece> attacking_pieces = working_board.pieces[king.team * -1];
        for (int i = 0; i < attacking_pieces.Count; i++) {
            int attacked_squares = 0;
            for (int j = king.cur_square.coor.x - 1; j < king.cur_square.coor.x + 1; j++) {
                for (int k = king.team == -1 ? king.cur_square.coor.y + 1 : king.cur_square.coor.y - 1; k < (king.team == -1 ? king.cur_square.coor.y - 2 : king.cur_square.coor.y + 2); k++) {
                    Square square = working_board.getSquareFromCoordinate(new Coordinate(j, k));
                    if (square != null && attacking_pieces[i].checkValidMove(working_board.rank[j].file[k])) {
                        attacked_squares++;
                    }
                }
            }
            if (attacked_squares > 0) {
                attackingPiecesCount++;
                switch (attacking_pieces[i].piece_type) {
                    case PieceType.Horse:
                        valueOfAttacks += attacked_squares * knightAttackingPower;
                        break;
                    case PieceType.Bishop:
                        valueOfAttacks += attacked_squares * bishopAttackingPower;
                        break;
                    case PieceType.Tower:
                        valueOfAttacks += attacked_squares * rookAttackingPower;
                        break;
                    case PieceType.Queen:
                        valueOfAttacks += attacked_squares * queenAttackingPower;
                        break;
                }
            }
        }
        attackValue = valueOfAttacks * attackingWeight[attackingPiecesCount];
        kingSafety += attackValue / 100;
        int pawnStormPawns = 0;
        for (int i = king.cur_square.coor.x - 1; i < king.cur_square.coor.x + 1; i++) {
            for (int j = king.team == -1 ? king.cur_square.coor.y - 1 : king.cur_square.coor.y + 1; j < (king.team == -1 ? king.cur_square.coor.y - 4 : king.cur_square.coor.y + 4); j++) {
                Square square = working_board.getSquareFromCoordinate(new Coordinate(i, j));
                if (square != null && square.holding_piece != null && square.holding_piece.piece_type == PieceType.Pawn && square.holding_piece.team != king.team) {
                    pawnStormPawns++;
                }
            }
        }
        kingSafety += pawnStormPawns * pawnAttackValue;
        int pawnsInFrontOfKing = 0;
        if (king.started) {
            for (int i = king.cur_square.coor.x - 1; i < king.cur_square.coor.x + 1; i++) {
                Square square = working_board.getSquareFromCoordinate(new Coordinate(i, king.team == -1 ? king.cur_square.coor.y - 1 : king.cur_square.coor.y + 1));
                if (square != null && square.holding_piece != null && square.holding_piece.piece_type == PieceType.Pawn && square.holding_piece.team == king.team) {
                    pawnsInFrontOfKing++;
                }
            }
            kingSafety += (3 - pawnsInFrontOfKing) * noPawnInFrontOfKingPenalty;
        }
        return kingSafety;
    }

    private static float mobility(int team, Board working_board) {
        int mobility = 0;
        for (int i = 0; i < working_board.pieces[team].Count; i++) {
            Piece piece = working_board.pieces[team][i];
            if (piece.piece_type == PieceType.Queen) {
                continue;
            }
            for (int j = 0; j < piece.allowed_moves.Count; j++) {
                Square square = working_board.getSquareFromCoordinate(new Coordinate(piece.cur_square.coor.x + piece.allowed_moves[j].x * piece.team, piece.cur_square.coor.y + piece.allowed_moves[j].y * piece.team));
                if (square != null) {
                    Coordinate coor_move = piece.getCoordinateMove(square);
                    if (piece.piece_type == PieceType.Horse) {
                        Square pawn_square1 = working_board.getSquareFromCoordinate(new Coordinate(piece.cur_square.coor.x + 1, piece.cur_square.coor.y + 1 * piece.team));
                        Square pawn_square2 = working_board.getSquareFromCoordinate(new Coordinate(piece.cur_square.coor.x - 1, piece.cur_square.coor.y + 1 * piece.team));
                        try {
                            if (pawn_square1 && pawn_square1.holding_piece != null && pawn_square1.holding_piece.piece_type == PieceType.Pawn && pawn_square1.holding_piece.checkValidCheckKingMove(square) ||
                            pawn_square2 != null && pawn_square1.holding_piece != null && pawn_square2.holding_piece.piece_type == PieceType.Pawn && pawn_square2.holding_piece.checkValidCheckKingMove(square)) {
                                continue;
                            }
                        } catch { }
                    }
                    if (piece.checkBreakPoint(coor_move) && piece.checkValidCheckKingMove(square)) {
                        mobility++;
                    }
                }
            }
        }
        return mobility / 20;
    }

    private static int squareControl(Square square, Board working_board) {
        int attackers = 0;
        for (int i = 0; i < working_board.pieces[-1].Count; i++) {
            for (int j = 0; j < working_board.pieces[-1][i].allowed_moves.Count; j++) {
                if (working_board.pieces[-1][i].allowed_moves[j].x == square.coor.x && working_board.pieces[-1][i].allowed_moves[j].y == square.coor.y) {
                    attackers++;
                }
            }
        }
        for (int i = 0; i < working_board.pieces[1].Count; i++) {
            for (int j = 0; j < working_board.pieces[1][i].allowed_moves.Count; j++) {
                if (working_board.pieces[1][i].allowed_moves[j].x == square.coor.x && working_board.pieces[1][i].allowed_moves[j].y == square.coor.y) {
                    attackers--;
                }
            }
        }
        if (attackers == 0) {
            return 0;
        } else if (attackers > 0) {
            return -1;
        } else {
            return 1;
        }
    }

    private static float spaceControl(Board working_board) {
        float squares_controlled = 0;
        for (int i = 0; i < working_board.rank.Length; i++) {
            for (int j = 0; j < working_board.rank[i].file.Length; j++) {
                Square square = working_board.rank[i].file[j];
                float squareControlMultiplier = 0;
                if (square.coor.x >= 3 && square.coor.x <= 4 && square.coor.y >=3 && square.coor.y <= 4) {
                    squareControlMultiplier = centreSquareMultiplier;
                } else if (square.coor.x >= 2 && square.coor.x <= 5 && square.coor.y >= 2 && square.coor.y <= 5) {
                    squareControlMultiplier = semiCentreSquareMultiplier;
                } else {
                    squareControlMultiplier = 1;
                }
                squares_controlled += squareControl(square, working_board) * squareControlMultiplier;
                if (square.holding_piece != null && squareControlMultiplier != 1) {
                    squares_controlled += square.holding_piece.team * -1 * squareControlMultiplier;
                }
            }
        }
        return squares_controlled * valuePerSquareControlled;
    }

    private static float piecePower(int team, Board working_board, List<Piece> white_pawns, List<Piece> black_pawns) {
        float power = 0;
        bool lightSquaredBishop = false;
        bool darkSquaredBishop = false;
        for (int i = 0; i < working_board.pieces[team].Count; i++) {
            Piece cur_piece = working_board.pieces[team][i];
            switch (cur_piece.piece_type) {
                case PieceType.Horse:
                    power += knightBonusPerPawn * (white_pawns.Count + black_pawns.Count);
                    if (!cur_piece.started) {
                        power -= minorPieceDevelopmentPenalty;
                    }
                    break;
                case PieceType.Bishop:
                    if ((cur_piece.cur_square.coor.x == 1 && working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x - 1, cur_piece.cur_square.coor.y)).holding_piece != null &&
                    working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x - 1, cur_piece.cur_square.coor.y)).holding_piece.piece_type == PieceType.Pawn || 
                    cur_piece.cur_square.coor.x == 6 && working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x + 1, cur_piece.cur_square.coor.y)).holding_piece != null &&
                    working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x + 1, cur_piece.cur_square.coor.y)).holding_piece.piece_type == PieceType.Pawn) &&
                    (cur_piece.team == -1 && cur_piece.cur_square.coor.y == 6 && working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x, cur_piece.cur_square.coor.y - 1)).holding_piece != null && 
                    working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x, cur_piece.cur_square.coor.y - 1)).holding_piece.piece_type == PieceType.Pawn ||
                    cur_piece.team == 1 && cur_piece.cur_square.coor.y == 1 && working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x, cur_piece.cur_square.coor.y + 1)).holding_piece != null &&
                    cur_piece.cur_square.coor.y == 1 && working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x, cur_piece.cur_square.coor.y + 1)).holding_piece.piece_type == PieceType.Pawn)) {
                        power += fianchettoBonus;
                    }
                    if (cur_piece.cur_square.team == -1) {
                        lightSquaredBishop = true;
                    } else {
                        darkSquaredBishop = true;
                    }
                    if (!cur_piece.started) {
                        power -= minorPieceDevelopmentPenalty;
                    }
                    break;
                case PieceType.Tower:
                    bool openFile = true;
                    bool semiOpenFile = true;
                    power -= rookPawnPenalty * (white_pawns.Count + black_pawns.Count);
                    if (cur_piece.team == -1 && cur_piece.cur_square.coor.y == 1 || cur_piece.team == 1 && cur_piece.cur_square.coor.y == 6) {
                        power += rookSeventhRankBonus;
                    }
                    if (cur_piece.team == -1) {
                        for (int j = cur_piece.cur_square.coor.y; j <= 7; j++) {
                            Piece holding_piece = working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x, j)).holding_piece;
                            if (holding_piece != null) {
                                if (holding_piece.piece_type == PieceType.Tower && holding_piece.team == cur_piece.team) {
                                    power += connectedRookBonus;
                                } else if (holding_piece.piece_type == PieceType.Pawn) {
                                    if (holding_piece.team == cur_piece.team) {
                                        openFile = false;
                                        semiOpenFile = false;
                                        break;
                                    } else {
                                        openFile = false;
                                        break;
                                    }
                                } else {
                                    break;
                                }
                            }
                        }
                    } else {
                        for (int j = cur_piece.cur_square.coor.y; j >= 0; j--) {
                            Piece holding_piece = working_board.getSquareFromCoordinate(new Coordinate(cur_piece.cur_square.coor.x, j)).holding_piece;
                            if (holding_piece != null) {
                                if (holding_piece.piece_type == PieceType.Tower && holding_piece.team == cur_piece.team) {
                                    power += connectedRookBonus;
                                } else if (cur_piece.team == -1 && holding_piece.piece_type == PieceType.Pawn) {
                                    if (holding_piece.team == cur_piece.team) {
                                        openFile = false;
                                        semiOpenFile = false;
                                        break;
                                    } else {
                                        openFile = false;
                                        break;
                                    }
                                } else {
                                    break;
                                }
                            }
                        }
                    }
                    if (openFile) {
                        power += rookOpenFileBonus;
                    } else if (semiOpenFile) {
                        power += rookSemiOpenFileBonus;
                    }
                    break;
                case PieceType.Queen:
                    if (cur_piece.cur_square.coor.y >= 3 && cur_piece.cur_square.coor.y <= 4) {
                        power -= queenEarlyDevelopMentPenalty / working_board.positions.Count;
                    }
                    break;
            }
        }
        if (lightSquaredBishop && darkSquaredBishop) {
            power += bishopPairBonus;
        }
        return power;
    }

    private static float evaluatePawnStructure(Board working_board, List<Piece> pawns, int team) {
        float pawnStructureScore = 0;
        for (int i = 0; i < pawns.Count; i++) {
            Piece pawn = pawns[i];
            bool passed = true;
            bool doubled = false;
            bool tarraschSupportingRule = false;
            bool tarraschBlockingRule = false;
            bool isolated = true;
            for (int j = 0; j < 7; j++) {
                Piece holding_piece = working_board.getSquareFromCoordinate(new Coordinate(pawn.cur_square.coor.x, j)).holding_piece;
                if (holding_piece != null) {
                    if (holding_piece.piece_type == PieceType.Pawn && (pawn.team == -1 && j < pawn.cur_square.coor.y || pawn.team == 1 && j > pawn.cur_square.coor.y)) {
                        if (passed) {
                            passed = false;
                            if (holding_piece.team == pawn.team) {
                                doubled = true;
                            }
                        }
                    } else if (holding_piece.piece_type == PieceType.Tower) {
                        if (holding_piece.team == team && team == -1 ^ j < pawn.cur_square.coor.y) {
                            tarraschSupportingRule = true;
                        }
                        if (holding_piece.team != team && team == -1 ^ j > pawn.cur_square.coor.y) {
                            tarraschBlockingRule = true;
                        }
                    }
                }
                Square sideSquare1 = working_board.getSquareFromCoordinate(new Coordinate(pawn.cur_square.coor.x - 1, j));
                if (sideSquare1 != null) {
                    Piece sideHolding_piece1 = sideSquare1.holding_piece;
                    if (sideHolding_piece1 != null) {
                        if (sideHolding_piece1.piece_type == PieceType.Pawn) {
                            if (sideHolding_piece1.team == team) {
                                isolated = false;
                            } else {
                                if (team == -1 && j < pawn.cur_square.coor.y || team == 1 && j > pawn.cur_square.coor.y) {
                                    passed = false;
                                }
                            }
                        } else if (sideHolding_piece1.piece_type == PieceType.Horse && (team == -1 && pawn.cur_square.coor.y <= 5 || pawn.team == 1 && pawn.cur_square.coor.y >= 4)) {
                            pawnStructureScore += knightOutpostBonus;
                        }
                    }
                }

                Square sideSquare2 = working_board.getSquareFromCoordinate(new Coordinate(pawn.cur_square.coor.x + 1, j));
                if (sideSquare2 != null) {
                    Piece sideHoldingPiece2 = sideSquare2.holding_piece;
                    if (sideHoldingPiece2 != null) {
                        if (sideHoldingPiece2.piece_type == PieceType.Pawn) {
                            if (sideHoldingPiece2.team == team) {
                                isolated = false;
                            } else {
                                if (team == -1 && j < pawn.cur_square.coor.y || team == 1 && j > pawn.cur_square.coor.y) {
                                    passed = false;
                                }
                            }
                        } else if (sideHoldingPiece2.piece_type == PieceType.Horse && (team == -1 && pawn.cur_square.coor.y <= 5 || team == 1 && pawn.cur_square.coor.y >= 4)) {
                            pawnStructureScore += knightOutpostBonus;
                        }
                    }
                }
            }
            if (passed) {
                if (tarraschSupportingRule == true) {
                    pawnStructureScore += tarraschSupportingRuleValue * pawn.team * -1;
                }
                if (tarraschBlockingRule == true) {
                    pawnStructureScore -= tarraschBlockingRuleValue * pawn.team * -1;
                }
                pawnStructureScore += passedPawnBonus * pawn.team == -1 ? pawn.cur_square.coor.y : 7 - pawn.cur_square.coor.y;
            }
            if (doubled) {
                pawnStructureScore -= doubledPawnPenalty;
            }
            if (isolated) {
                pawnStructureScore -= isolatedPawnPenalty;
            }
        }
        return pawnStructureScore;
    }
}