using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

/*
==============================
[Piece] - Script placed on every piece in the board.
==============================
*/
public class Piece : MonoBehaviour {
    public List<Move> allowed_moves = new List<Move>(); // List of moves the piece can move, starting from its current position
    public MoveType move_type; // Type of move the piece is going to move
    private Piece castling_tower; // Once we know which tower the king is trying to castle, we save it here

    public List<Coordinate> break_points = new List<Coordinate>(); // Coordinates that will break the piece's direction
    public bool started; // StartOnly MoveType controller, set to true when the piece moves for the first time
    public Square cur_square; // Where is the piece right now
    public Board board;

    public GameObject knightButton;
    public GameObject bishopButton;
    public GameObject rookButton;
    public GameObject queenButton;
    public GameObject Canvas;

    [SerializeField]
    public PieceType piece_type;

    [SerializeField]
    public int team; // Whites = -1, Blacks = 1

    [SerializeField]
    public List<Piece> castling_towers;

    void Start() {
        // Initialize valid moves
        switch (piece_type) {
            case PieceType.Pawn:
                addPawnAllowedMoves();
                break;
            case PieceType.Tower:
                addLinealAllowedMoves();
                break;
            case PieceType.Horse:
                addHorseAllowedMoves();
                break;
            case PieceType.Bishop:
                addDiagonalAllowedMoves();
                break;
            case PieceType.Queen:
                addLinealAllowedMoves();
                addDiagonalAllowedMoves();
                break;
            case PieceType.King:
                addKingAllowedMoves();
                break;
        }
    }

    /*
    ---------------
    Moves related functions
    ---------------
    */
    // Once the user drops the piece, we'll try to move it, if it was dropped in a non-valid square,
    // the piece will be returned to its position
    public void movePiece(Square square) {
        if (board.winner == Winner.Nobody && checkValidMove(square)) {
            // Switch cases for the current move type
            switch (move_type) {
                case MoveType.StartOnly:
                    // If the piece is the king and can castle
                    if (piece_type == PieceType.King && checkCastling(square)) {
                        // Update castling tower's position (depending on where the tower is, we will move it 3 or 2 squares in the "x" axis)
                        if (castling_tower.cur_square.coor.x == 0) {
                            castling_tower.castleTower(castling_tower.cur_square.coor.x + 2);
                        } else {
                            castling_tower.castleTower(castling_tower.cur_square.coor.x - 3);
                        }
                    }
                    break;
                case MoveType.EatMove:
                case MoveType.EatMoveJump:
                    // If the move type involves eating, eat the enemy piece
                    eatPiece(square.holding_piece);
                    break;
                case MoveType.EatEnpassant:
                    // Eat the enemy pawn
                    if (square.holding_piece != null) {
                        eatPiece(square.holding_piece);
                    } else {
                        eatPiece(board.enpassant_pawn_square.holding_piece);
                    }
                    break;
            }

            // Update piece's current square
            cur_square.holdPiece(null);
            square.holdPiece(this);
            cur_square = square;
            if (!started) started = true;

            board.enpassant_square = null;
            board.enpassant_pawn_square = null;

            if (piece_type == PieceType.Pawn) {
                board.fiftyMoveRuleCount = 0;

                if (move_type == MoveType.StartOnly) {
                    Coordinate enpassant_coor = new Coordinate(square.coor.x, square.coor.y - 1 * team);
                    board.enpassant_square = board.getSquareFromCoordinate(enpassant_coor);
                    board.enpassant_pawn_square = square;
                }
            }

            board.cur_turn *= -1;
            board.positions.Add(board);
        } else {
            board.illegalMoveSound.Play();
        }

        // Clear break points & update piece's position
        break_points.Clear();
    }

