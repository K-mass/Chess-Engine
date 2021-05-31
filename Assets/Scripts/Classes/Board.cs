using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

/*
==============================
[Board] - Main script, controls the game
==============================
*/
public class Board : MonoBehaviour {
    private List<Square> hovered_squares = new List<Square>(); // List squares to hover
    private Square closest_square; // Current closest square when dragging a piece
    private int cur_theme = 2;

    public int cur_turn = -1; // -1 = whites; 1 = blacks
    public Dictionary<int, Piece> checking_pieces = new Dictionary<int, Piece>(); // Which piece is checking the king (key = team)
    public List<Board> positions = new List<Board>();
    public Square enpassant_square; // Coordinates that are enpassant squares
    public Square enpassant_pawn_square;
    public int fiftyMoveRuleCount = 0;
    public bool isGameInProgress;
    public GameObject setupPosButton;

    public AIBot aiBot;

    // UI variables
    public bool use_hover; // Hover valid moves & closest square
    public bool rotate_camera; // Enable/disable camera rotation

    public GameObject gameTxt;
    public GameObject Canvas;

    public GameObject whitePawn;
    public GameObject whiteKnight;
    public GameObject whiteBishop;
    public GameObject whiteRook;
    public GameObject whiteQueen;
    public GameObject whiteKing;

    public GameObject blackPawn;
    public GameObject blackKnight;
    public GameObject blackBishop;
    public GameObject blackRook;
    public GameObject blackQueen;
    public GameObject blackKing;

    public GameObject horseButton;
    public GameObject bishopButton;
    public GameObject towerButton;
    public GameObject queenButton;

    public GameObject setupWhitePawn;
    public GameObject setupWhiteKnight;
    public GameObject setupWhiteBishop;
    public GameObject setupWhiteRook;
    public GameObject setupWhiteQueen;
    public GameObject setupWhiteKing;

    public GameObject setupBlackPawn;
    public GameObject setupBlackKnight;
    public GameObject setupBlackBishop;
    public GameObject setupBlackRook;
    public GameObject setupBlackQueen;
    public GameObject setupBlackKing;

    public GameObject button;

    public AudioSource moveSound;
    public AudioSource captureSound;
    public AudioSource illegalMoveSound;
    public AudioSource startSound;

    public Winner winner = Winner.Nobody;

    [SerializeField]
    MainCamera main_camera;

    [SerializeField]
    Material square_hover_mat; // Piece's valid squares material

    [SerializeField]
    Material square_closest_mat; // Piece's closest square material

    [SerializeField]
    List<Theme> themes = new List<Theme>();

    [SerializeField]
    List<Renderer> board_sides = new List<Renderer>();

    [SerializeField]
    List<Renderer> board_corners = new List<Renderer>();

    public SquareFile[] rank;

    [SerializeField]
    public List<Piece> white_pieces = new List<Piece>();

    [SerializeField]
    public List<Piece> black_pieces = new List<Piece>();

    public Dictionary<int, List<Piece>> pieces = new Dictionary<int, List<Piece>>(); // List of all white pieces in the game (16)

    void Start() {
        pieces.Add(-1, white_pieces);
        pieces.Add(1, black_pieces);

        setBoardTheme();
        addSquareCoordinates(); // Add "local" coordinates to all squares
        setStartPiecesCoor(); // Update all piece's coordinate
        startSound.Play();
    }

    /*
    ---------------
    Squares related functions
    ---------------
    */ 
    // Returns closest square to the given position
    public Square getClosestSquare(Vector3 pos) {
        Square square = rank[0].file[0];
        float closest = Vector3.Distance(pos, rank[0].file[0].coor.pos);

        for (int i = 0; i < rank.Length; i++) {
            for (int j = 0; j < rank[i].file.Length; j++) {
                float distance = Vector3.Distance(pos, rank[i].file[j].coor.pos);

                if (distance < closest) {
                    square = rank[i].file[j];
                    closest = distance;
                }
            }
        }
        return square;
    }

    // Returns the square that is at the given coordinate (local position in the board)
    public Square getSquareFromCoordinate(Coordinate coor) {
        if (coor.x >= 0 && coor.x <= 7 && coor.y >= 0 && coor.y <= 7) {
            return rank[coor.x].file[coor.y];
        }
        return null;
    }

