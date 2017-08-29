using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class MapGenerator
{
    public int width;
    public int height;
    public int[,] map;
    public List<Room> rooms;
    public enum TILETYPE : int { WALL, FLOOR };



    public MapGenerator(int width, int height)
    {
        this.width = width;
        this.height = height;
        map = new int[width, height];
    }


    public void RandomFillMap(int fillPercent, string seed)
    {
        System.Random rand = new System.Random(seed.GetHashCode());
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                map[x, y] = (rand.Next(1, 101) < fillPercent) ? (int)TILETYPE.WALL : (int)TILETYPE.FLOOR;
            }
        }
    }

    public void SmoothMap(int iterations, int threshold)
    {
        for (int i = 0; i < iterations; i++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int neighbourWallTiles = GetSurroundingWallCount(x, y);
                    if (neighbourWallTiles > threshold)
                        map[x, y] = (int)TILETYPE.WALL;
                    else if (neighbourWallTiles < threshold)
                        map[x, y] = (int)TILETYPE.FLOOR;

                }
            }
        }
    }


    public void ConnectAllRooms()
    {
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();

        // while (allRooms.Count > 1)
        for (int i = rooms.Count; i > 1; i--)
        {
            int bestDistance = Int32.MaxValue;

            foreach (Room roomA in rooms)
            {
                foreach (Room roomB in rooms)
                {
                    if (roomA == roomB) { continue; }

                    for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                    {
                        for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                        {
                            Coord tileA = roomA.edgeTiles[tileIndexA];
                            Coord tileB = roomB.edgeTiles[tileIndexB];
                            int distanceBetweenRooms = (int)(Math.Pow(tileA.x - tileB.x, 2) + Math.Pow(tileA.y - tileB.y, 2));

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
        //Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 2, false);
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

        int longest = Math.Abs(dx);
        int shortest = Math.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Math.Abs(dy);
            shortest = Math.Abs(dx);

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


    public List<Room> FindRoomsInMap()
    {
        List<Room> rooms = new List<Room>();
        List<List<Coord>> roomRegions = GetRegions((int)TILETYPE.FLOOR);
        foreach (List<Coord> roomRegion in roomRegions)
        {
            rooms.Add(new Room(roomRegion, (int)TILETYPE.WALL, map));
        }
        this.rooms = rooms;
        return rooms;
    }


    public void RemoveRoomsBySize(int size)
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


    public void RemoveWallsBySize(int size)
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

    bool IsInMapRange(int x, int y) { return x >= 0 && x < width && y >= 0 && y < height; }
    
}