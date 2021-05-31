using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
==============================
[DragAndDrop] - Script placed on every piece in the board.
==============================
*/
class DragAndDrop : MonoBehaviour {
    private bool dragging;
    private float distance;
    private Piece this_piece;
    public bool aiMoving = false;
    float upElapsedTime;
    float moveElapsedTime;
    private Square square;
    private PieceType promotion_type = PieceType.None;

    private Vector3 startPosition;
    private Vector3 targetUpPos;
    private Vector3 targetMovePos;

    private bool upPosReached;
    private bool movePosReached;

    [SerializeField]
    private Board board;

    void Start() {
        this_piece = GetComponent<Piece>(); // Get piece's component
    }

    public void moveToSquare(Square target, PieceType pType) {
        Vector3 targetPos = target.gameObject.transform.position;

        aiMoving = true;

        upElapsedTime = 0;
        moveElapsedTime = 0;

        upPosReached = false;
        movePosReached = false;

        startPosition = transform.position;
        targetUpPos = new Vector3(transform.position.x, 2.7f, transform.position.z);
        targetMovePos = new Vector3(targetPos.x, 2.7f, targetPos.z);

        promotion_type = pType;
        square = target;
    }

    void Update() {
        if (aiMoving) {
            if (!upPosReached) {
                upElapsedTime += Time.deltaTime;
                transform.position = Vector3.Lerp(startPosition, targetUpPos, upElapsedTime / 0.2f);

                if (Mathf.Approximately((transform.position - targetUpPos).sqrMagnitude, 0)) {
                    upPosReached = true;
                }
            } else if (!movePosReached) {
                moveElapsedTime += Time.deltaTime;
                transform.position = Vector3.Lerp(targetUpPos, targetMovePos, moveElapsedTime / 1);

                if (Mathf.Approximately((transform.position - targetMovePos).sqrMagnitude, 0)) {
                    movePosReached = true;
                }
            } else {
                aiMoving = false;
                Piece old_holding_piece = square.holding_piece;

                if (old_holding_piece == null) {
                    board.moveSound.Play();
                } else {
                    board.captureSound.Play();
                }
                Square old_square = this_piece.cur_square;
                Square closest_square = board.getClosestSquare(transform.position);
                this_piece.movePiece(square);
                if (old_holding_piece != null) {
                    Destroy(old_holding_piece.gameObject);
                }

                if (promotion_type != PieceType.None) {
                    this_piece.cur_square.holdPiece(null);
                    this_piece.eatMe();

                    board.promote(square, promotion_type, 1);
                    Destroy(this_piece.gameObject);
                }
                transform.position = new Vector3(this_piece.cur_square.coor.pos.x, transform.position.y, this_piece.cur_square.coor.pos.z);
                transform.rotation = new Quaternion(0, 0, 0, 0);

                foreach (KeyValuePair<int, List<Piece>> entry in board.pieces) {
                    for (int i = 0; i < board.pieces[entry.Key].Count; i++) {
                        if (board.pieces[entry.Key][i].piece_type == PieceType.Tower) {
                            board.pieces[entry.Key][i].transform.position = new Vector3(board.pieces[entry.Key][i].cur_square.coor.pos.x, board.pieces[entry.Key][i].transform.position.y, board.pieces[entry.Key][i].cur_square.coor.pos.z);
                            board.pieces[entry.Key][i].transform.rotation = new Quaternion(0, 0, 0, 0);
                        }
                    }
                }

                board.changeTurn();
            }

        } else if (dragging) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 rayPoint = ray.GetPoint(distance);

            // Update piece's dragging position, we try to place it as close as we can to the mouse
            transform.position = new Vector3(rayPoint.x - 0.5f, 2.7f, rayPoint.z);
            transform.rotation = new Quaternion(0, 0, 0, 0);

            // Hover the square this piece could go id we drop it
            if (board.use_hover) {
                Square closest_square = board.getClosestSquare(transform.position);
                board.hoverClosestSquare(closest_square);
            }

            if (board.isGameInProgress == false && Input.GetKeyDown(KeyCode.Delete)) {
                if (this_piece.piece_type == PieceType.Tower && this_piece.started == false) {
                    board.getKingPiece(this_piece.team).castling_towers.Remove(this_piece);
                }
                if (board.use_hover) board.resetHoveredSquares();

                this_piece.cur_square.holdPiece(null);
                board.destroyPiece(this_piece);
                Destroy(this.gameObject);
            }
        } else if (board.isGameInProgress == false && Input.GetKeyDown(KeyCode.Escape)) {
            foreach (KeyValuePair<int, List<Piece>> entry in board.pieces) {
                List<Piece> team_pieces = board.pieces[entry.Key];
                for (int i = 0; i < team_pieces.Count; i++) {
                    Destroy(team_pieces[i].gameObject);
                    team_pieces[i].cur_square.holdPiece(null);
                    board.destroyPiece(team_pieces[i]);
                }
            }
        }
    }

    void OnMouseDown() {
        // If it's my turn
        if (board.cur_turn == this_piece.team || !board.isGameInProgress) {
            GetComponent<Rigidbody>().isKinematic = true;
            // Set distance between the mouse & this piece
            distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (board.use_hover && board.isGameInProgress) {
                board.hoverValidSquares(this_piece);
            }
            dragging = true; // Start dragging
        }
    }

    public void setBoard(Board newBoard) {
        board = newBoard;
    }

    void OnMouseUp() {
        if (dragging) {
            GetComponent<Rigidbody>().isKinematic = false;
            // Get closest square & try to move the piece to it
            Square closest_square = board.getClosestSquare(transform.position);
            if (board.isGameInProgress) {
                StartCoroutine(updatePiece(this_piece, closest_square));
            } else {
                this_piece.cur_square.holdPiece(null);
                this_piece.cur_square = closest_square;
                if (closest_square.holding_piece != null) {
                    if (closest_square.holding_piece.piece_type == PieceType.Tower && closest_square.holding_piece.started == false) {
                        board.getKingPiece(closest_square.holding_piece.team).castling_towers.Remove(closest_square.holding_piece);
                    }
                    board.destroyPiece(closest_square.holding_piece);
                    Destroy(closest_square.holding_piece.gameObject);
                }
                closest_square.holdPiece(this_piece);
            }
            transform.position = new Vector3(this_piece.cur_square.coor.pos.x, transform.position.y, this_piece.cur_square.coor.pos.z);
            transform.rotation = new Quaternion(0, 0, 0, 0);

            foreach (KeyValuePair<int, List<Piece>> entry in board.pieces) {
                for (int i = 0; i < board.pieces[entry.Key].Count; i++) {
                    if (board.pieces[entry.Key][i].piece_type == PieceType.Tower) {
                        board.pieces[entry.Key][i].transform.position = new Vector3(board.pieces[entry.Key][i].cur_square.coor.pos.x, board.pieces[entry.Key][i].transform.position.y, board.pieces[entry.Key][i].cur_square.coor.pos.z);
                        board.pieces[entry.Key][i].transform.rotation = new Quaternion(0, 0, 0, 0);
                    }
                }
            }

            if (board.use_hover) board.resetHoveredSquares();
            dragging = false; // Stop dragging
        }
    }

    private IEnumerator updatePiece(Piece piece, Square square) {
        Square old_square = piece.cur_square;
        Piece old_holding_piece = square.holding_piece;

        piece.movePiece(square);

        if (old_square != piece.cur_square) {
            if (old_holding_piece == null) {
                board.moveSound.Play();
            } else {
                board.captureSound.Play();
                Destroy(old_holding_piece.gameObject);
            }
            if (piece.piece_type == PieceType.Pawn && (square.coor.y == 0 && piece.team == -1 || square.coor.y == 7 && piece.team == 1)) {
                board.horseButton = Instantiate(board.button, new Vector3(0, 0, 0), Quaternion.identity, board.Canvas.transform);
                board.setupButton(board.horseButton, "Promote to knight", new Vector3(-500, 90, 0), PieceType.Horse, piece.team, piece.cur_square);
                board.bishopButton = Instantiate(board.button, new Vector3(0, 0, 0), Quaternion.identity, board.Canvas.transform);
                board.setupButton(board.bishopButton, "Promote to bishop", new Vector3(-500, 45, 0), PieceType.Bishop, piece.team, piece.cur_square);
                board.towerButton = Instantiate(board.button, new Vector3(0, 0, 0), Quaternion.identity, board.Canvas.transform);
                board.setupButton(board.towerButton, "Promote to rook", new Vector3(-500, 0, 0), PieceType.Tower, piece.team, piece.cur_square);
                board.queenButton = Instantiate(board.button, new Vector3(0, 0, 0), Quaternion.identity, board.Canvas.transform);
                board.setupButton(board.queenButton, "Promote to queen", new Vector3(-500, -45, 0), PieceType.Queen, piece.team, piece.cur_square);

                piece.cur_square.holdPiece(null);
                piece.eatMe();
                Destroy(piece.gameObject);

                yield break;
            }

            yield return new WaitForSeconds(1);
            board.changeTurn();
        }
    }
}