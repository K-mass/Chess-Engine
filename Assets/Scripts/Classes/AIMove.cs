public class AIMove {
    public readonly Coordinate start;
    public readonly Coordinate end;
    public readonly PieceType movingPieceType;
    public MoveCategory moveType;
    public PieceType pType;
    public readonly int team;

    public AIMove(Coordinate start, Coordinate end, PieceType movingPieceType, PieceType pType, int team) {
        this.start = start;
        this.end = end;
        this.pType = pType;
        this.movingPieceType = movingPieceType;
        this.team = team;
    }

    public AIMove(Coordinate start, Coordinate end, PieceType movingPieceType, PieceType pType, MoveCategory moveType, int team) {
        this.start = start;
        this.end = end;
        this.pType = pType;
        this.moveType = moveType;
        this.movingPieceType = movingPieceType;
        this.team = team;
    }
}