using System;
using System.Collections.Generic;

public class Room : IComparable<Room>
{
    public List<Coord> tiles;
    public List<Coord> edgeTiles;
    public int roomSize;

    public Room() { }

    public Room(List<Coord> roomTiles, int wallTile, int[,] map)
    {
        tiles = roomTiles;
        roomSize = tiles.Count;
        edgeTiles = FindEdgeTiles(roomTiles, wallTile, map);
    }

    public List<Coord> FindEdgeTiles(List<Coord> tiles, int wallTile, int[,] map)
    {
        List<Coord> edges = new List<Coord>();
        foreach (Coord tile in tiles)
        {
            bool found = false;
            for (int x = tile.x - 1; x <= tile.x + 1; x++)
            {
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    if (map[x, y] == wallTile)
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