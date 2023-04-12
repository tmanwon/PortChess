using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
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

    // Start is called before the first frame update
    void Start()
    {
        lightPawns = new ArrayList();
        darkPawns = new ArrayList();
        isWhitesTurn = true;

        // Load SQLite DB File
        var formattedFilePath = string.Format("{0}/{1}", Application.persistentDataPath, "portchessdb.sqlite");
        // If sqlite database hasn't been stored on local device yet...
        if (!File.Exists(formattedFilePath))
        {
            Debug.Log("Master Project: DB not in Persistent Path");
            // Store on Android Device
            var loadDb = new WWW("jar:file://" + Application.dataPath + "!/assets/" + "portchessdb.sqlite");
            while (!loadDb.isDone) { }
            File.WriteAllBytes(formattedFilePath, loadDb.bytes);
            Debug.Log("Master Project: DB has been written to device");
        }
        else
        {
            Debug.Log("Master Project: DB found in Persistent Path");
        }
        // Database Connection
        //databasePath = "URI=file:" + Application.dataPath + "/StreamingAssets/" + "portchessdb.sqlite";
        databasePath = "URI=file:" + Application.persistentDataPath + "/" + "portchessdb.sqlite";
        databaseConnection = new SqliteConnection(databasePath);

    }

    // HOST ONLY: Start match against connected opponent
    public void StartGame()
    {
        isWhitesTurn = true;
        InstantiatePieces();
        EnablePieces();
        DisablePieces();
    }

    public void HostGame()
    {

    }

    public void JoinGame()
    {

    }

    // Show user's match history
    public void StartAnalysis()
    {
        databaseConnection.Open();
        LoadMatchHistory();
    }

    // Set up board for analyzing the selected game
    public void AnalyzeSelectedGame()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isWhitesTurn)
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
        else
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

    public void LoadMatchHistory()
    {
        matchHistory.Clear();
        queryCommand = new SqliteCommand("SELECT * from matches;", databaseConnection);
        cursor = queryCommand.ExecuteReader();
        // For every record...
        while (cursor.Read())
        {
            // Add as element to ArrayList
            print(string.Format("Master Project - Found Entry: {0}, {1}, {2}", cursor[0], cursor[1], cursor[2]));
            ArrayList buffer = new ArrayList();
            buffer.Add(string.Format("{0}", cursor[0]));
            buffer.Add(string.Format("{0}", cursor[1]));
            buffer.Add(string.Format("{0}", cursor[2]));
            buffer.Add(string.Format("{0}", cursor[3]));
            buffer.Add(string.Format("{0}", cursor[4]));
            matchHistory.Add(buffer);
        }
        ArrayList obj = (ArrayList) matchHistory[0];
        analysisContent.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = "WIN";
        analysisContent.transform.GetChild(0).gameObject.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = string.Format("{0}", obj[4]);
        analysisContent.transform.GetChild(0).gameObject.transform.GetChild(4).gameObject.GetComponent<TMP_Text>().text = "DICK";
        analysisContent.transform.GetChild(0).gameObject.transform.GetChild(5).gameObject.GetComponent<TMP_Text>().text = "BALLS";

        /*
        // Create a notebook page for every entry
        foreach (ArrayList obj in matchHistory)
        {
            // Create new page object
            print(string.Format("Master Project - Adding page: {0}, {1}, {2}", obj[0], obj[1], obj[2]));
            GameObject page = Instantiate(pagePrefab, new Vector3(0, 0, 0), Quaternion.identity);
            page.transform.SetParent(notebook.transform, false);
            page.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            page.transform.RotateAroundLocal(Vector3.back, 0f);
            page.transform.localPosition = new Vector3(0, 0.2f, 0);
            pages.Add(page);

            // Create new page canvas object to display information
            GameObject pageCanvas = Instantiate(pokeableTest, new Vector3(0, 0, 0), Quaternion.identity);
            pageCanvas.transform.SetParent(page.transform, false);
            pageCanvas.transform.localScale = new Vector3(23, 23, 23);
            pageCanvas.transform.localRotation = Quaternion.Euler(0, 180, 0);
            //pageCanvas.transform.Translate(new Vector3(0.02f, -0.15f, -0.015f));
            pageCanvas.transform.Translate(new Vector3(0.1f, -0.01f, -0.01f));

            dateField = pageCanvas.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<TMP_InputField>();
            contentField = pageCanvas.transform.GetChild(0).gameObject.transform.GetChild(1).gameObject.GetComponent<TMP_InputField>();

            contentField.interactable = false;
            dateField.interactable = false;
            contentField.interactable = true;
            dateField.interactable = true;
            contentField.text = string.Format("{0}", obj[1]);
            dateField.text = string.Format("{0}", obj[0]);
        }
        */

    }

    // Instantiate Chess Pieces and Initialize Board Dictionary
    public void InstantiatePieces()
    {
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
                isWhitesTurn = (isWhitesTurn) ? (false) : (true);
                EnablePieces();
                DisablePieces();
            }
            // If space is of your color... you can't move there
            else if (boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))].GetComponent<ChessPiece>() != null && boardStatus[(100 + (Mathf.RoundToInt(targetX) * 10) + Mathf.RoundToInt(targetY))].GetComponent<ChessPiece>().colorType == boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))].GetComponent<ChessPiece>().colorType)
            {
                boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))].transform.localPosition = new Vector3((Mathf.RoundToInt(selectedPieceX) * 5) + 3, -3.8f, (Mathf.RoundToInt(selectedPieceY) * 5) - 38);
            }
        } else
        {
            boardStatus[(100 + (Mathf.RoundToInt(selectedPieceX) * 10) + Mathf.RoundToInt(selectedPieceY))].transform.localPosition = new Vector3((Mathf.RoundToInt(selectedPieceX) * 5) + 3, -3.8f, (Mathf.RoundToInt(selectedPieceY) * 5) - 38);
        }
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
                    /*
                    if (boardStatus[100 + (testCoordX * 10) + testCoordY].GetComponent<ChessPiece>().colorType == pieceToMove.GetComponent<ChessPiece>().colorType)
                    {
                        return false;
                    }
                    */
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
