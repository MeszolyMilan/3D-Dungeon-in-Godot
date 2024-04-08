using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class DungGeneration
{
    public static RandomNumberGenerator RNG { get; private set; }
    public List<Building> Buildings { get; private set; }
    public List<Vector2[]> Hallways { get; private set; }
    public Vector2[] ConnPs { get; private set; }
    public int[] ConnPTris { get; private set; }
    public List<(long, long)> Conns { get; private set; }
    public AStarGrid2D Test { get; set; }
    private int _spawnRadius;
    private int _buildingCount;
    private int _buildingMaxSize;
    private int _extraConnectionChance;

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
            Building building = new Building(position, _buildingMaxSize);
            Buildings.Add(building);
        }
        //Move if overlapped
        while (true)
        {
            bool overlaping = false;
            foreach (var building1 in Buildings)
            {
                foreach (var building2 in Buildings)
                {
                    if (building1 == building2 || building1.Rect.Intersects(building2.Rect) == false) { continue; }
                    overlaping = true;
                    Vector2I center1 = building1.Rect.GetCenter();
                    Vector2I center2 = building2.Rect.GetCenter();

                    if (center1.LengthSquared() < center2.LengthSquared())
                    {
                        Vector2 direction = center2 - center1;
                        building2.Move((Vector2I)direction);
                    }
                    else
                    {
                        Vector2 direction = center1 - center2;
                        building1.Move((Vector2I)direction);
                    }
                }
            }
            if (overlaping == false) { break; }
        }
        foreach (var building in Buildings)
        {
            building.Replace();
        }
    }
    private void GenerateHallwayConnections()
    {
        Conns = new List<(long, long)>();
        ConnPs = new Vector2[Buildings.Count];
        AStar2D dealunayGraph = new AStar2D();
        AStar2D hallwayGraph = new AStar2D(); //Minimum Spending Tree

        for (int i = 0; i < Buildings.Count; i++)
        {
            Vector2I pos = Buildings[i].Doors[0].Position;
            ConnPs[i] = pos;
            dealunayGraph.AddPoint(i, pos);
            hallwayGraph.AddPoint(i, pos);
        }
        ConnPTris = Geometry2D.TriangulateDelaunay(ConnPs);
        for (int i = 0; i < ConnPTris.Length - 2; i++)
        {
            int p0 = ConnPTris[i];
            int p1 = ConnPTris[i + 1];
            int p2 = ConnPTris[i + 2];
            //Hogy ne connecteljuk ugyan azt a 2 pontot 2x
            if (p0 != p1) { dealunayGraph.ConnectPoints(p0, p1); }
            if (p1 != p2) { dealunayGraph.ConnectPoints(p1, p2); }
            if (p0 != p2) { dealunayGraph.ConnectPoints(p0, p2); }
        }
        List<long> visitedPs = new List<long> { RNG.RandiRange(0, ConnPs.Length - 1) };
        while (visitedPs.Count != Buildings.Count)
        {
            List<(long, long)> possibleConns = new List<(long, long)>();
            foreach (var visitedP in visitedPs.ToArray())
            {
                foreach (var connP in dealunayGraph.GetPointConnections(visitedP))
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
            hallwayGraph.ConnectPoints(shortestConn.Item1, shortestConn.Item2);
            dealunayGraph.DisconnectPoints(shortestConn.Item1, shortestConn.Item2);
        }
        foreach (var point in dealunayGraph.GetPointIds())
        {
            foreach (var connection in dealunayGraph.GetPointConnections(point))
            {
                if (point > connection) //Hogy ne connecteljuk ugyan azt a 2 pontot 2x
                {
                    //Add remaining connections with x chance
                    if (_extraConnectionChance >= RNG.RandiRange(0, 100))
                    {
                        hallwayGraph.ConnectPoints(point, connection);
                        Conns.Add((point, connection));
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
        AStarGrid2D grid = new AStarGrid2D()
        {
            Region = region,
            DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never,
            DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan,
            DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan
        };
        grid.Update();

        foreach (var building in Buildings)
        {
            // foreach (var room in building.Rooms)
            // {
            //     for (int x = 0; x < room.Size.X; x++)
            //     {
            //         for (int y = 0; y < room.Size.Y; y++)
            //         {
            //             grid.SetPointSolid(room.Position + new Vector2I(x, y));
            //         }
            //     }
            // }
            for (int x = 0; x < building.Rect.Size.X; x++)
            {
                for (int y = 0; y < building.Rect.Size.Y; x++)
                {
                    //grid.SetPointSolid(building.Rect.Position + new Vector2I(x, y));
                }
            }
            int absX = Math.Abs(building.Rect.End.X - building.Doors[0].Position.X);
            int absY = Math.Abs(building.Rect.End.Y - building.Doors[0].Position.Y);
            for (int x = 0; x < absX; x++)
            {
                for (int y = 0; y < absY; y++)
                {
                    //grid.SetPointSolid(building.Doors[0].Position + new Vector2I(x, y) * building.Doors[0].Direction, false);
                }
            }
        }
        GD.Print(Buildings[0].Rect.End + " " + Buildings[0].Doors[0].Position + " " + Buildings[0].Doors[0].Direction);
        Hallways = new List<Vector2[]>();
        foreach (var connection in Conns)
        {
            Hallways.Add(grid.GetPointPath((Vector2I)ConnPs[connection.Item1], (Vector2I)ConnPs[connection.Item2]));
        }
    }
}