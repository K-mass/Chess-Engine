public class Evaluation {
    public readonly AIMove move;
    public readonly float value;
    public readonly PieceType ptype;

    public Evaluation(AIMove move, float value) {
        this.move = move;
        this.value = value;
    }
}
