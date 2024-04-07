using Godot;
using System;
[Tool]
public partial class DungGenEditor : Node2D
{
    [ExportCategory("Generation Modifiers")]
    [Export]
    private ulong _seed_
    {
        get => _seed;
        set
        {
            _seed = value;
            Generate();
        }
    }
    [Export(PropertyHint.Range, "2,1024,1,or_greater")]
    private int _spawnCircleSize_
    {
        get => _spawnCircleSize;
        set
        {
            _spawnCircleSize = value;
            Generate();
        }
    }
    [Export(PropertyHint.Range, "3,256,1,")]
    private int _buildingSize_
    {
        get => _buildingSize;
        set
        {
            _buildingSize = value;
            Generate();
        }
    }
    [Export(PropertyHint.Range, "3,30,1,")]
    private int _buildingCount_
    {
        get => _buildingCount;
        set
        {
            _buildingCount = value;
            Generate();
        }
    }
    [Export]
    private bool _generate
    {
        get => false;
        set
        {
            if (value)
            {
                Generate();
            }
        }
    }
    [Export]
    private bool _showTriangles_
    {
        get
        {
            return _showTriangles;
        }
        set
        {
            QueueRedraw();
            _showTriangles = value;
        }
    }
    private int _spawnCircleSize = 128;
    private int _buildingSize = 32;
    private int _buildingCount = 3;
    private ulong _seed;
    private DungGeneration _gen;
    private bool _showTriangles;
    private void Generate()
    {
        _gen = new DungGeneration(_seed, _spawnCircleSize, _buildingCount, _buildingSize);
        if (Engine.IsEditorHint()) { QueueRedraw(); }
        //GetParent<DungRenderer>().Generate(_gen);
    }
    public override void _Draw()
    {
        if (_gen == null) { return; }
        Color circleColor = Colors.White;
        circleColor.A = 0.25f;
        DrawArc(Position, _spawnCircleSize, 0, 360, 36, circleColor, 8);

        foreach (var building in _gen.Buildings)
        {
            //Rooms
            foreach (var room in building.Rooms)
            {
                Color color = room == building.Rooms[0] ? Colors.Red : Colors.Blue;
                DrawRect(room, color);
            }
            //Building rect
            Color rectColor = Colors.White;
            rectColor.A = 0.5f;
            DrawRect(building.Rect, rectColor, false, 0.5f);
            //Doors
            foreach (var door in building.Doors)
            {
                DrawCircle(door.Position, 0.5f, Colors.Green);
            }
        }
        if (_showTriangles)
        {
            Color trisColor = Colors.Yellow;
            trisColor.A = 0.5f;
            for (int i = 0; i < _gen.BuildingTris.Length - 2; i++)
            {
                DrawLine(_gen.MainDoors[_gen.BuildingTris[i]], _gen.MainDoors[_gen.BuildingTris[i + 1]], trisColor, 0.5f);
                DrawLine(_gen.MainDoors[_gen.BuildingTris[i + 1]], _gen.MainDoors[_gen.BuildingTris[i + 2]], trisColor, 0.5f);
                DrawLine(_gen.MainDoors[_gen.BuildingTris[i]], _gen.MainDoors[_gen.BuildingTris[i + 2]], trisColor, 0.5f);
            }
        }
    }
}