    public void movePiece(AIMove move) {
        Square square = board.getSquareFromCoordinate(move.end);
        if (board.winner == Winner.Nobody && checkValidMove(square)) {
            // Switch cases for the current move type
            switch (move_type) {
                case MoveType.StartOnly:
                    // If the piece is the king and can castle
                    if (piece_type == PieceType.King && checkCastling(square)) {
                        // Update castling tower's position (depending on where the tower is, we will move it 3 or 2 squares in the "x" axis)
                        if (castling_tower.cur_square.coor.x == 0) {
                            castling_tower.castleTower(castling_tower.cur_square.coor.x + 2);
                        } else {
                            castling_tower.castleTower(castling_tower.cur_square.coor.x - 3);
                        }
                    }
                    break;
                case MoveType.EatMove:
                case MoveType.EatMoveJump:
                    // If the move type involves eating, eat the enemy piece
                    eatPiece(square.holding_piece);
                    break;
                case MoveType.EatEnpassant:
                    // Eat the enemy pawn
                    if (square.holding_piece != null) {
                        eatPiece(square.holding_piece);
                    } else {
                        eatPiece(board.enpassant_pawn_square.holding_piece);
                    }
                    break;
            }

            // Update piece's current square
            cur_square.holdPiece(null);
            square.holdPiece(this);
            cur_square = square;
            if (!started) started = true;

            board.enpassant_square = null;
            board.enpassant_pawn_square = null;

            if (piece_type == PieceType.Pawn) {
                board.fiftyMoveRuleCount = 0;

                if (move_type == MoveType.StartOnly) {
                    Coordinate enpassant_coor = new Coordinate(square.coor.x, square.coor.y - 1 * team);
                    board.enpassant_square = board.getSquareFromCoordinate(enpassant_coor);
                    board.enpassant_pawn_square = square;
                }
            }

            if (move.pType != PieceType.None) {
                piece_type = move.pType;
                allowed_moves.Clear();
                switch (piece_type) {
                    case PieceType.Tower:
                        addLinealAllowedMoves();
                        break;
                    case PieceType.Horse:
                        addHorseAllowedMoves();
                        break;
                    case PieceType.Bishop:
                        addDiagonalAllowedMoves();
                        break;
                    case PieceType.Queen:
                        addLinealAllowedMoves();
                        addDiagonalAllowedMoves();
                        break;
                }
            }

            board.cur_turn *= -1;
            board.positions.Add(board);
        } else {
            board.illegalMoveSound.Play();
        }

        // Clear break points & update piece's position
        break_points.Clear();
    }

    // Get the coordinate starting from this piece position (0, 0)
    public Coordinate getCoordinateMove(Square square) { 
        int coor_x = (square.coor.x - cur_square.coor.x) * team;
        int coor_y = (square.coor.y - cur_square.coor.y) * team;

        return new Coordinate(coor_x, coor_y);
    }

    // Check if the piece can move to the given square
    public bool checkValidMove(Square square) {
        Coordinate coor_move = getCoordinateMove(square);

        for (int i = 0; i < allowed_moves.Count; i++) {
            if (coor_move.x == allowed_moves[i].x && coor_move.y == allowed_moves[i].y) {
                move_type = allowed_moves[i].type;
                switch (move_type) {
                    case MoveType.StartOnly:
                        // If this piece hasn't been moved before, can move to the square or is trying to castle
                        if (!started && checkCanMove(square) && checkCastling(square))
                            return true;
                        break;
                    case MoveType.Move:
                        if (checkCanMove(square)) {
                            return true;
                        }
                        break;
                    case MoveType.EatMove:
                    case MoveType.EatMoveJump:
                        if (checkCanEatMove(square)) {
                            return true;
                        }
                        break;
                    case MoveType.EatEnpassant:
                        if (checkCanEatEnpassant(square)) {
                            return true;
                        }
                        break;
                }
            }
        }
        return false;
    }

    // Check if we move this piece to the given square the king keeps in check mode
    public bool checkValidCheckKingMove(Square square) {
        bool avoids_check = false;

        Piece old_holding_piece = square.holding_piece;
        Square old_square = cur_square;
        
        cur_square.holdPiece(null);
        cur_square = square;
        if (square.holding_piece != null) {
            if (square.holding_piece.piece_type == PieceType.King) {
                cur_square = old_square;
                cur_square.holdPiece(this);
                return true;
            } else {
                board.destroyPiece(square.holding_piece);
            }
        }
        square.holdPiece(this);

        // If my king isn't checked or I can eat the checking piece
        if (!board.isCheckKing(team)) {
            avoids_check = true;
        }

        cur_square = old_square;
        cur_square.holdPiece(this);
        square.holdPiece(old_holding_piece);
        if (old_holding_piece != null) {
            board.pieces[old_holding_piece.team].Add(old_holding_piece);
        }
        return avoids_check;
    }

