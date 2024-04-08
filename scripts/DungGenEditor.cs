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
        get => _showTriangles;
        set
        {
            _showTriangles = value;
            QueueRedraw();
        }
    }
    [Export(PropertyHint.Range, "0,100,1,")]
    private int _extraConnectionChance_
    {
        get => _extraConnectionChance;
        set
        {
            _extraConnectionChance = value;
            Generate();
        }
    }
    [Export]
    private bool _showConnections_
    {
        get => _showConnections;
        set
        {
            _showConnections = value;
            QueueRedraw();
        }
    }
    [Export]
    private bool _showHallways_
    {
        get => _showHallways;
        set
        {
            _showHallways = value;
            if (value) { _showConnections = false; _showTriangles = false; }
            QueueRedraw();
        }
    }
    private int _spawnCircleSize = 128;
    private int _buildingSize = 32;
    private int _buildingCount = 3;
    private int _extraConnectionChance = 0;
    private ulong _seed;
    private DungGeneration _gen;
    private bool _showTriangles;
    private bool _showConnections;
    private bool _showHallways;
    private void Generate()
    {
        _gen = new DungGeneration(_seed, _spawnCircleSize, _buildingCount, _buildingSize, _extraConnectionChance);
        QueueRedraw();
        //GetParent<DungRenderer>().Generate(_gen);
    }
    public override void _Draw()
    {
        if (_gen == null || Engine.IsEditorHint() == false) { return; }

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
                DrawCircle(door.Position, 2, Colors.Green);
            }
        }
        if (_showTriangles)
        {
            for (int i = 0; i < _gen.ConnPTris.Length - 2; i++)
            {
                DrawLine(_gen.ConnPs[_gen.ConnPTris[i]], _gen.ConnPs[_gen.ConnPTris[i + 1]], Colors.Yellow, 0.5f);
                DrawLine(_gen.ConnPs[_gen.ConnPTris[i + 1]], _gen.ConnPs[_gen.ConnPTris[i + 2]], Colors.Yellow, 0.5f);
                DrawLine(_gen.ConnPs[_gen.ConnPTris[i]], _gen.ConnPs[_gen.ConnPTris[i + 2]], Colors.Yellow, 0.5f);
            }
        }
        if (_showConnections)
        {
            foreach (var conn in _gen.Conns)
            {
                DrawLine(_gen.ConnPs[conn.Item1], _gen.ConnPs[conn.Item2], Colors.Yellow, 2f);
            }
        }
        if (_showHallways)
        {
            Vector2 offset = new Vector2(0.5f, 0.5f);
            foreach (var hallway in _gen.Hallways)
            {
                for (int i = 0; i < hallway.Length - 1; i++)
                {
                    DrawLine(hallway[i] + offset, hallway[i + 1] + offset, Colors.Yellow, 1);
                }
            }
        }
    }
}
