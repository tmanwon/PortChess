using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using System.Xml.Linq;
using TMPro;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;
using System;

public class ChessGame : MonoBehaviour
{
    // FIELDS OF THE CLASS
    public GameObject boardAnchor; // Board Anchor
    public OVRHand hand;
    public OVRSkeleton handSkeleton;
    public bool isWhitesTurn;
    public GameObject selectedPiece;
    public float selectedPieceX;
    public float selectedPieceY;
    public GameObject analysisContent;
    public GameObject gameCardPrefab;
    private ArrayList matchHistory;
    private int currentMove;
    private int matchId;
    private string username;
    private ArrayList moves;
    private int moveIndex;
    private bool isInGame;
    private bool isAnalyzing;
    private bool gameOver = false;
    // SQLite
    private string databasePath;
    private SqliteConnection databaseConnection;
    private SqliteCommand queryCommand;
    private SqliteDataReader cursor;
    // Piece Prefabs
    public GameObject bishopDarkPrefab;
    public GameObject bishopLightPrefab;
    public GameObject pawnDarkPrefab;
    public GameObject pawnLightPrefab;
    public GameObject knightDarkPrefab;
    public GameObject knightLightPrefab;
    public GameObject rookDarkPrefab;
    public GameObject rookLightPrefab;
    public GameObject kingDarkPrefab;
    public GameObject kingLightPrefab;
    public GameObject queenDarkPrefab;
    public GameObject queenLightPrefab;
    // Piece Objects
    private GameObject leftBishopDark;
    private GameObject rightBishopDark;
    private GameObject leftKnightDark;
    private GameObject rightKnightDark;
    private GameObject leftRookDark;
    private GameObject rightRookDark;
    private GameObject leftBishopLight;
    private GameObject rightBishopLight;
    private GameObject leftKnightLight;
    private GameObject rightKnightLight;
    private GameObject leftRookLight;
    private GameObject rightRookLight;
    private GameObject KingDark;
    private GameObject KingLight;
    private GameObject QueenDark;
    private GameObject QueenLight;

    private ArrayList lightPawns;
    private ArrayList darkPawns;

    private Dictionary<int, GameObject> boardStatus;

    float yVelocity = 0.0f;

    // CONSTRUCTOR
    void Start()
    {
        // Initialize ArrayList Trackers
        lightPawns = new ArrayList();
        darkPawns = new ArrayList();
        isWhitesTurn = true;
        matchHistory = new ArrayList();
        moves = new ArrayList();
        boardStatus = new Dictionary<int, GameObject>();
        // Load SQLite DB File from device local storage
        var formattedFilePath = string.Format("{0}/{1}", UnityEngine.Application.persistentDataPath, "portchessdb.sqlite");
        // If sqlite database hasn't been stored on local device yet...
        if (!File.Exists(formattedFilePath))
        {
            Debug.Log("Master Project: DB not in Persistent Path");
            // Store on Android Device
            var loadDb = new WWW("jar:file://" + UnityEngine.Application.dataPath + "!/assets/" + "portchessdb.sqlite");
            while (!loadDb.isDone) { }
            File.WriteAllBytes(formattedFilePath, loadDb.bytes);
            Debug.Log("Master Project: DB has been written to device");
        }
        else
        {
            Debug.Log("Master Project: DB found in Persistent Path");
        }
        // Database Connection
        databasePath = "URI=file:" + UnityEngine.Application.persistentDataPath + "/" + "portchessdb.sqlite";
        databaseConnection = new SqliteConnection(databasePath);
        databaseConnection.Open();
        // Get user's Meta display name
        Oculus.Platform.Users.GetLoggedInUser().OnComplete(GetDisplayNameCallback);

    }

    // Meta display name callback
    private void GetDisplayNameCallback(Message msg)
    {
        print("Master Project - Callback");
        if (!msg.IsError)
        {
            User user = msg.GetUser();
            username = user.DisplayName;
        }
    }

