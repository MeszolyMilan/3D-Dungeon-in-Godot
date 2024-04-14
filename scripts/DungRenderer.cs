using Godot;
using System;
using System.Collections.Generic;

public partial class DungRenderer : Node3D
{
    [Export] private PackedScene _dungEditorScene;
    [Export] private PackedScene _buildingCellScene;
    [Export] private PackedScene _hallwayCellScene;
    [Export] private PackedScene _doorScene;
    [Export] private Camera3D _cam;
    private const int CELL_SIZE = 2;
    public override void _Ready()
    {
        Render();
    }
    public void Render()
    {
        DungGeneration gen = _dungEditorScene.Instantiate<DungGenEditor>().Generate();
        int width = gen.TileMap.GetLength(0);
        int height = gen.TileMap.GetLength(1);
        //Cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileTypes cellType = gen.TileMap[x, y];
                if (cellType == TileTypes.EMPTY) { continue; }
                Cell cell = null;
                if (cellType == TileTypes.WALL)
                {
                    cell = _buildingCellScene.Instantiate<Cell>();
                    Cell.Neighbours neighbours = new Cell.Neighbours()
                    {
                        Left = x == 0 || gen.TileMap[x - 1, y] == TileTypes.WALL,
                        Right = x == width - 1 || gen.TileMap[x + 1, y] == TileTypes.WALL,
                        Up = y == 0 || gen.TileMap[x, y - 1] == TileTypes.WALL,
                        Down = y == height - 1 || gen.TileMap[x, y + 1] == TileTypes.WALL
                    };
                    cell.RemoveFaces(neighbours);
                }
                else if (cellType == TileTypes.FLOOR)
                {
                    cell = _buildingCellScene.Instantiate<Cell>();
                    Cell.Neighbours neighbours = new Cell.Neighbours()
                    {
                        Left = true,
                        Right = true,
                        Up = true,
                        Down = true
                    };
                    cell.RemoveFaces(neighbours);
                }
                else if (cellType == TileTypes.HALLWAY)
                {
                    cell = _hallwayCellScene.Instantiate<Cell>();
                    Cell.Neighbours neighbours = new Cell.Neighbours()
                    {
                        Left = x == 0 ? false : gen.TileMap[x - 1, y] != TileTypes.EMPTY,
                        Right = x == width - 1 ? false : gen.TileMap[x + 1, y] != TileTypes.EMPTY,
                        Up = y == 0 ? false : gen.TileMap[x, y - 1] != TileTypes.EMPTY,
                        Down = y == height - 1 ? false : gen.TileMap[x, y + 1] != TileTypes.EMPTY
                    };
                    cell.RemoveFaces(neighbours);
                }
                cell.Position = new Vector3(x, 0, y) * CELL_SIZE;
                AddChild(cell);
            }
        }
        //Door
        foreach (var building in gen.Buildings)
        {
            foreach (var door in building.Doors)
            {
                DungGeneration.GetTileMapXY(door.Position, out int X, out int Y);
                Node3D node = _doorScene.Instantiate<Node3D>();
                node.Position = new Vector3(X, 0, Y) * CELL_SIZE + new Vector3(door.Direction.X * 0.8f, 1, door.Direction.Y * 0.8f);
                if (door.Direction == Vector2I.Up || door.Direction == Vector2I.Down)
                {
                    node.RotateY(Mathf.DegToRad(90));
                }
                AddChild(node);
            }
        }
        DungGeneration.GetTileMapXY(gen.Buildings[0].Rooms[0].GetCenter(), out int spawnX, out int spawnY);
        _cam.Position = new Vector3(spawnX, 0, spawnY) * CELL_SIZE + new Vector3(0, 1, 0);
    }
}
