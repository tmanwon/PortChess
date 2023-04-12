using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class ChessBoard : MonoBehaviour
{
    // Fields of the class
    [SerializeField]
    private GameObject parent;
    [SerializeField]
    private GameObject DarkTilePrefab;
    [SerializeField]
    private GameObject LightTilePrefab;
    public GameObject[,] tiles;
    public Dictionary<string, GameObject> BoardDictionary;
    [SerializeField]
    public GameObject OrbitPoint;
    public float OrbitRadius;
    public float OrbitSpeed;
    public float CastRadius;
    public bool RandomPosAcquired;
    public LayerMask Avoid;
    private Vector3 RandomInSphere;
    public bool Orbiting;


    // Start is called before the first frame update (ie. Contructor)
    void Start()
    {
        Orbiting = true;
        GenerateAllTiles(1, 8, 8);
        //EndOrbit();
        //parent.GetComponent<ChessGame>().StartGame();

    }

    // Update is called once per frame
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