    // Returns if the piece can move to the given square
    private bool checkCanMove(Square square) {
        Coordinate coor_move = getCoordinateMove(square);

        // If square is free, square isn't further away from the breaking squares and the move won't cause a check
        if (square.holding_piece == null && checkBreakPoint(coor_move) && checkValidCheckKingMove(square)) return true;
        return false;
    }

    // Returns if the piece can eat an enemy piece that is placed in the given square
    private bool checkCanEat(Square square) {
        Coordinate coor_move = getCoordinateMove(square);

        // If square is holding an enemy piece, square isn't further away from the breaking squares and the move won't cause a check
        if (square.holding_piece != null && square.holding_piece.team != team && checkBreakPoint(coor_move) && checkValidCheckKingMove(square)) return true;
        return false;
    }

    // Returns if the piece can eat or move to the given square
    private bool checkCanEatMove(Square square) {
        if (checkCanEat(square) || checkCanMove(square)) return true; 
        return false;
    }

    private bool checkCanEnpassant(Square square) {
        // If square is an enpassant square and the move won't cause a check
        if (square.holding_piece == null && board.enpassant_square == square && checkValidCheckKingMove(square)) return true;
        return false;
    }

    private bool checkCanEatEnpassant(Square square) {
        // Returns if the piece can eat or enpassant the given square
        if (checkCanEat(square) || checkCanEnpassant(square)) return true;
        return false;
    }

    /*
    ---------------
    Break points related functions
    ---------------
    */
    // Checks if the given coordinate isn't far away from the breaking points.
    // Since the given coordinate is related to the current square's position,
    // we'll need to check all the axis possibilities (negatives and positives)
    public bool checkBreakPoint(Coordinate coor) {
        for (int i = 0; i < break_points.Count; i++) {
            if (break_points[i].x == 0 && coor.x == 0){
                if (break_points[i].y < 0 && (coor.y < break_points[i].y)) {
                    return false;
                }
                else if (break_points[i].y > 0 && (coor.y > break_points[i].y)) {
                    return false;
                }
            }
            else if (break_points[i].y == 0 && coor.y == 0){
                if (break_points[i].x > 0 && (coor.x > break_points[i].x)) {
                    return false;
                }
                else if (break_points[i].x < 0 && (coor.x < break_points[i].x)) {
                    return false;
                }
            }
            else if (break_points[i].y > 0 && (coor.y > break_points[i].y)) {
                if (break_points[i].x > 0 && (coor.x > break_points[i].x)) {
                    return false;
                }
                else if (break_points[i].x < 0 && (coor.x < break_points[i].x)) {
                    return false;
                }
            }
            else if (break_points[i].y < 0 && (coor.y < break_points[i].y)){
                if (break_points[i].x > 0 && (coor.x > break_points[i].x)) {
                    return false;
                }
                else if (break_points[i].x < 0 && (coor.x < break_points[i].x)) {
                    return false;
                }
            }
        }
        return true;
    }

    // Add piece's break positions, squares that are further away won't be allowed
    public void addBreakPoint(Square square) {
        Coordinate coor_move = getCoordinateMove(square);

        for (int j = 0; j < allowed_moves.Count ; j++) {
            if (coor_move.x == allowed_moves[j].x && coor_move.y == allowed_moves[j].y) {
                switch (allowed_moves[j].type) {
                    case MoveType.StartOnly:
                    case MoveType.Move:
                    case MoveType.EatEnpassant:
                    case MoveType.EatMove:
                        // If square is holding a piece
                        if (square.holding_piece != null) {
                            break_points.Add(coor_move);
                        }
                        break;
                }
            }
        }   
    }

    /*
    ---------------
    Castling related functions
    ---------------
    */ 
    // Castle this tower with the king, updating its position
    public void castleTower(int coor_x) {
        Coordinate castling_coor = new Coordinate(coor_x, cur_square.coor.y);
        Square square = board.getSquareFromCoordinate(castling_coor);

        cur_square.holdPiece(null);
        square.holdPiece(this);
        cur_square = square;
        if (!started) started = true;
    }