    // Hover piece's closest square
    public void hoverClosestSquare(Square square) {
        if (closest_square) closest_square.unHoverSquare();
        square.hoverSquare(themes[cur_theme].square_closest);
        closest_square = square;
    }

    // Hover all the piece's allowed moves squares
    public void hoverValidSquares(Piece piece) {
        addPieceBreakPoints(piece);

        for (int i = 0; i < piece.allowed_moves.Count; i++) {
            Coordinate ending_coor = new Coordinate(piece.cur_square.coor.x + piece.allowed_moves[i].x * piece.team, piece.cur_square.coor.y + piece.allowed_moves[i].y * piece.team);
            Square ending_square = getSquareFromCoordinate(ending_coor);

            if (ending_square != null && piece.checkValidMove(ending_square)) {
                ending_square.hoverSquare(themes[cur_theme].square_hover);
                hovered_squares.Add(ending_square);
            }
        }
    }

    // Once the piece is dropped, reset all squares materials to the default
    public void resetHoveredSquares() {
        for (int i = 0; i < hovered_squares.Count ; i++) {
            hovered_squares[i].resetMaterial();
        }
        hovered_squares.Clear();
        closest_square.resetMaterial();
        closest_square = null;
    }

    // If the king is trying to castle with a tower, we'll check if an enemy piece is targeting any square
    // between the king and the castling tower
    public bool checkCastlingSquares(Square square1, Square square2, int castling_team) {
        List<Square> castling_squares = new List<Square>();

        if (square1.coor.x < square2.coor.x) {
            for (int i = square1.coor.x; i < square2.coor.x; i++) {
                Coordinate coor = new Coordinate(i, square1.coor.y);
                castling_squares.Add(getSquareFromCoordinate(coor));
            }
        } else {
            for (int i = square1.coor.x; i > square2.coor.x; i--) {
                Coordinate coor = new Coordinate(i, square1.coor.y);
                castling_squares.Add(getSquareFromCoordinate(coor));
            }
        }
        for (int i = 0; i < pieces[castling_team * -1].Count; i++) {
            addPieceBreakPoints(pieces[castling_team * -1][i]);
            for (int j = 0; j < castling_squares.Count; j++) {
                if (castling_squares[j].holding_piece != null && castling_squares[j].holding_piece.piece_type != PieceType.King || pieces[castling_team * -1][i].checkValidMove(castling_squares[j])) return false;
            }
        }

        return true;
    }

    // Set start square's local coordinates & its current position
    private void addSquareCoordinates() {
        for (int i = 0; i < rank.Length; i++) {
            for (int j = 0; j < rank[i].file.Length; j++) {
                rank[i].file[j].coor = new Coordinate(i, j);
                rank[i].file[j].coor.pos = new Vector3(rank[i].file[j].transform.position.x - 0.5f, rank[i].file[j].transform.position.y, rank[i].file[j].transform.position.z - 0.5f);
                if (rank[i].file[j].team == -1) rank[i].file[j].GetComponent<Renderer>().material = themes[cur_theme].square_white;
                else if (rank[i].file[j].team == 1) rank[i].file[j].GetComponent<Renderer>().material = themes[cur_theme].square_black;
                rank[i].file[j].start_mat = rank[i].file[j].GetComponent<Renderer>().material;
            }
        }
    }

    private bool checkThreefoldRepetition() {
        int positionsRepeated = 1;
        Board this_position = positions[positions.Count - 1];

        for (int i = 0; i < positions.Count - 1; i++) {
            if (isPositionSame(this_position, positions[i])) {
                positionsRepeated++;
            }
        }

        if (positionsRepeated >= 3) {
            return true;
        }
        return false;
    }

