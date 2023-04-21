/*
 * William Gonzalez
 * AP CS50
 * Cmdr. Schenk
 * 5th Period
 * Master Project (PortChess) - Match POCO
 * 27 April 2023
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base class modeling the database record of a chess match
public class Match : MonoBehaviour
{
    public int match_id;
    private GameObject boardAnchor; // ChessGame Object

    // Constructor
    void Start()
    {
        boardAnchor = GameObject.FindGameObjectWithTag("Anchor");
    }

    // Analyze Button Action
    public void analyzeGame()
    {
        boardAnchor.GetComponent<ChessGame>().AnalyzeSelectedGame(match_id);
    }
}
