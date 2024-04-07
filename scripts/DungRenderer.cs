using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class DungRenderer : Node
{
    private Dictionary<TileTypes, int> GenerateMeshTable(MeshLibrary meshs)
    {
        var table = new Dictionary<TileTypes, int>();
        foreach (var id in meshs.GetItemList())
        {
            switch (meshs.GetItemName(id))
            {
                case "border": table.Add(TileTypes.BORDER, id); break;
                case "door": table.Add(TileTypes.DOOR, id); break;
                case "floor": table.Add(TileTypes.FLOOR, id); break;
                case "wall": table.Add(TileTypes.WALL, id); break;
            }
        }
        return table;
    }
    public void Generate(DungGeneration generation)
    {
        GridMap gridMap = GetNode("GridMap") as GridMap;
        gridMap.Clear();
        var meshTable = GenerateMeshTable(gridMap.MeshLibrary);
        // foreach (var building in generation.Buildings)
        // {
        //     var tiles = building.GetTiles();
        //     for (int x = 0; x < building.MainRoom.Size.X; x++)
        //     {
        //         for (int y = 0; y < building.MainRoom.Size.Y; y++)
        //         {
        //             int idx = x * building.MainRoom.Size.Y + y;
        //             meshTable.TryGetValue(tiles[idx], out var tile);
        //             gridMap.SetCellItem(new Vector3I(building.MainRoom.Position.X + x, 0, building.MainRoom.Position.Y + y), tile);
        //         }
        //     }
        // }
    }

}