    private bool isPositionSame(Board position1, Board position2) {
        foreach (KeyValuePair<int, List<Piece>> entry in position1.pieces) {
            if (position1.pieces[entry.Key].Count != position2.pieces[entry.Key].Count) {
                return false;
            }
            for (int i = 0; i < position1.pieces[entry.Key].Count; i++) {
                if (position1.pieces[entry.Key][i].piece_type != position2.pieces[entry.Key][i].piece_type) {
                    return false;
                }
            }
            Piece king1 = position1.getKingPiece(entry.Key);
            Piece king2 = position2.getKingPiece(entry.Key);
            bool can_castle_short1 = false;
            bool can_castle_long1 = false;
            bool can_castle_short2 = false;
            bool can_castle_long2 = false;
            Piece tower1_1 = position1.getSquareFromCoordinate(new Coordinate(0, king1.cur_square.coor.y)).holding_piece;
            can_castle_short1 = (!king1.started && tower1_1 != null && tower1_1.piece_type == PieceType.Tower && tower1_1.team == entry.Key && !tower1_1.started) ? true : false;
            Piece tower1_2 = position1.getSquareFromCoordinate(new Coordinate(0, king2.cur_square.coor.y)).holding_piece;
            can_castle_short2 = (!king2.started && tower1_2 != null && tower1_2.piece_type == PieceType.Tower && tower1_2.team == entry.Key && !tower1_2.started) ? true : false;

            if (can_castle_short1 != can_castle_short2) {
                return false;
            }

            Piece tower2_1 = position1.getSquareFromCoordinate(new Coordinate(7, king1.cur_square.coor.y)).holding_piece;
            can_castle_long1 = (!king1.started && tower2_1 != null && tower2_1.piece_type == PieceType.Tower && tower2_1.team == entry.Key && !tower2_1.started) ? true : false;
            Piece tower2_2 = position1.getSquareFromCoordinate(new Coordinate(7, king2.cur_square.coor.y)).holding_piece;
            can_castle_long2 = (!king2.started && tower2_2 != null && tower2_2.piece_type == PieceType.Tower && tower2_2.team == entry.Key && !tower2_2.started) ? true : false;

            if (can_castle_long1 != can_castle_long2) {
                return false;
            }
        }
        return true;
    }

    private bool isInsufficientMaterial(int team) {
        int bishopCount = 0;
        int knightCount = 0;

        for (int i = 0; i < pieces[team].Count; i++) {
            switch (pieces[team][i].piece_type) {
                case PieceType.Bishop:
                    bishopCount++;
                    break;
                case PieceType.Horse:
                    knightCount++;
                    break;
                case PieceType.Queen:
                case PieceType.Tower:
                case PieceType.Pawn:
                    return false;
            }
        }

        if (bishopCount >= 2 || knightCount >= 3 || bishopCount == 1 && knightCount == 1) {
            return false;
        }
        return true;
    }

