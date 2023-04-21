/*
 * Tillman Won
 * AP CS50
 * Cmdr. Schenk
 * 5th Period
 * Master Project - Chess Board Controller Class
 * 27 April 2023
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

// Controller class managing the life cycle of the chess board
public class ChessBoard : MonoBehaviour
{
    // Fields of the class
    [SerializeField]
    private GameObject parent; // Board anchor primative in unity editor
    [SerializeField]
    private GameObject DarkTilePrefab; // Predefined Dark tile material
    [SerializeField]
    private GameObject LightTilePrefab; // Predefined Light tile material
    public GameObject[,] tiles; // 2D Array tracking each Tile object
    public bool Orbiting;


    // Constructor
    void Start()
    {
        Orbiting = true;
        GenerateAllTiles(1, 8, 8);
    }

    // Update is called once per frame by Unity
    void Update()
    {
        // Stop orbit if game started
        if (!Orbiting) {
            EndOrbit();
        }
    }

    // Place all tiles
    void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateTile(tileSize, x, y);
    }

    // Manages placement for individual tiles
    GameObject GenerateTile(float tileSize, int x, int y)
    {
        GameObject tileObject;
        if (y % 2 == 0 && x % 2 == 0)
        {
            tileObject = Instantiate(DarkTilePrefab);
        } else if (y % 2 == 0 && x % 2 != 0)
        {
            tileObject = Instantiate(LightTilePrefab);
        } else if (y % 2 != 0 && x % 2 == 0) {
            tileObject = Instantiate(LightTilePrefab);
        } else if (y % 2 != 0 && x % 2 != 0) {
            tileObject = Instantiate(DarkTilePrefab);
        } else
        {
            tileObject = Instantiate(LightTilePrefab);
        }
        tileObject.name = string.Format("{0}, {1}", x, y);
        tileObject.GetComponent<Tile>().x = x;
        tileObject.GetComponent<Tile>().y = y;
        tileObject.transform.parent = parent.transform;
        tileObject.transform.localPosition = new Vector3((x * 5) + 3f, -0.49f, (y * 5) - 38f);

        return tileObject;
    }

    // Orbits each tile around Orbit Point
    public void Orbit()
    {
        Orbiting = true;
        foreach (GameObject tile in tiles)
        {
            tile.gameObject.GetComponent<Tile>().StartOrbit();

        }
    }

    // Stops orbit and return tiles to board placement
    public void EndOrbit() {
        Orbiting = false;
        foreach (GameObject tile in tiles)
        {
            tile.gameObject.GetComponent<Tile>().StopOrbit();
            Vector3 newPos = new Vector3((tile.gameObject.GetComponent<Tile>().x * 5) + 3f, -0.49f, (tile.gameObject.GetComponent<Tile>().y * 5) - 38f);
            tile.gameObject.transform.localPosition = Vector3.Lerp(tile.transform.localPosition, newPos, 5f * Time.deltaTime);
            tile.gameObject.transform.localRotation = Quaternion.Lerp(tile.gameObject.transform.rotation, Quaternion.identity, 5f * Time.deltaTime);
        }
        Orbiting = checkIfOrbitReturnFinished();
    }

    // Checks if tile reached it's board position
    bool checkIfOrbitReturnFinished() {
        foreach (GameObject tile in tiles) {
            Vector3 newPos = new Vector3((tile.gameObject.GetComponent<Tile>().x * 5) + 3f, -0.49f, (tile.gameObject.GetComponent<Tile>().y * 5) - 38f);
            
            if (tile.gameObject.transform.localPosition != newPos) {
                return false;
            }
        }
        return true;
    }

}
