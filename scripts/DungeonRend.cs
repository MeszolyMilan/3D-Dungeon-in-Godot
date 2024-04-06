using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class DungeonRend : Node
{
    #region  EDITOR CODE
    [ExportCategory("Generation Modifiers")]
    [Export(PropertyHint.Range, "64,1024,64,or_greater")]
    private int _borderSize_
    {
        get => _borderSize;
        set
        {
            _borderSize = value;
            GenerateNew();
        }
    }
    [Export(PropertyHint.Range, "32,256,8,or_greater")]
    private int _buildingSize_
    {
        get => _buildingSize;
        set
        {
            if (value >= _borderSize_) { return; }
            _buildingSize = value;
            GenerateNew();
        }
    }
    [Export]
    private bool _generateNew
    {
        get => false;
        set
        {
            if (value)
            {
                GenerateNew();
            }
        }
    }
    private void GenerateNew()
    {
        _gridMap ??= GetNode("GridMap") as GridMap;
        if (_meshTable == null) { GenerateMeshTable(_gridMap.MeshLibrary); }
        RenderBorder();
        Generate();

    }
    private void RenderBorder()
    {
        _gridMap.Clear();
        if (_meshTable.TryGetValue(TileTypes.BORDER, out int idx))
        {
            for (int i = -1; i <= _borderSize; i++)
            {
                _gridMap.SetCellItem(new Vector3I(i, 0, -1), idx);
                _gridMap.SetCellItem(new Vector3I(-1, 0, i), idx);
                _gridMap.SetCellItem(new Vector3I(_borderSize, 0, i), idx);
                _gridMap.SetCellItem(new Vector3I(i, 0, _borderSize), idx);
            }
        }
    }
    private List<MeshInstance3D> meshs = new List<MeshInstance3D>();
    private void DebugRenderRects(List<DungeonGen.Rect> rects)
    {
        foreach (var item in meshs)
        {
            item.QueueFree();
        }
        meshs.Clear();
        RandomNumberGenerator rng = new RandomNumberGenerator();
        for (int i = 0; i < 4; i++)
        {
            var rect = rects[rng.RandiRange(0, rects.Count - 1)];
            MeshInstance3D meshInst = new MeshInstance3D();
            BoxMesh mesh = new BoxMesh();
            OrmMaterial3D material = new OrmMaterial3D();
            material.AlbedoColor = new Color(rng.RandfRange(0, 1), rng.RandfRange(0, 1), rng.RandfRange(0, 1), 1);
            mesh.Material = material;
            mesh.Size = new Vector3(rect.Size.X, rng.RandiRange(1, 20), rect.Size.Y);
            meshInst.Mesh = mesh;
            meshInst.Position = new Vector3(rect.StartPos.X + rect.Size.X / 2, 0, rect.StartPos.Y + rect.Size.Y / 2);
            GetTree().EditedSceneRoot.AddChild(meshInst);
            meshs.Add(meshInst);
            rects.Remove(rect);
        }

    }
    #endregion
    private GridMap _gridMap;
    private Dictionary<TileTypes, int> _meshTable = null;
    private int _borderSize = 64;
    private int _buildingSize = 32;
    private void GenerateMeshTable(MeshLibrary meshs)
    {
        _meshTable = new Dictionary<TileTypes, int>();
        foreach (var id in meshs.GetItemList())
        {
            switch (meshs.GetItemName(id))
            {
                case "border": _meshTable.Add(TileTypes.BORDER, id); break;
                case "door": _meshTable.Add(TileTypes.DOOR, id); break;
                case "floor": _meshTable.Add(TileTypes.FLOOR, id); break;
                case "wall": _meshTable.Add(TileTypes.WALL, id); break;
            }
        }
        GD.Print("Mesh Table Generated");
    }
    private void Generate()
    {
        DungeonGen gen = new DungeonGen(_borderSize, _buildingSize);
        DebugRenderRects(gen.Rects);
    }

}