    /*
    ---------------
    Pieces related functions
    ---------------
    */
    // Add pieces squares that are breaking the given piece's allowed positions
    public void addPieceBreakPoints(Piece piece) {
        piece.break_points.Clear();
        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 8; j++) {
                piece.addBreakPoint(rank[i].file[j]);
            }
        }
    }

    // Check if the king's given team is in check
    public bool isCheckKing(int team) {
        Piece king = getKingPiece(team);

        List<Piece> team_pieces = pieces[team * -1];

        for (int i = 0; i < team_pieces.Count; i++) {
            addPieceBreakPoints(team_pieces[i]);
            if (team_pieces[i].checkValidMove(king.cur_square)) {
                checking_pieces[team] = team_pieces[i];
                return true;
            }
        }
        return false;
    }

    // Check if the given team lost
    public void isCheckMate(int team) {
        if (!checkValidMoves(team)) {
            if (isCheckKing(team)) {
                winner = team == -1 ? Winner.Black: Winner.White;
            } else {
                winner = Winner.Draw;
            }
        }
    }

    public bool checkValidMoves(int team) {
        List<Piece> team_pieces = pieces[team];

        for (int i = 0; i < team_pieces.Count; i++) {
            Piece cur_piece = team_pieces[i];
            for (int j = 0; j < cur_piece.allowed_moves.Count; j++) {
                Coordinate ending_coor = new Coordinate(cur_piece.cur_square.coor.x + cur_piece.allowed_moves[j].x, cur_piece.cur_square.coor.y + cur_piece.allowed_moves[j].y);
                Square ending_square = getSquareFromCoordinate(ending_coor);

                if (ending_square != null && cur_piece.checkValidMove(ending_square)) {
                    return true;
                }
            }
        }
        return false;
    }

    // Get king's given team
    public Piece getKingPiece(int team) {
        List<Piece> team_pieces = pieces[team];

        for (int i = 0; i < team_pieces.Count; i++) {
            if (team_pieces[i].piece_type == PieceType.King) {
                return team_pieces[i];
            }
        }
        return null;
    }

    // Remove the given piece from the pieces list
    public void destroyPiece(Piece piece) {
        pieces[piece.team].Remove(piece);
    }

    // Update each piece's coordinates getting the closest square
    private void setStartPiecesCoor() {
        foreach (KeyValuePair<int, List<Piece>> entry in pieces) {
            List<Piece> team_pieces = pieces[entry.Key];

            for (int i = 0; i < team_pieces.Count; i++) {
                Square closest_square = getClosestSquare(team_pieces[i].transform.position);
                closest_square.holdPiece(team_pieces[i]);
                team_pieces[i].setStartSquare(closest_square);
                team_pieces[i].board = this;
                if (team_pieces[i].team == -1) setPieceTheme(team_pieces[i].transform, themes[cur_theme].piece_white);
                else if (team_pieces[i].team == 1) setPieceTheme(team_pieces[i].transform, themes[cur_theme].piece_black);
            }
        }
    }

    private void setPieceTheme(Transform piece_tr, Material mat) {
        for (int i = 0; i < piece_tr.childCount; ++i) {
            Transform child = piece_tr.GetChild(i);
            try {
                child.GetComponent<Renderer>().material = mat;
            }
            catch (Exception) {
                for (int j = 0; j < child.childCount; ++j) {
                    Transform child2 = child.GetChild(j);
                    child2.GetComponent<Renderer>().material = mat;
                }
            }
        }
    }

    private void instantiatePiece(Square square, GameObject piece, int team) {
        Piece this_piece = piece.GetComponent<Piece>();

        piece.transform.position = new Vector3(square.coor.pos.x, transform.position.y, square.coor.pos.z);
        pieces[this_piece.team].Add(this_piece);
        square.holdPiece(this_piece);
        this_piece.cur_square = square;
        this_piece.board = this;
        piece.GetComponent<DragAndDrop>().setBoard(this);
        if (this_piece.team == -1) setPieceTheme(this_piece.transform, themes[cur_theme].piece_white);
        else if (this_piece.team == 1) setPieceTheme(this_piece.transform, themes[cur_theme].piece_black);
    }

    public void promote(Square square, PieceType piece_type, int team) {
        if (team == -1) {
            switch (piece_type) {
                case PieceType.Pawn:
                    GameObject pawnWhite = Instantiate(whitePawn, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, pawnWhite, team);
                    break;
                case PieceType.Horse:
                    GameObject horseWhite = Instantiate(whiteKnight, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, horseWhite, team);
                    break;
                case PieceType.Bishop:
                    GameObject bishopWhite = Instantiate(whiteBishop, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, bishopWhite, team);
                    break;
                case PieceType.Tower:
                    GameObject towerWhite = Instantiate(whiteRook, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, towerWhite, team);
                    break;
                case PieceType.Queen:
                    GameObject queenWhite = Instantiate(whiteQueen, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, queenWhite, team);
                    break;
                case PieceType.King:
                    if (getKingPiece(-1) == null) {
                        GameObject kingWhite = Instantiate(whiteKing, new Vector3(0, 0, 0), Quaternion.identity);
                        instantiatePiece(square, kingWhite, team);
                    }
                    break;
            }
        } else {
            switch (piece_type) {
                case PieceType.Pawn:
                    GameObject pawnBlack = Instantiate(blackPawn, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, pawnBlack, team);
                    break;
                case PieceType.Horse:
                    GameObject horseBlack = Instantiate(blackKnight, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, horseBlack, team);
                    break;
                case PieceType.Bishop:
                    GameObject bishopBlack = Instantiate(blackBishop, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, bishopBlack, team);
                    break;
                case PieceType.Tower:
                    GameObject towerBlack = Instantiate(blackRook, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, towerBlack, team);
                    break;
                case PieceType.Queen:
                    GameObject queenBlack = Instantiate(blackQueen, new Vector3(0, 0, 0), Quaternion.identity);
                    instantiatePiece(square, queenBlack, team);
                    break;
                case PieceType.King:
                    if (getKingPiece(1) == null) {
                        GameObject kingBlack = Instantiate(blackKing, new Vector3(0, 0, 0), Quaternion.identity);
                        instantiatePiece(square, kingBlack, team);
                    }
                    break;
            }
        }

        if (horseButton != null) {
            Destroy(horseButton);
        }
        if (bishopButton != null) {
            Destroy(bishopButton);
        }
        if (towerButton != null) {
            Destroy(towerButton);
        }
        if (queenButton != null) {
            Destroy(queenButton);
        }

        horseButton = null;
        bishopButton = null;
        towerButton = null;
        queenButton = null;

        if (isGameInProgress) {
            positions.RemoveAt(positions.Count - 1);
            positions.Add(this);

            updateWinner();
            changeTurn();
        }
    }

    /*
    ---------------
    Game related functions
    ---------------
    */
    public void updateWinner() {
        isCheckMate(cur_turn);
        checkThreefoldRepetition();
        if (isInsufficientMaterial(-1) && isInsufficientMaterial(1)) {
            winner = Winner.Draw;
        }
    }

    // Change current turn, we check if a team lost before rotating the camera
    

    public void changeTurn() {
        if (winner == Winner.Draw) {
            startSound.Play();
            displayText("Draw");
        } else if (winner == Winner.White) {
            startSound.Play();
            displayText("White wins");
        } else if (winner == Winner.Black) {
            startSound.Play();
            displayText("Black wins");
        }

        if (winner == Winner.Nobody) {
            if (cur_turn == -1) {
                fiftyMoveRuleCount++;

                if (fiftyMoveRuleCount == 51) {
                    displayText("Draw by fifty move rule");
                    startSound.Play();
                }
            } else {
                Evaluation aiBotEvaluation = AIBot.minEvaluation(this, float.NegativeInfinity, float.PositiveInfinity,

                // This integeger controls the depth of the move search algorithm. The time it takes to search moves increases exponentially with the depth.
                3

                );
                getSquareFromCoordinate(aiBotEvaluation.move.start).holding_piece.GetComponent<DragAndDrop>().moveToSquare(getSquareFromCoordinate(aiBotEvaluation.move.end), aiBotEvaluation.move.pType);
            }
        }

        if (rotate_camera) {
            main_camera.changeTeam(cur_turn);
        }
    }

    // Show check mate message
    private void displayText(string text) {
        GameObject txt = Instantiate(gameTxt, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
        txt.GetComponent<TextMeshProUGUI>().text = text;
        txt.transform.localPosition = new Vector3(0, 300, 0);
        txt.transform.localRotation = Quaternion.identity;
    }

    public void setupButton(GameObject button, string text, Vector3 position, PieceType pieceType, int team, Square square) {
        button.transform.localPosition = position;
        button.transform.localRotation = Quaternion.identity;
        button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = text;
        Button btn = button.GetComponent<Button>();
        btn.onClick.AddListener(delegate { promote(square, pieceType, team); });
    }

    public void setupButton(GameObject button, string text, Vector3 position, PieceType pieceType, int team) {
        button.transform.localPosition = position;
        button.transform.localRotation = Quaternion.identity;
        button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = text;
        Button btn = button.GetComponent<Button>();
        btn.onClick.AddListener(delegate { promote(getAvailableSquare(), pieceType, team); });
    }

    private Square getAvailableSquare() {
        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 8; j++) {
                if (rank[i].file[j].holding_piece == null) {
                    return rank[i].file[j];
                }
            }
        }
        return rank[0].file[0];
    }

    public void setupPosition() {
        if (isGameInProgress) {
            isGameInProgress = false;
            setupPosButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Play from position";

            setupWhitePawn = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupWhitePawn, "White pawn", new Vector3(-500, 135, 0), PieceType.Pawn, -1);
            setupWhiteKnight = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupWhiteKnight, "White knight", new Vector3(-500, 90, 0), PieceType.Horse, -1);
            setupWhiteBishop = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupWhiteBishop, "White bishop", new Vector3(-500, 45, 0), PieceType.Bishop, -1);
            setupWhiteRook = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupWhiteRook, "White rook", new Vector3(-500, 0, 0), PieceType.Tower, -1);
            setupWhiteQueen = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupWhiteQueen, "White queen", new Vector3(-500, -45, 0), PieceType.Queen, -1);
            setupWhiteKing = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupWhiteKing, "White king", new Vector3(-500, -90, 0), PieceType.King, -1);
            setupBlackPawn = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupBlackPawn, "Black pawn", new Vector3(500, 135, 0), PieceType.Pawn, 1);
            setupBlackKnight = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupBlackKnight, "Black knight", new Vector3(500, 90, 0), PieceType.Horse, 1);
            setupBlackBishop = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupBlackBishop, "Black bishop", new Vector3(500, 45, 0), PieceType.Bishop, 1);
            setupBlackRook = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupBlackRook, "Black rook", new Vector3(500, 0, 0), PieceType.Tower, 1);
            setupBlackQueen = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupBlackQueen, "Black queen", new Vector3(500, -45, 0), PieceType.Queen, 1);
            setupBlackKing = Instantiate(button, new Vector3(0, 0, 0), Quaternion.identity, Canvas.transform);
            setupButton(setupBlackKing, "Black king", new Vector3(500, -90, 0), PieceType.King, 1);
        } else {
            Piece white_king = getKingPiece(-1);
            Piece black_king = getKingPiece(1);

            if (white_king && black_king && !white_king.checkValidMove(black_king.cur_square)) {
                isGameInProgress = true;
                setupPosButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Setup position";

                Destroy(setupWhitePawn);
                Destroy(setupWhiteKnight);
                Destroy(setupWhiteBishop);
                Destroy(setupWhiteRook);
                Destroy(setupWhiteQueen);
                Destroy(setupWhiteKing);
                Destroy(setupBlackPawn);
                Destroy(setupBlackKnight);
                Destroy(setupBlackBishop);
                Destroy(setupBlackRook);
                Destroy(setupBlackQueen);
                Destroy(setupBlackKing);

                setupWhitePawn = null;
                setupWhiteKnight = null;
                setupWhiteBishop = null;
                setupWhiteRook = null;
                setupWhiteQueen = null;
                setupWhiteKing = null;
                setupBlackPawn = null;
                setupBlackKnight = null;
                setupBlackBishop = null;
                setupBlackRook = null;
                setupBlackQueen = null;
                setupBlackKing = null;

                fiftyMoveRuleCount = 0;
                positions.Clear();
                positions.Add(this);
                enpassant_pawn_square = null;
                enpassant_square = null;
                checking_pieces.Clear();
            }
        }
    }

    /*
    ---------------
    User Interface related functions
    ---------------
    */
    public void useHover(bool use) {
        use_hover = use;
    }

    public void rotateCamera(bool rotate) {
        rotate_camera = rotate;
    }

    public void setBoardTheme() {
        for (int i = 0; i < board_sides.Count ; i++) {
            board_sides[i].material = themes[cur_theme].board_side;
            board_corners[i].material = themes[cur_theme].board_corner;
        }
    }

    public void updateGameTheme(int theme) {
        cur_theme = theme;
        setBoardTheme();
        foreach (KeyValuePair<int, List<Piece>> entry in pieces) {
            for (int i = 0; i < pieces[entry.Key].Count; i++) {
                setPieceTheme(pieces[entry.Key][i].transform, themes[cur_theme].piece_white);
            }
        }
        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 8; j++) {
                if (rank[i].file[j].team == -1) rank[i].file[j].GetComponent<Renderer>().material = themes[cur_theme].square_white;
                else if (rank[i].file[j].team == 1) rank[i].file[j].GetComponent<Renderer>().material = themes[cur_theme].square_black;
                rank[i].file[j].start_mat = rank[i].file[j].GetComponent<Renderer>().material;
            }
        }
    }
}