    // Check if the king can make a castle
    private bool checkCastling(Square square) {
        if (piece_type == PieceType.King) {
            Coordinate coor_move = getCoordinateMove(square);
            Coordinate castling_tower_coor;

            if (coor_move.x == 2 * team) {
                castling_tower_coor = new Coordinate(7, square.coor.y);
            } else {
                castling_tower_coor = new Coordinate(0, square.coor.y);
            }

            castling_tower = board.getSquareFromCoordinate(castling_tower_coor).holding_piece;

            if (castling_tower == null || castling_tower.piece_type != PieceType.Tower) {
                return false;
            }

            bool can_castle = board.checkCastlingSquares(cur_square, castling_tower.cur_square, team);

            return (!castling_tower.started && can_castle) ? true : false;
        } else {
            return true;
        }
    }

    // Adds an allowed piece move
    private void addAllowedMove(int coor_x, int coor_y, MoveType type) {
        Move new_move = new Move(coor_x, coor_y, type);
        allowed_moves.Add(new_move);
    }

    // Pawns allowed moves
    public void addPawnAllowedMoves() {
        addAllowedMove(0, 1, MoveType.Move);
        addAllowedMove(0, 2, MoveType.StartOnly);
        addAllowedMove(1, 1, MoveType.EatEnpassant);
        addAllowedMove(-1, 1, MoveType.EatEnpassant);
    }

    // Towers & part of the Queen's alowed moves
    public void addLinealAllowedMoves() {
        for (int coor_x = 1; coor_x < 8; coor_x++) {
            addAllowedMove(coor_x, 0, MoveType.EatMove);
            addAllowedMove(0, coor_x, MoveType.EatMove);
            addAllowedMove(-coor_x, 0, MoveType.EatMove);
            addAllowedMove(0, -coor_x, MoveType.EatMove);
        }
    }

    // Bishops & part of the Queen's alowed moves
    public void addDiagonalAllowedMoves() {
        for (int coor_x = 1; coor_x < 8; coor_x++) {
            addAllowedMove(coor_x, -coor_x, MoveType.EatMove);
            addAllowedMove(-coor_x, coor_x, MoveType.EatMove);
            addAllowedMove(coor_x, coor_x, MoveType.EatMove);
            addAllowedMove(-coor_x, -coor_x, MoveType.EatMove);
        }
    }

    // Horses allowed moves
    public void addHorseAllowedMoves() {
        for (int coor_x = 1; coor_x < 3; coor_x++) {
            for (int coor_y = 1; coor_y < 3; coor_y++) {
                if (coor_y != coor_x) {
                    addAllowedMove(coor_x, coor_y, MoveType.EatMoveJump);
                    addAllowedMove(-coor_x, -coor_y, MoveType.EatMoveJump);
                    addAllowedMove(coor_x, -coor_y, MoveType.EatMoveJump);
                    addAllowedMove(-coor_x, coor_y, MoveType.EatMoveJump);
                }
            }
        }
    }

    // King's allowed moves (castling included)
    public void addKingAllowedMoves() {
        // Castling moves
        addAllowedMove(-2, 0, MoveType.StartOnly);
        addAllowedMove(2, 0, MoveType.StartOnly);

        // Normal moves
        addAllowedMove(0, 1, MoveType.EatMove);
        addAllowedMove(1, 1, MoveType.EatMove);
        addAllowedMove(1, 0, MoveType.EatMove);
        addAllowedMove(1, -1, MoveType.EatMove);
        addAllowedMove(0, -1, MoveType.EatMove);
        addAllowedMove(-1, -1, MoveType.EatMove);
        addAllowedMove(-1, 0, MoveType.EatMove);
        addAllowedMove(-1, 1, MoveType.EatMove);
    }

    /*
    ---------------
    Other functions
    ---------------
    */
    public void setStartSquare(Square square) {
        cur_square = square;
    }

    // Function called when someone eats this piece
    public void eatMe() {
        if (piece_type == PieceType.Tower && started == false) {
            board.getKingPiece(team).castling_towers.Remove(this);
        }

        board.fiftyMoveRuleCount = 0;

        board.destroyPiece(this);
    }

    // Called when this piece is eating an enemy piece
    private void eatPiece(Piece piece) {
        if (piece != null && piece.team != team) {
            piece.eatMe();
        }
    }
}