using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    public string id;
    public PieceType pieceType;
    public ColorType colorType;
    public int x;
    public int y;
    public OVRHand hand;
    public OVRSkeleton handSkeleton;
    public GameObject pieceToScale;
    private bool isSelected;
    private bool isHovering;
    private float initialPosition;
    public int moves;

    // Start is called before the first frame update
    void Start()
    {
        isSelected = false;
        isHovering = false;
        moves = 0;
        hand = hand.GetComponent<OVRHand>();
        handSkeleton = handSkeleton.GetComponent<OVRSkeleton>();
        if (!hand) hand = GetComponent<OVRHand>();
        if (!handSkeleton) handSkeleton = GetComponent<OVRSkeleton>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isHovering)
        {
            foreach (var bone in handSkeleton.Bones)
            {
                if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                {
                    float fingerY = bone.Transform.position.y;
                    float yTravel = Mathf.Abs(initialPosition - fingerY);
                    float scaleFactor = ((-0.9f / 0.047f) * yTravel) + 1;
                    pieceToScale.transform.localScale = new Vector3(1, scaleFactor, 1);
                }
            }
        }

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

    public void Selected()
    {
        GameObject boardAnchor = GameObject.FindGameObjectWithTag("Anchor");
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

    public void OnHover()
    {
        //isHovering = true;
        HighlightPossibleMoves();
        /*
        foreach (var bone in handSkeleton.Bones)
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
            {
                initialPosition = bone.Transform.position.y;
            }
        }
        */
    }

    public void OnHoverExit()
    {
        deHighlight();
    }

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