    // HOST ONLY: Start match against connected opponent
    public void StartGame()
    {
        gameOver = false;
        currentMove = 0;
        isWhitesTurn = true;
        InstantiatePieces();
        CreateMatchID();
        SaveMove();
        isInGame = true;
        EnablePieces();
        DisablePieces();
        foreach(Transform child in analysisContent.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void CreateMatchID()
    {
        queryCommand = new SqliteCommand("INSERT INTO matches (user_id, opponent_id) VALUES (0, 0)", databaseConnection);
        queryCommand.ExecuteNonQuery();
        queryCommand = new SqliteCommand("SELECT MAX(match_id) FROM matches", databaseConnection);
        cursor = queryCommand.ExecuteReader();
        print("Master Project - Match ID Created");
        while (cursor.Read())
        {
            // Add as element to ArrayList
            print(string.Format("Master Project - Found Match ID: {0}", cursor[0]));
            matchId = (int)(Int64) cursor[0];
        }
        print(string.Format("Master Project - New Match ID: {0}", matchId));
    }

    // Host set up
    public void HostGame()
    {

    }

    // Client set up
    public void JoinGame()
    {

    }

    // Show user's match history
    public void StartAnalysis()
    {
        LoadMatchHistory();
    }

    // Set up board for analyzing the selected game
    public void AnalyzeSelectedGame(int selectedMatch)
    {
        boardAnchor.GetComponent<ChessBoard>().EndOrbit();
        isAnalyzing = true;
        moves.Clear();
        moveIndex = 0;
        print(string.Format("Master Project: Loading moves from match {0}", selectedMatch));
        queryCommand = new SqliteCommand(string.Format("SELECT * FROM moves WHERE match_id = {0}", selectedMatch), databaseConnection);
        cursor = queryCommand.ExecuteReader();
        print("Master Project - Preparing to read moves");
        while (cursor.Read())
        {
            // Add move as element to ArrayList
            print(string.Format("Master Project - Loading Move: {0}", cursor[0]));
            ArrayList buffer = new ArrayList();
            // Parse columns A-H into ArrayLists
            for (int i = 1; i < 9; i++)
            {
                buffer.Add(new ArrayList(string.Format("{0}", cursor[i]).Split(",")));
                print(string.Format("Master Project - Loaded column {0}", i));
            }
            moves.Add(buffer);
            print(string.Format("Master Project - Loaded Move: {0}", cursor[0]));
        }
        print(string.Format("Master Project - Finished Loading Moves from Match: {0}", selectedMatch));
        InstantiatePieces();
        isWhitesTurn = true;
        ReadSelectedMove(moveIndex);
    }

    // Back Button Action
    public void BackButtonClicked()
    {
        gameOver = false;
        boardAnchor.GetComponent<ChessBoard>().Orbit();
        DestroyPieces();
        lightPawns.Clear();
        darkPawns.Clear();
        isInGame = false;
        isAnalyzing = false;
    }

    // Update is called every frame
    void Update()
    {
        if (isInGame && gameOver)
        {
            float stackScale = Mathf.SmoothDamp(leftRookLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale.y, 1f, ref yVelocity, 0.01f);
            leftBishopDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightBishopDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            leftKnightDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightKnightDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            leftRookDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightRookDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            KingDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            QueenDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);

            leftRookLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightRookLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            leftBishopLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightBishopLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            leftKnightLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightKnightLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            KingLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            QueenLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            foreach (GameObject pawn in lightPawns)
            {
                pawn.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            }
            foreach (GameObject pawn in darkPawns)
            {
                pawn.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            }
        } else if (isWhitesTurn && isInGame)
        {
            
            float chipScale = Mathf.SmoothDamp(leftRookLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale.y, 0.1f, ref yVelocity, 0.01f);
            float stackScale = Mathf.SmoothDamp(leftBishopDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale.y, 1f, ref yVelocity, 0.01f);
            leftRookLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            rightRookLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            leftBishopLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            rightBishopLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            leftKnightLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            rightKnightLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            KingLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            QueenLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);

            leftBishopDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightBishopDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            leftKnightDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightKnightDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            leftRookDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightRookDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            KingDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            QueenDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);

            foreach (GameObject pawn in lightPawns)
            {
                pawn.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            }
            foreach (GameObject pawn in darkPawns)
            {
                pawn.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            }
        }
        else if (!isWhitesTurn && isInGame)
        {
            float chipScale = Mathf.SmoothDamp(leftBishopDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale.y, 0.1f, ref yVelocity, 0.01f);
            float stackScale = Mathf.SmoothDamp(leftRookLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale.y, 1f, ref yVelocity, 0.01f);
            leftRookLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightRookLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            leftBishopLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightBishopLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            leftKnightLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            rightKnightLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            KingLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            QueenLight.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);

            leftBishopDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            rightBishopDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            leftKnightDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            rightKnightDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            leftRookDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            rightRookDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            KingDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            QueenDark.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);

            foreach (GameObject pawn in lightPawns)
            {
                pawn.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, stackScale, 1);
            }
            foreach (GameObject pawn in darkPawns)
            {
                pawn.GetComponent<ChessPiece>().pieceToScale.transform.localScale = new Vector3(1, chipScale, 1);
            }
        }
    }

    // Load user's match history from database
    public void LoadMatchHistory()
    {
        print("Master Project - Loading Match History");
        matchHistory.Clear();
        queryCommand = new SqliteCommand("SELECT * from matches;", databaseConnection);
        cursor = queryCommand.ExecuteReader();
        print("Query Returned");
        // For every record...
        while (cursor.Read())
        {
            // Add as element to ArrayList
            print(string.Format("Master Project - Found Match: {0}, {1}, {2}", cursor[0], cursor[1], cursor[2]));
            ArrayList buffer = new ArrayList();
            buffer.Add(string.Format("{0}", cursor[0]));
            buffer.Add(string.Format("{0}", cursor[1]));
            buffer.Add(string.Format("{0}", cursor[2]));
            buffer.Add(string.Format("{0}", cursor[3]));
            buffer.Add(string.Format("{0}", cursor[4]));
            matchHistory.Add(buffer);
        }
        print("Master Project - Finshed Loading Matches");
        // Create a game card for each match in user's match history
        foreach (ArrayList obj in matchHistory)
        {
            // Create game card
            print(string.Format("Master Project - Creating Match Card: {0}, {1}, {2}", obj[0], obj[1], obj[2]));
            GameObject card = Instantiate(gameCardPrefab);
            card.transform.SetParent(analysisContent.transform, false);
            card.GetComponent<Match>().match_id = int.Parse(string.Format("{0}", obj[0]));

            // Match Result
            card.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = string.Format("{0}", obj[3]);
            // Datetime
            card.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = string.Format("{0}", obj[4]);
            // Username
            card.transform.GetChild(4).gameObject.GetComponent<TMP_Text>().text = "tmanwon";
            // Opponent Name
            card.transform.GetChild(5).gameObject.GetComponent<TMP_Text>().text = "Opponent";
            print("Master Project - Match Card Created");
        }
    }

    // Instantiate Chess Pieces and Initialize Board Dictionary
    public void InstantiatePieces()
    {
        boardStatus.Clear();
        // Dark Rooks
        leftRookDark = Instantiate(rookDarkPrefab) as GameObject;
        leftRookDark.GetComponent<ChessPiece>().id = "LeftRookDark";
        leftRookDark.GetComponent<ChessPiece>().hand = hand;
        leftRookDark.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        leftRookDark.transform.parent = boardAnchor.transform;
        leftRookDark.transform.localPosition = new Vector3((7 * 5) + 3f, -3.8f, (7 * 5) - 38f);
        rightRookDark = Instantiate(rookDarkPrefab) as GameObject;
        rightRookDark.GetComponent<ChessPiece>().id = "RightRookDark";
        rightRookDark.GetComponent<ChessPiece>().hand = hand;
        rightRookDark.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        rightRookDark.transform.parent = boardAnchor.transform;
        rightRookDark.transform.localPosition = new Vector3((0 * 5) + 3f, -3.8f, (7 * 5) - 38f);
        // Light Rooks
        leftRookLight = Instantiate(rookLightPrefab) as GameObject;
        leftRookLight.GetComponent<ChessPiece>().id = "LeftRookLight";
        leftRookLight.GetComponent<ChessPiece>().hand = hand;
        leftRookLight.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        leftRookLight.transform.parent = boardAnchor.transform;
        leftRookLight.transform.localPosition = new Vector3((0 * 5) + 3f, -3.8f, (0 * 5) - 38f);
        rightRookLight = Instantiate(rookLightPrefab) as GameObject;
        rightRookLight.GetComponent<ChessPiece>().id = "RightRookLight";
        rightRookLight.GetComponent<ChessPiece>().hand = hand;
        rightRookLight.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        rightRookLight.transform.parent = boardAnchor.transform;
        rightRookLight.transform.localPosition = new Vector3((7 * 5) + 3f, -3.8f, (0 * 5) - 38f);
        // Dark Knights
        leftKnightDark = Instantiate(knightDarkPrefab) as GameObject;
        leftKnightDark.GetComponent<ChessPiece>().id = "LeftKnightDark";
        leftKnightDark.GetComponent<ChessPiece>().hand = hand;
        leftKnightDark.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        leftKnightDark.transform.parent = boardAnchor.transform;
        leftKnightDark.transform.localPosition = new Vector3((6 * 5) + 3f, -3.8f, (7 * 5) - 38f);
        rightKnightDark = Instantiate(knightDarkPrefab) as GameObject;
        rightKnightDark.GetComponent<ChessPiece>().id = "RightKnightDark";
        rightKnightDark.GetComponent<ChessPiece>().hand = hand;
        rightKnightDark.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        rightKnightDark.transform.parent = boardAnchor.transform;
        rightKnightDark.transform.localPosition = new Vector3((1 * 5) + 3f, -3.8f, (7 * 5) - 38f);
        // Light Knights
        leftKnightLight = Instantiate(knightLightPrefab) as GameObject;
        leftKnightLight.GetComponent<ChessPiece>().id = "LeftKnightLight";
        leftKnightLight.GetComponent<ChessPiece>().hand = hand;
        leftKnightLight.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        leftKnightLight.transform.parent = boardAnchor.transform;
        leftKnightLight.transform.localPosition = new Vector3((1 * 5) + 3f, -3.8f, (0 * 5) - 38f);
        rightKnightLight = Instantiate(knightLightPrefab) as GameObject;
        rightKnightLight.GetComponent<ChessPiece>().id = "RightKnightLight";
        rightKnightLight.GetComponent<ChessPiece>().hand = hand;
        rightKnightLight.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        rightKnightLight.transform.parent = boardAnchor.transform;
        rightKnightLight.transform.localPosition = new Vector3((6 * 5) + 3f, -3.8f, (0 * 5) - 38f);
        // Dark Bishops
        leftBishopDark = Instantiate(bishopDarkPrefab) as GameObject;
        leftBishopDark.GetComponent<ChessPiece>().id = "LeftBishopDark";
        leftBishopDark.GetComponent<ChessPiece>().hand = hand;
        leftBishopDark.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        leftBishopDark.transform.parent = boardAnchor.transform;
        leftBishopDark.transform.localPosition = new Vector3((5 * 5) + 3f, -3.8f, (7 * 5) - 38f);
        rightBishopDark = Instantiate(bishopDarkPrefab) as GameObject;
        rightBishopDark.GetComponent<ChessPiece>().id = "RightBishopDark";
        rightBishopDark.GetComponent<ChessPiece>().hand = hand;
        rightBishopDark.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        rightBishopDark.transform.parent = boardAnchor.transform;
        rightBishopDark.transform.localPosition = new Vector3((2 * 5) + 3f, -3.8f, (7 * 5) - 38f);
        // Light Bishops
        leftBishopLight = Instantiate(bishopLightPrefab) as GameObject;
        leftBishopLight.GetComponent<ChessPiece>().id = "LeftBishopLight";
        leftBishopLight.GetComponent<ChessPiece>().hand = hand;
        leftBishopLight.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        leftBishopLight.transform.parent = boardAnchor.transform;
        leftBishopLight.transform.localPosition = new Vector3((2 * 5) + 3f, -3.8f, (0 * 5) - 38f);
        rightBishopLight = Instantiate(bishopLightPrefab) as GameObject;
        rightBishopLight.GetComponent<ChessPiece>().id = "RightBishopLight";
        rightBishopLight.GetComponent<ChessPiece>().hand = hand;
        rightBishopLight.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        rightBishopLight.transform.parent = boardAnchor.transform;
        rightBishopLight.transform.localPosition = new Vector3((5 * 5) + 3f, -3.8f, (0 * 5) - 38f);
        // Dark Queen
        QueenDark = Instantiate(queenDarkPrefab) as GameObject;
        QueenDark.GetComponent<ChessPiece>().id = "QueenDark";
        QueenDark.GetComponent<ChessPiece>().hand = hand;
        QueenDark.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        QueenDark.transform.parent = boardAnchor.transform;
        QueenDark.transform.localPosition = new Vector3((3 * 5) + 3f, -3.8f, (7 * 5) - 38f);
        // Light Queen
        QueenLight = Instantiate(queenLightPrefab) as GameObject;
        QueenLight.GetComponent<ChessPiece>().id = "QueenLight";
        QueenLight.GetComponent<ChessPiece>().hand = hand;
        QueenLight.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        QueenLight.transform.parent = boardAnchor.transform;
        QueenLight.transform.localPosition = new Vector3((3 * 5) + 3f, -3.8f, (0 * 5) - 38f);
        // Dark King
        KingDark = Instantiate(kingDarkPrefab) as GameObject;
        KingDark.GetComponent<ChessPiece>().id = "KingDark";
        KingDark.GetComponent<ChessPiece>().hand = hand;
        KingDark.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        KingDark.transform.parent = boardAnchor.transform;
        KingDark.transform.localPosition = new Vector3((4 * 5) + 3f, -3.8f, (7 * 5) - 38f);
        // Light King
        KingLight = Instantiate(kingLightPrefab) as GameObject;
        KingLight.GetComponent<ChessPiece>().id = "KingLight";
        KingLight.GetComponent<ChessPiece>().hand = hand;
        KingLight.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
        KingLight.transform.parent = boardAnchor.transform;
        KingLight.transform.localPosition = new Vector3((4 * 5) + 3f, -3.8f, (0 * 5) - 38f);
        // Dark Pawns
        string[] letters = {"A", "B", "C", "D", "E", "F", "G", "H"};
        for (int i = 0; i < 8; i++)
        {
            GameObject pawnDark = Instantiate(pawnDarkPrefab) as GameObject;
            pawnDark.GetComponent<ChessPiece>().id = string.Format("{0}7PawnDark", letters[i]);
            pawnDark.GetComponent<ChessPiece>().hand = hand;
            pawnDark.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
            pawnDark.transform.parent = boardAnchor.transform;
            pawnDark.transform.localPosition = new Vector3((i * 5) + 3f, -3.8f, (6 * 5) - 38f);
            darkPawns.Add(pawnDark);
        }
        // Light Pawns
        for (int i = 0; i < 8; i++)
        {
            GameObject pawnLight = Instantiate(pawnLightPrefab) as GameObject;
            pawnLight.GetComponent<ChessPiece>().id = string.Format("{0}2PawnLight", letters[i]);
            pawnLight.GetComponent<ChessPiece>().hand = hand;
            pawnLight.GetComponent<ChessPiece>().handSkeleton = handSkeleton;
            pawnLight.transform.parent = boardAnchor.transform;
            pawnLight.transform.localPosition = new Vector3((i * 5) + 3f, -3.8f, (1 * 5) - 38f);
            lightPawns.Add(pawnLight);
        }

        boardStatus = new Dictionary<int, GameObject>
        {
            // A
            {100, leftRookLight},
            {101, (GameObject) lightPawns[0]},
            {102, boardAnchor},
            {103, boardAnchor},
            {104, boardAnchor},
            {105, boardAnchor},
            {106, (GameObject) darkPawns[0]},
            {107, rightRookDark},
            // B
            {110, leftKnightLight},
            {111, (GameObject) lightPawns[1]},
            {112, boardAnchor},
            {113, boardAnchor},
            {114, boardAnchor},
            {115, boardAnchor},
            {116, (GameObject) darkPawns[1]},
            {117, rightKnightDark},
            // C
            {120, leftBishopLight},
            {121, (GameObject) lightPawns[2]},
            {122, boardAnchor},
            {123, boardAnchor},
            {124, boardAnchor},
            {125, boardAnchor},
            {126, (GameObject) darkPawns[2]},
            {127, rightBishopDark},
            // D
            {130, QueenLight},
            {131, (GameObject) lightPawns[3]},
            {132, boardAnchor},
            {133, boardAnchor},
            {134, boardAnchor},
            {135, boardAnchor},
            {136, (GameObject) darkPawns[3]},
            {137, QueenDark},
            // E
            {140, KingLight},
            {141, (GameObject) lightPawns[4]},
            {142, boardAnchor},
            {143, boardAnchor},
            {144, boardAnchor},
            {145, boardAnchor},
            {146, (GameObject) darkPawns[4]},
            {147, KingDark},
            // F
            {150, rightBishopLight},
            {151, (GameObject) lightPawns[5]},
            {152, boardAnchor},
            {153, boardAnchor},
            {154, boardAnchor},
            {155, boardAnchor},
            {156, (GameObject) darkPawns[5]},
            {157, leftBishopDark},
            // G
            {160, rightKnightLight},
            {161, (GameObject) lightPawns[6]},
            {162, boardAnchor},
            {163, boardAnchor},
            {164, boardAnchor},
            {165, boardAnchor},
            {166, (GameObject) darkPawns[6]},
            {167, leftKnightDark},
            // H
            {170, rightRookLight},
            {171, (GameObject) lightPawns[7]},
            {172, boardAnchor},
            {173, boardAnchor},
            {174, boardAnchor},
            {175, boardAnchor},
            {176, (GameObject) darkPawns[7]},
            {177, leftRookDark}
        };

    }

    // Destroy any instantiated Chess Piece objects
    public void DestroyPieces()
    {
        GameObject[] pieces;
        pieces = GameObject.FindGameObjectsWithTag("Piece");
        foreach (GameObject chessPiece in pieces)
        {
            Destroy(chessPiece);
        }
    }

    // Save move to database
    public void SaveMove()
    {
        print(string.Format("Master Project - Saving Move {0}", currentMove));
        ArrayList rows = new ArrayList();
        for (int i = 0; i < 8; i++)
        {
            string prepStmt = "";
            for (int j = 0; j < 8; j++)
            {
                if (boardStatus[100 + (10 * i) + j] == boardAnchor)
                {
                    prepStmt += "None,";
                } else
                {
                    prepStmt += boardStatus[100 + (10 * i) + j].GetComponent<ChessPiece>().id + ",";
                }
            }
            prepStmt = prepStmt.Remove(prepStmt.Length - 1, 1);
            print(string.Format("Master Project - Saving Col: {0}", prepStmt));
            rows.Add(prepStmt);
        }
        queryCommand = new SqliteCommand(string.Format("INSERT INTO moves (move_no, A, B, C, D, E, F, G, H, match_id) VALUES ({0}, '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', {9})", currentMove, rows[0], rows[1], rows[2], rows[3], rows[4], rows[5], rows[6], rows[7], matchId), databaseConnection);
        queryCommand.ExecuteNonQuery();
        print("Master Project - Moved Saved");
    }

    // Read move from database
    public void ReadSelectedMove(int selectedMove)
    {
        print(string.Format("Master Project - Reading Move {0}", selectedMove));
        ArrayList moveToRead = (ArrayList)moves[selectedMove];

        for (int i = 0; i < 8; i++)
        {
            ArrayList col = (ArrayList) moveToRead[i];
            for (int j = 0; j < 8; j++)
            {
                String row = string.Format("{0}", col[j]);
                if (row != "None")
                {
                    foreach (int key in boardStatus.Keys)
                    {
                        if (boardStatus[key].GetComponent<ChessPiece>() != null)
                        {
                            if (row == boardStatus[key].GetComponent<ChessPiece>().id)
                            {
                                boardStatus[key].transform.localPosition = new Vector3((i * 5) + 3, -3.8f, (j * 5) - 38);
                            }
                        }
                        
                    }
                }
            }
        }
        print("Master Project - Move Showed");
    }

    // Show next move
    public void ReadNextMove()
    {
        if (moves.Count == (moveIndex + 1))
        {
            moveIndex = 0;
            ReadSelectedMove(moveIndex);
        } else
        {
            moveIndex++;
            ReadSelectedMove(moveIndex);
        }
    }

    // Show previous move
    public void ReadPreviousMove()
    {
        if ((moveIndex - 1) < 0)
        {
            moveIndex = moves.Count - 1;
            ReadSelectedMove(moveIndex);
        } else
        {
            moveIndex--;
            ReadSelectedMove(moveIndex);
        }
    }

    // Enable chess pieces based on who's turn it is
    public void EnablePieces()
    {
        if (isWhitesTurn)
        {
            leftRookLight.GetComponent<PokeInteractable>().Enable();
            rightRookLight.GetComponent<PokeInteractable>().Enable();
            leftBishopLight.GetComponent<PokeInteractable>().Enable();
            rightBishopLight.GetComponent<PokeInteractable>().Enable();
            leftKnightLight.GetComponent<PokeInteractable>().Enable();
            rightKnightLight.GetComponent<PokeInteractable>().Enable();
            KingLight.GetComponent<PokeInteractable>().Enable();
            QueenLight.GetComponent<PokeInteractable>().Enable();

            foreach (GameObject pawn in lightPawns)
            {
                pawn.GetComponent<PokeInteractable>().Enable();
            }

        } else
        {
            leftBishopDark.GetComponent<PokeInteractable>().Enable();
            rightBishopDark.GetComponent<PokeInteractable>().Enable();
            leftKnightDark.GetComponent<PokeInteractable>().Enable();
            rightKnightDark.GetComponent<PokeInteractable>().Enable();
            leftRookDark.GetComponent<PokeInteractable>().Enable();
            rightRookDark.GetComponent<PokeInteractable>().Enable();
            KingDark.GetComponent<PokeInteractable>().Enable();
            QueenDark.GetComponent<PokeInteractable>().Enable();

            foreach (GameObject pawn in darkPawns)
            {
                pawn.GetComponent<PokeInteractable>().Enable();
            }
        }
    }

    // Disable pieces based on who's turn it is
    public void DisablePieces()
    {
        if (isWhitesTurn)
        {
            leftBishopDark.GetComponent<PokeInteractable>().Disable();
            rightBishopDark.GetComponent<PokeInteractable>().Disable();
            leftKnightDark.GetComponent<PokeInteractable>().Disable();
            rightKnightDark.GetComponent<PokeInteractable>().Disable();
            leftRookDark.GetComponent<PokeInteractable>().Disable();
            rightRookDark.GetComponent<PokeInteractable>().Disable();
            KingDark.GetComponent<PokeInteractable>().Disable();
            QueenDark.GetComponent<PokeInteractable>().Disable();

            foreach (GameObject pawn in darkPawns)
            {
                pawn.GetComponent<PokeInteractable>().Disable();
            }
        } else
        {
            leftRookLight.GetComponent<PokeInteractable>().Disable();
            rightRookLight.GetComponent<PokeInteractable>().Disable();
            leftBishopLight.GetComponent<PokeInteractable>().Disable();
            rightBishopLight.GetComponent<PokeInteractable>().Disable();
            leftKnightLight.GetComponent<PokeInteractable>().Disable();
            rightKnightLight.GetComponent<PokeInteractable>().Disable();
            KingLight.GetComponent<PokeInteractable>().Disable();
            QueenLight.GetComponent<PokeInteractable>().Disable();

            foreach (GameObject pawn in lightPawns)
            {
                pawn.GetComponent<PokeInteractable>().Disable();
            }


        }
    }

    // Determine legality of move and handle them
    public void OnMove(float targetX, float targetY)
    {
        // First ensure the selected piece can make the requested move
        bool isValid = ValidateMove(selectedPiece, Mathf.RoundToInt(targetX), Mathf.RoundToInt(targetY), Mathf.RoundToInt(selectedPieceX), Mathf.RoundToInt(selectedPieceY));
        if (isValid)
        {
            // If space is empty, move to it
            if (boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))] == boardAnchor)
            {
                boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))] = boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))];
                boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))] = boardAnchor;
                selectedPiece.GetComponent<ChessPiece>().moves++;
                currentMove++;
                SaveMove();
                isWhitesTurn = (isWhitesTurn) ? (false) : (true);
                EnablePieces();
                DisablePieces();
            }
            // If space is of other color... capture it
            else if (boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))].GetComponent<ChessPiece>() != null && boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))].GetComponent<ChessPiece>().colorType != boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))].GetComponent<ChessPiece>().colorType)
            {
                Destroy(boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))]);
                boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))] = boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))];
                boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))] = boardAnchor;
                selectedPiece.GetComponent<ChessPiece>().moves++;
                currentMove++;
                SaveMove();
                isWhitesTurn = (isWhitesTurn) ? (false) : (true);
                EnablePieces();
                DisablePieces();
            }
            // If space is of your color... you can't move there
            else if (boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))].GetComponent<ChessPiece>() != null && boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))].GetComponent<ChessPiece>().colorType == boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))].GetComponent<ChessPiece>().colorType)
            {
                boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))].transform.localPosition = new Vector3((Mathf.RoundToInt(selectedPieceX) * 5) + 3, -3.8f, (Mathf.RoundToInt(selectedPieceY) * 5) - 38);
            }

            // Check if king in checkmate
            if (inCheckmate()) {
                print("Master Project - In Checkmate");
                EndGame();
            } else
            {
                print("Master Project - Not in checkmate");
            }

        } else
        {
            boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))].transform.localPosition = new Vector3((Mathf.RoundToInt(selectedPieceX) * 5) + 3, -3.8f, (Mathf.RoundToInt(selectedPieceY) * 5) - 38);
        }

    }

    // Check if king is in checkmate
    bool inCheckmate()
    {
        foreach (GameObject piece in boardStatus.Values)
        {
            if (piece.gameObject.GetComponent<ChessPiece>() != null)
            {
                if (piece.gameObject.GetComponent<ChessPiece>().colorType == ColorType.WHITE)
                {
                    print("Master Project - Checking piece for black king checkmate " + piece.gameObject.GetComponent<ChessPiece>().id);
                    int kingX = (Mathf.RoundToInt(KingDark.gameObject.transform.localPosition.x) - 3) / 5;
                    int kingY = (Mathf.RoundToInt(KingDark.gameObject.transform.localPosition.z) + 38) / 5;
                    int pieceX = (Mathf.RoundToInt(piece.gameObject.transform.localPosition.x) - 3) / 5;
                    int pieceY = (Mathf.RoundToInt(piece.gameObject.transform.localPosition.z) + 38) / 5;

                    try
                    {
                        if (ValidateMove(piece.gameObject, kingX, kingY, pieceX, pieceY))
                        {
                            print("Master Project - Piece can capture Dark king " + piece.gameObject.GetComponent<ChessPiece>().id);
                            // Update matchstatus in database
                            queryCommand = new SqliteCommand(string.Format("UPDATE matches SET match_result = 'WIN' WHERE match_id = {0}", matchId), databaseConnection);
                            queryCommand.ExecuteNonQuery();
                            return true;
                        }
                    } catch
                    {

                    }
                } else if (piece.gameObject.GetComponent<ChessPiece>().colorType == ColorType.DARK)
                {
                    print("Master Project - Checking piece for white king checkmate" + piece.gameObject.GetComponent<ChessPiece>().id);
                    int kingX = (Mathf.RoundToInt(KingLight.gameObject.transform.localPosition.x) - 3) / 5;
                    int kingY = (Mathf.RoundToInt(KingLight.gameObject.transform.localPosition.z) + 38) / 5;
                    int pieceX = (Mathf.RoundToInt(piece.gameObject.transform.localPosition.x) - 3) / 5;
                    int pieceY = (Mathf.RoundToInt(piece.gameObject.transform.localPosition.z) + 38) / 5;

                    try
                    {
                        if (ValidateMove(piece.gameObject, kingX, kingY, pieceX, pieceY))
                        {
                            print("Master Project - Piece can capture Light king " + piece.gameObject.GetComponent<ChessPiece>().id);
                            // Update matchstatus in database
                            queryCommand = new SqliteCommand(string.Format("UPDATE matches SET match_result = 'LOSS' WHERE match_id = {0}", matchId), databaseConnection);
                            queryCommand.ExecuteNonQuery();
                            return true;
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }
        // Not in checkmate
        return false;
    }

    // End game if a player wins
    public void EndGame()
    {
        // Disable all pieces
        leftBishopDark.GetComponent<PokeInteractable>().Disable();
        rightBishopDark.GetComponent<PokeInteractable>().Disable();
        leftKnightDark.GetComponent<PokeInteractable>().Disable();
        rightKnightDark.GetComponent<PokeInteractable>().Disable();
        leftRookDark.GetComponent<PokeInteractable>().Disable();
        rightRookDark.GetComponent<PokeInteractable>().Disable();
        KingDark.GetComponent<PokeInteractable>().Disable();
        QueenDark.GetComponent<PokeInteractable>().Disable();

        foreach (GameObject pawn in darkPawns)
        {
            pawn.GetComponent<PokeInteractable>().Disable();
        }
        leftRookLight.GetComponent<PokeInteractable>().Disable();
        rightRookLight.GetComponent<PokeInteractable>().Disable();
        leftBishopLight.GetComponent<PokeInteractable>().Disable();
        rightBishopLight.GetComponent<PokeInteractable>().Disable();
        leftKnightLight.GetComponent<PokeInteractable>().Disable();
        rightKnightLight.GetComponent<PokeInteractable>().Disable();
        KingLight.GetComponent<PokeInteractable>().Disable();
        QueenLight.GetComponent<PokeInteractable>().Disable();

        foreach (GameObject pawn in lightPawns)
        {
            pawn.GetComponent<PokeInteractable>().Disable();
        }

        gameOver = true;
    }

    // Validate moves based on piece type
    public bool ValidateMove(GameObject pieceToMove, int targetX, int targetY, int selectedX, int selectedY)
    {
        int deltaX = targetX - selectedX;
        int deltaY = targetY - selectedY;
        print(string.Format("DELTA X: {0}, DELTA Y: {1}", deltaX, deltaY));
        int incrementX = (deltaX == 0) ? (0) : ((deltaX > 0) ? (1) : (-1));
        int incrementY = (deltaY == 0) ? (0) : ((deltaY > 0) ? (1) : (-1));
        int testCoordX = selectedX + incrementX;
        int testCoordY = selectedY + incrementY;

        // Check if piece is blocking path of movement for all pieces but knight
        if (pieceToMove.GetComponent<ChessPiece>().pieceType != PieceType.KNIGHT)
        {
            while (testCoordX != targetX || testCoordY != targetY)
            {
                if (boardStatus[100 + (testCoordX * 10) + testCoordY].GetComponent<ChessPiece>() != null)
                {
                    return false;
                }
                testCoordX += incrementX;
                testCoordY += incrementY;
            }
        }
        
        switch (pieceToMove.GetComponent<ChessPiece>().pieceType)
        {
            case PieceType.PAWN:
                if (deltaX == 0 && deltaY == 0) {
                    print("Didn't move anywhere");
                    return false;
                }

                // If piece is dark
                if (pieceToMove.GetComponent<ChessPiece>().colorType == ColorType.DARK)
                {
                    // Valid if capturing diagonally
                    if ((deltaY == -1) && (Mathf.Abs(deltaX) == 1))
                    {
                        if (boardStatus[(100 + (targetX * 10) + targetY)].GetComponent<ChessPiece>() != null)
                        {
                            return (boardStatus[(100 + (targetX * 10) + targetY)].GetComponent<ChessPiece>().colorType == ColorType.WHITE);
                        }
                        else
                        {
                            return false;
                        }
                    }
                        
                    // Invalid if moving horizontally
                    if (deltaX != 0)
                    {
                        return false;
                    }
                    // Able to move 2 ranks on first move
                    if (pieceToMove.GetComponent<ChessPiece>().moves == 0)
                    {
                        return ((deltaY == -1) || (deltaY == -2));
                    } else
                    {
                        return (deltaY == -1);
                    }
                } else // If piece is light
                {
                    // Valid if capturing diagonally
                    if ((deltaY == 1) && (Mathf.Abs(deltaX) == 1))
                    {
                        if (boardStatus[(100 + (targetX * 10) + targetY)].GetComponent<ChessPiece>() != null)
                        {
                            return (boardStatus[(100 + (targetX * 10) + targetY)].GetComponent<ChessPiece>().colorType == ColorType.DARK);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    // Invalid if moving horizontally
                    if (deltaX != 0)
                    {
                        return false;
                    }
                    // Able to move 2 ranks on first move
                    if (pieceToMove.GetComponent<ChessPiece>().moves == 0)
                    {
                        return ((deltaY == 1) || (deltaY == 2));
                    }
                    else
                    {
                        return (deltaY == 1);
                    }
                }
            case PieceType.BISHOP:
                if (deltaX == 0 && deltaY == 0) { return false; }
                // Valid if horizontal travel is same as vertical travel
                return (Mathf.Abs(deltaX) == Mathf.Abs(deltaY));
            case PieceType.KNIGHT:
                if (deltaX == 0 && deltaY == 0) { return false; }
                // Valid if ABS of vertical and horizontal travel is 1/2 or vice versa
                return ((Mathf.Abs(deltaX) == 1 && Mathf.Abs(deltaY) == 2) || (Mathf.Abs(deltaX) == 2 && Mathf.Abs(deltaY) == 1));
            case PieceType.ROOK:
                if (deltaX == 0 && deltaY == 0) { return false; }
                // Valid if only traveling in one vector
                return ((Mathf.Abs(deltaX) == 0 && Mathf.Abs(deltaY) > 0) || (Mathf.Abs(deltaX) > 0 && Mathf.Abs(deltaY) == 0));
            case PieceType.QUEEN:
                if (deltaX == 0 && deltaY == 0) { return false; }
                // Valid if moving like rook and bishop
                return ((Mathf.Abs(deltaX) == Mathf.Abs(deltaY)) || ((Mathf.Abs(deltaX) == 0 && Mathf.Abs(deltaY) > 0) || (Mathf.Abs(deltaX) > 0 && Mathf.Abs(deltaY) == 0)));
            case PieceType.KING:
                if (deltaX == 0 && deltaY == 0) { return false; }
                // Valid if moving up to 1 horizontal and vertical travel
                return (Mathf.Abs(deltaX) <= 1 && Mathf.Abs(deltaY) <= 1);
            default:
                return false;
        }

    }

}
