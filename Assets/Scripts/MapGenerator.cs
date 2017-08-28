using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
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
    public int initialFillPercent;

    public bool connectRooms;
    public bool removeWalls;
    public bool removeRooms;
    public int wallSizeThreshold;
    public int roomSizeThreshold;
    private GameObject mapGO;

    enum TILETYPE : int { WALL, FLOOR };

    int[,] map;
    List<Room> rooms;



    // ________________________ METHODS ____________________________
    void Start()
    {
        GenerateMap();
        DrawMap();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject.Destroy(mapGO);
            GenerateMap();
            DrawMap();
        }
    }

    void GenerateMap()
    {
        map = new int[width, height];
        RandomFillMap();
        SmoothMap(smoothingIterations);
        ProcessMap();
    }


    void RandomFillMap()
    {
        if (useRandomSeed) { seed = Time.time.ToString(); }
        System.Random rand = new System.Random(seed.GetHashCode());
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                map[x, y] = (rand.Next(1, 101) < initialFillPercent) ? (int)TILETYPE.WALL : (int)TILETYPE.FLOOR;
            }
        }
    }

    void SmoothMap(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int neighbourWallTiles = GetSurroundingWallCount(x, y);
                    if (neighbourWallTiles > wallThreshold)
                        map[x, y] = (int)TILETYPE.WALL;
                    else if (neighbourWallTiles < wallThreshold)
                        map[x, y] = (int)TILETYPE.FLOOR;

                }
            }
        }
    }



    void ProcessMap()
    {
        if (removeWalls) { RemoveWallsBySize(wallSizeThreshold); }
        rooms = FindRoomsInMap();
        if (removeRooms) { RemoveRoomsBySize(roomSizeThreshold); }
        rooms.Sort();
        if (connectRooms) { ConnectAllRooms(rooms); }
    }



    void ConnectAllRooms(List<Room> allRooms)
    {
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();

        // while (allRooms.Count > 1)
        for (int i = allRooms.Count; i > 1; i--)
        {
            int bestDistance = Int32.MaxValue;

            foreach (Room roomA in allRooms)
            {
                foreach (Room roomB in allRooms)
                {
                    if (roomA == roomB) { continue; }

                    for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                    {
                        for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                        {
                            Coord tileA = roomA.edgeTiles[tileIndexA];
                            Coord tileB = roomB.edgeTiles[tileIndexB];
                            int distanceBetweenRooms = (int)(Mathf.Pow(tileA.x - tileB.x, 2) + Mathf.Pow(tileA.y - tileB.y, 2));

                            if (distanceBetweenRooms < bestDistance)
                            {
                                bestDistance = distanceBetweenRooms;
                                bestTileA = tileA;
                                bestTileB = tileB;
                                bestRoomA = roomA;
                                bestRoomB = roomB;
                            }
                        }
                    }
                }
            }
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
        }
    }

    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 2, false);
        Room.MakeSingleRoom(roomA, roomB);
        rooms.Remove(roomB);

        List<Coord> path = GetPath(tileA, tileB);
        foreach (Coord c in path) { DrawCircle(c, 1); }
    }

    void DrawCircle(Coord c, int r)
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r * r)
                {
                    int drawX = c.x + x;
                    int drawY = c.y + y;
                    if (IsInMapRange(drawX, drawY))
                    {
                        map[drawX, drawY] = (int)TILETYPE.FLOOR;
                    }
                }
            }
        }
    }

    List<Coord> GetPath(Coord from, Coord to)
    {
        List<Coord> line = new List<Coord>();

        int x = from.x;
        int y = from.y;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x, y));

            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }

        return line;
    }



    List<Room> FindRoomsInMap()
    {
        List<Room> rooms = new List<Room>();
        List<List<Coord>> roomRegions = GetRegions((int)TILETYPE.FLOOR);
        foreach (List<Coord> roomRegion in roomRegions)
        {
            rooms.Add(new Room(roomRegion, map));
        }
        return rooms;
    }



    void RemoveRoomsBySize(int size)
    {
        List<Room> roomsToRemove = new List<Room>();
        foreach (Room room in rooms)
        {
            if (room.tiles.Count < size)
            {
                roomsToRemove.Add(room);
                foreach (Coord tile in room.tiles)
                {
                    map[tile.x, tile.y] = (int)TILETYPE.WALL;
                }
            }
        }

        foreach (Room room in roomsToRemove)
        {
            rooms.Remove(room);
        }
    }





    void RemoveWallsBySize(int size)
    {
        List<List<Coord>> wallRegions = GetRegions((int)TILETYPE.WALL);
        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < size)
            {
                foreach (Coord tile in wallRegion)
                {
                    map[tile.x, tile.y] = (int)TILETYPE.FLOOR;
                }
            }
        }
    }


    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        bool[,] visited = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!visited[x, y] && map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion)
                    {
                        visited[tile.x, tile.y] = true;
                    }
                }
            }
        }

        return regions;
    }


    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        bool[,] visited = new bool[width, height];
        int tileType = map[startX, startY];

        // Flood-fill algorithm
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.x - 1; x <= tile.x + 1; x++)
            {
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.y || x == tile.x))
                    {
                        if (!visited[x, y] && map[x, y] == tileType)
                        {
                            visited[x, y] = true;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    // ______________________ HELPER FUNCTIONS ___________________

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (IsInMapRange(neighbourX, neighbourY))
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        if (map[neighbourX, neighbourY] == (int)TILETYPE.WALL)
                            wallCount++;
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }



    void DrawMap()
    {
        mapGO = new GameObject("Map");
        if (map != null)
        {
            // Draw background map.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GameObject toInstantiate = (map[x, y] == (int)TILETYPE.WALL) ? wallTile : floorTile;
                    Vector3 pos = new Vector3(-width / 2 + x, -height / 2 + y, 0);
                    GameObject instance = Instantiate(toInstantiate, pos, Quaternion.identity) as GameObject;
                    instance.transform.SetParent(mapGO.transform);
                }
            }

            // Draw room and edge tile markers.
            foreach (Room room in rooms)
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


    bool IsInMapRange(int x, int y) { return x >= 0 && x < width && y >= 0 && y < height; }
    Vector3 CoordToWorldPoint(Coord tile) { return new Vector3(-width / 2 + tile.x, -height / 2 + tile.y, 0); }


    // ___________________________________________ INNER CLASS

    struct Coord
    {
        public int x;
        public int y;

        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }


    class Room : IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public int roomSize;

        public Room() { }

        public Room(List<Coord> roomTiles, int[,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            edgeTiles = FindEdgeTiles(roomTiles, map);
        }

        public List<Coord> FindEdgeTiles(List<Coord> tiles, int[,] map)
        {
            List<Coord> edges = new List<Coord>();
            foreach (Coord tile in tiles)
            {
                bool found = false;
                for (int x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (int y = tile.y - 1; y <= tile.y + 1; y++)
                    {
                        if (map[x, y] == (int)TILETYPE.WALL)
                        {
                            // Skip diagonals.
                            if (x == tile.x || y == tile.y)
                            {
                                edges.Add(tile);                                
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found) break;
                }
            }
            return edges;
        }



        public static void MakeSingleRoom(Room roomA, Room roomB)
        {
            roomA.roomSize += roomB.roomSize;
            foreach (Coord tile in roomB.tiles)
            {
                roomA.tiles.Add(tile);
            }

            foreach (Coord edgeTile in roomB.edgeTiles)
            {
                roomA.edgeTiles.Add(edgeTile);
            }
        }


        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }
    }

} // END MAPGENERATOR