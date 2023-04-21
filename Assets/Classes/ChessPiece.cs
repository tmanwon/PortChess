/*
 * Tillman Won
 * AP CS50
 * Cmdr. Schenk
 * 5th Period
 * Master Project - Chess Piece Controller Class
 * 27 April 2023
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controller class managing lifespan of chess piece
public class ChessPiece : MonoBehaviour
{
    public string id; // ID of chess piece
    public PieceType pieceType; // Color of chess piece
    public ColorType colorType; // Type of chess piece
    public int x; // X Position on board
    public int y; // Y Position on board
    public OVRHand hand;
    public OVRSkeleton handSkeleton;
    public GameObject pieceToScale; // Reference to chess piece's mesh in scene
    private bool isSelected; // Is poked
    public int moves; // Moves piece has made

    // Constructor
    void Start()
    {
        isSelected = false;
        moves = 0;
        hand = hand.GetComponent<OVRHand>();
        handSkeleton = handSkeleton.GetComponent<OVRSkeleton>();
        if (!hand) hand = GetComponent<OVRHand>();
        if (!handSkeleton) handSkeleton = GetComponent<OVRSkeleton>();
    }

    // Update is called once per frame by Unity
    void Update()
    {
        // Chess piece is selected by user, move chess piece to user's index finger
        if (isSelected)
        {
            foreach (var bone in handSkeleton.Bones)
            {
                if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                {
                    float fingerY = bone.Transform.position.y;
                    transform.position = new Vector3(bone.Transform.position.x, transform.position.y, bone.Transform.position.z);
                }
            }
        }
    }

    // Find Closest Tile object to Chess Piece
    public GameObject FindClosestTile()
    {
        GameObject[] tiles;
        GameObject nearestTile = null;
        float distance;
        float nearestDistance = 100000;
        tiles = GameObject.FindGameObjectsWithTag("Tile");
        for (int i = 0; i < tiles.Length; i++)
        {
            distance = Vector3.Distance(transform.localPosition, tiles[i].transform.localPosition);
            if (distance < nearestDistance)
            {
                nearestTile = tiles[i];
                nearestDistance = distance;
            }
        }
        return nearestTile;
    }

    // Action event when user selects a piece
    public void Selected()
    {
        GameObject boardAnchor = GameObject.FindGameObjectWithTag("Anchor");
        // Tell chess game that this piece is selected
        if (boardAnchor.GetComponent<ChessGame>().selectedPiece == null)
        {
            boardAnchor.GetComponent<ChessGame>().selectedPiece = this.gameObject;
            boardAnchor.GetComponent<ChessGame>().selectedPieceX = (transform.localPosition.x - 3f) / 5f;
            boardAnchor.GetComponent<ChessGame>().selectedPieceY = (transform.localPosition.z + 38f) / 5f;
            print(boardAnchor.GetComponent<ChessGame>().selectedPieceX);
            print(boardAnchor.GetComponent<ChessGame>().selectedPieceY);
            isSelected = true;
        }
        
    }

    // Action event when user unselects piece
    public void Unselected()
    {
        isSelected = false;
        GameObject nearestTile = FindClosestTile();
        GameObject boardAnchor = GameObject.FindGameObjectWithTag("Anchor");
        transform.localPosition = new Vector3(nearestTile.transform.localPosition.x, transform.localPosition.y, nearestTile.transform.localPosition.z);
        print((nearestTile.transform.localPosition.x - 3f) / 5f);
        print((nearestTile.transform.localPosition.z + 38f) / 5f);
        boardAnchor.GetComponent<ChessGame>().OnMove((transform.localPosition.x - 3f) / 5f, (transform.localPosition.z + 38f) / 5f);
        boardAnchor.GetComponent<ChessGame>().selectedPiece = null;

    }

    // Button action event when user hovers over piece
    public void OnHover()
    {
        HighlightPossibleMoves();
    }

    // Button action event when user stops hovering over piece
    public void OnHoverExit()
    {
        deHighlight();
    }

    // Highlight possible places for piece to move if being hovered
    public void HighlightPossibleMoves()
    {
        GameObject[] tiles;
        tiles = GameObject.FindGameObjectsWithTag("Tile");
        for (int i = 0; i < tiles.Length; i++)
        {
            GameObject boardAnchor = GameObject.FindGameObjectWithTag("Anchor");
            float targetX = (tiles[i].GetComponent<Tile>().transform.localPosition.x - 3f) / 5f;
            float targetY = (tiles[i].GetComponent<Tile>().transform.localPosition.z + 38f) / 5f;
            float selectedX = (transform.localPosition.x - 3f) / 5f;
            float selectedY = (transform.localPosition.z + 38f) / 5f;
            bool kek = boardAnchor.GetComponent<ChessGame>().ValidateMove(this.gameObject, Mathf.RoundToInt(targetX), Mathf.RoundToInt(targetY), Mathf.RoundToInt(selectedX), Mathf.RoundToInt(selectedY));
            if (kek)
            {
                tiles[i].GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
            }
        }
    }

    // Unhighlight all places on board
    public void deHighlight()
    {
        GameObject[] tiles;
        tiles = GameObject.FindGameObjectsWithTag("Tile");
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");
        }
    }

}
