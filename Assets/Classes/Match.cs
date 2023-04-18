using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match : MonoBehaviour
{
    public int match_id;
    private GameObject boardAnchor;

    // Constructor
    void Start()
    {
        boardAnchor = GameObject.FindGameObjectWithTag("Anchor");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void analyzeGame()
    {
        boardAnchor.GetComponent<ChessGame>().AnalyzeSelectedGame(match_id);
    }
}
