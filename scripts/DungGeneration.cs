using System;
using System.Collections.Generic;
using Godot;

public class DungGeneration
{
    public static RandomNumberGenerator RNG { get; private set; }
    public List<Building> Buildings { get; private set; }
    public List<Vector2I[]> Hallways { get; private set; }
    //Only for show
    public Vector2[] MainDoors { get; private set; }
    public int[] BuildingTris { get; private set; }
    //
    private int _spawnRadius;
    private int _buildingCount;
    private int _buildingMaxSize;

    public DungGeneration(ulong seed, int spawnRadius, int buildingCount, int buildingMaxSize)
    {
        RNG = new RandomNumberGenerator();
        if (seed != 0) { RNG.Seed = seed; }
        else { GD.Print("Seed: " + RNG.Seed); }
        _spawnRadius = spawnRadius;
        _buildingCount = buildingCount;
        _buildingMaxSize = buildingMaxSize;
        GenerateBuildings();
        GenerateHallways();
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
    private void GenerateHallways()
    {
        MainDoors = new Vector2[Buildings.Count];
        AStar2D trisGraph = new AStar2D();
        AStar2D mstGraph = new AStar2D(); //Minimum Spanning Tree
        for (int i = 0; i < Buildings.Count; i++)
        {
            Vector2I pos = Buildings[i].Doors[0].Position;
            MainDoors[i] = pos;
            trisGraph.AddPoint(trisGraph.GetAvailablePointId(), pos);
            mstGraph.AddPoint(mstGraph.GetAvailablePointId(), pos);
        }
        BuildingTris = Geometry2D.TriangulateDelaunay(MainDoors);
        for (int i = 0; i < BuildingTris.Length - 2; i++)
        {
            int p0 = BuildingTris[i];
            int p1 = BuildingTris[i + 1];
            int p2 = BuildingTris[i + 2];
            trisGraph.ConnectPoints(p0, p1);
            trisGraph.ConnectPoints(p1, p2);
            trisGraph.ConnectPoints(p0, p2);
        }
        //int[] visitedPoints = new int[];
    }
    private Vector2I GetRandomPoint()
    {
        float r = _spawnRadius * Mathf.Sqrt(RNG.Randf());
        float t = RNG.Randf() * 2 * Mathf.Pi;
        int x = (int)(r * Mathf.Cos(t));
        int y = (int)(r * Mathf.Sin(t));
        return new Vector2I(x, y);
    }
}