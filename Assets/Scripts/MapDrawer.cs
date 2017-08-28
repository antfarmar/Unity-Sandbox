using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDrawer : MonoBehaviour
{
    public GameObject wallTile;
    public GameObject floorTile;
    public GameObject roomMarkerTile;
    public GameObject edgeMarkerTile;
    public int width;
    public int height;
    public string seed;
    public bool useRandomSeed;
    [Range(0, 8)]
    public int wallThreshold;
    [Range(0, 10)]
    public int smoothingIterations;
    [Range(0, 100)]
    public int fillPercent;
    public bool connectRooms;
    public bool removeWalls;
    public bool removeRooms;
    public int wallSizeThreshold;
    public int roomSizeThreshold;

    private GameObject mapGO;
    // enum TILETYPE : int { WALL, FLOOR };
    MapGenerator MAP;


    void Start()
    {
        CreateNewMap();
        DrawMap();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject.Destroy(mapGO);
            CreateNewMap();
            DrawMap();
        }
    }

    void CreateNewMap()
    {
        MAP = new MapGenerator(width, height);
        if (useRandomSeed) { seed = Time.time.ToString(); }
        MAP.RandomFillMap(fillPercent, seed);
        MAP.SmoothMap(smoothingIterations, wallThreshold);
        if (removeWalls) { MAP.RemoveWallsBySize(wallSizeThreshold); }
        MAP.FindRoomsInMap();
        if (removeRooms) { MAP.RemoveRoomsBySize(roomSizeThreshold); }
        MAP.rooms.Sort();
        if (connectRooms) { MAP.ConnectAllRooms(); }
    }


    void DrawMap()
    {
        mapGO = new GameObject("Map");
        if (MAP != null)
        {
            // Draw background map.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GameObject toInstantiate = (MAP.map[x, y] == (int)MAP.TILETYPE.WALL) ? wallTile : floorTile;
                    Vector3 pos = new Vector3(-width / 2 + x, -height / 2 + y, 0);
                    GameObject instance = Instantiate(toInstantiate, pos, Quaternion.identity) as GameObject;
                    instance.transform.SetParent(mapGO.transform);
                }
            }

            // Draw room and edge tile markers.
            foreach (Room room in MAP.rooms)
            {
                foreach (Coord tile in room.tiles)
                {
                    Vector3 pos = new Vector3(-width / 2 + tile.x, -height / 2 + tile.y, 0);
                    GameObject instance = Instantiate(roomMarkerTile, pos, Quaternion.identity) as GameObject;
                    instance.transform.SetParent(mapGO.transform);
                }

                foreach (Coord tile in room.edgeTiles)
                {
                    Vector3 pos = new Vector3(-width / 2 + tile.x, -height / 2 + tile.y, 0);
                    GameObject instance = Instantiate(edgeMarkerTile, pos, Quaternion.identity) as GameObject;
                    instance.transform.SetParent(mapGO.transform);
                }
            }
        }
    }

    //Vector3 CoordToWorldPoint(Coord tile) { return new Vector3(-width / 2 + tile.x, -height / 2 + tile.y, 0); }
    
}