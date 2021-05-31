
/*
==============================
[MoveType] - Types of piece moves
==============================
*/
public enum MoveType {
    StartOnly, // Allowed move until the piece is moved the first time
    Move, //Enpassant eat or standard move
    EatEnpassant, // Allowed move only if the piece can eat an enemy piece
    EatMove, // Piece can move or eat
    EatMoveJump, // No "break" restrictions, piece can move except if a team's piece is already in this coordinate
}