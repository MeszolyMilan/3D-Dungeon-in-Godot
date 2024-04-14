using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;

public class DungGeneration
{
    public static RandomNumberGenerator RNG { get; private set; }
    public TileTypes[,] TileMap { get; private set; }
    public List<Building> Buildings { get; private set; }
    public List<Vector2[]> Hallways { get; private set; }
    public Vector2[] ConnPs { get; private set; }
    public int[] ConnPTris { get; private set; }
    public List<(long, long)> Conns { get; private set; }
    private int _spawnRadius;
    private int _buildingCount;
    private int _buildingMaxSize;
    private int _extraConnectionChance;
    private static Vector2I _zeroPos;
    public DungGeneration(ulong seed, int spawnRadius, int buildingCount, int buildingMaxSize, int extraConnectionChance)
    {
        RNG = new RandomNumberGenerator();
        if (seed != 0) { RNG.Seed = seed; }
        else { GD.Print("Seed: " + RNG.Seed); }
        _spawnRadius = spawnRadius;
        _buildingCount = buildingCount;
        _buildingMaxSize = buildingMaxSize;
        _extraConnectionChance = extraConnectionChance;
        GenerateBuildings();
        GenerateHallwayConnections();
        GenerateHallways();
    }
    public static void GetTileMapXY(Vector2I position, out int X, out int Y)
    {
        X = Mathf.Abs(position.X - _zeroPos.X);
        Y = Mathf.Abs(position.Y - _zeroPos.Y);
    }
    private Vector2I GetRandomPoint()
    {
        float r = _spawnRadius * Mathf.Sqrt(RNG.Randf());
        float t = RNG.Randf() * 2 * Mathf.Pi;
        int x = (int)(r * Mathf.Cos(t));
        int y = (int)(r * Mathf.Sin(t));
        return new Vector2I(x, y);
    }
    private void GenerateBuildings()
    {
        Buildings = new List<Building>();
        for (int i = 0; i < _buildingCount; i++)
        {
            Vector2I position = GetRandomPoint();
            Building building = new Building(position, _buildingMaxSize, $"B{i}");
            Buildings.Add(building);
        }
        //Handle overlap
        List<(Building, Building)> overlapped = new List<(Building, Building)>();
        do
        {
            overlapped.Clear();
            for (int x = 0; x < Buildings.Count - 1; x++)
            {
                var building1 = Buildings[x];
                for (int y = x + 1; y < Buildings.Count; y++)
                {
                    var building2 = Buildings[y];
                    if (building1.Rect.Intersects(building2.Rect))
                    {
                        overlapped.Add((building1, building2));
                    }
                }
            }
            foreach (var overlap in overlapped)
            {
                while (overlap.Item1.Rect.Intersects(overlap.Item2.Rect))
                {
                    Vector2I direction = overlap.Item1.Rect.GetCenter() - overlap.Item2.Rect.GetCenter();
                    overlap.Item1.Move(direction);
                    overlap.Item2.Move(-direction);
                }
            }
        }
        while (overlapped.Count > 0);

        foreach (var building in Buildings)
        {
            building.Replace();
        }
    }
    private void GenerateHallwayConnections()
    {
        Conns = new List<(long, long)>();
        ConnPs = new Vector2[Buildings.Count];
        AStar2D connectionGraph = new AStar2D();

        for (int i = 0; i < Buildings.Count; i++)
        {
            Vector2I pos = Buildings[i].Doors[0].Position;
            ConnPs[i] = pos;
            connectionGraph.AddPoint(i, pos);
        }
        ConnPTris = Geometry2D.TriangulateDelaunay(ConnPs);
        for (int i = 0; i < ConnPTris.Length - 2; i++)
        {
            int p0 = ConnPTris[i];
            int p1 = ConnPTris[i + 1];
            int p2 = ConnPTris[i + 2];
            //Hogy ne connecteljuk ugyan azt a 2 pontot 2x
            if (p0 != p1) { connectionGraph.ConnectPoints(p0, p1); }
            if (p1 != p2) { connectionGraph.ConnectPoints(p1, p2); }
            if (p0 != p2) { connectionGraph.ConnectPoints(p0, p2); }
        }

        List<long> visitedPs = new List<long> { RNG.RandiRange(0, ConnPs.Length - 1) };
        while (visitedPs.Count != Buildings.Count)
        {
            List<(long, long)> possibleConns = new List<(long, long)>();
            foreach (var visitedP in visitedPs.ToArray())
            {
                foreach (var connP in connectionGraph.GetPointConnections(visitedP))
                {
                    if (visitedPs.Contains(connP) == false)
                    {
                        possibleConns.Add((visitedP, connP));
                    }
                }
            }
            var shortestConn = possibleConns[0];
            for (int i = 1; i < possibleConns.Count; i++)
            {
                var connection = possibleConns[i];
                if (ConnPs[shortestConn.Item1].DistanceSquaredTo(ConnPs[shortestConn.Item2]) >
                ConnPs[connection.Item1].DistanceSquaredTo(ConnPs[connection.Item2]))
                {
                    shortestConn = connection;
                }
            }
            visitedPs.Add(shortestConn.Item2);
            Conns.Add(shortestConn);
            connectionGraph.DisconnectPoints(shortestConn.Item1, shortestConn.Item2);
        }
        //Extra connections
        foreach (var pointA in connectionGraph.GetPointIds())
        {
            foreach (var pointB in connectionGraph.GetPointConnections(pointA))
            {
                if (pointA > pointB) //Hogy ne connecteljuk ugyan azt a 2 pontot 2x
                {
                    //Add remaining connections with x chance
                    if (_extraConnectionChance >= RNG.RandiRange(0, 100))
                    {
                        Conns.Add((pointA, pointB));
                    }
                }
            }
        }
    }
    private void GenerateHallways()
    {
        Rect2I region = new Rect2I();
        foreach (var building in Buildings)
        {
            region = region.Merge(building.Rect);
        }
        region = new Rect2I(region.Position - Vector2I.One, region.Size + new Vector2I(2, 2));
        _zeroPos = region.Position;
        AStarGrid2D grid = new AStarGrid2D()
        {
            Region = region,
            DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never,
            DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan,
            DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan
        };
        grid.Update();
        TileMap = new TileTypes[region.Size.X, region.Size.Y];

        foreach (var building in Buildings)
        {
            grid.FillSolidRegion(building.Rect);
            int absX = 0;
            int absY = 0;
            Building.Door mainDoor = building.Doors[0];
            if (mainDoor.Direction == Vector2I.Left || mainDoor.Direction == Vector2I.Up)
            {
                absX = Math.Abs(building.Rect.Position.X - mainDoor.Position.X);
                absY = Math.Abs(building.Rect.Position.Y - mainDoor.Position.Y);
            }
            else if (mainDoor.Direction == Vector2I.Right || mainDoor.Direction == Vector2I.Down)
            {
                absX = Math.Abs(building.Rect.End.X - mainDoor.Position.X);
                absY = Math.Abs(building.Rect.End.Y - mainDoor.Position.Y);
            }
            for (int x = 0; x <= absX; x++)
            {
                for (int y = 0; y <= absY; y++)
                {
                    grid.SetPointSolid(building.Doors[0].Position + new Vector2I(x, y) * building.Doors[0].Direction, false);
                }
            }
            building.WriteTo(TileMap);
        }
        Hallways = new List<Vector2[]>();
        foreach (var conn in Conns)
        {
            Vector2[] path = grid.GetPointPath((Vector2I)ConnPs[conn.Item1], (Vector2I)ConnPs[conn.Item2]);
            Hallways.Add(path);
            for (int i = 1; i < path.Length - 1; i++)
            {
                GetTileMapXY((Vector2I)path[i], out int X, out int Y);
                TileMap[X, Y] = TileTypes.HALLWAY;
            }
        }
    }
}