using System.Collections.Generic;
using Godot;

public class DungeonGen
{
    public TileTypes[] Tiles { get; private set; }
    public List<Rect> Rects { get; private set; }
    public struct Rect
    {
        public Vector2I StartPos;
        public Vector2I Size;
        public Rect(Vector2I startPos, Vector2I size)
        {
            StartPos = startPos;
            Size = size;
        }
    }
    private int _borderSize;
    private int _buildingSize;
    private RandomNumberGenerator _rng;
    public DungeonGen(int borderSize, int buildingSize)
    {
        _borderSize = borderSize;
        _buildingSize = buildingSize;
        _rng = new RandomNumberGenerator();
        Tiles = new TileTypes[_borderSize * _borderSize];
        Generate();
    }
    private void Generate()
    {
        GenerateBuildings();
    }
    private void GenerateBuildings()
    {
        Rects = SplitRect(new Rect(Vector2I.Zero, new Vector2I(_borderSize, _borderSize)), _buildingSize);

        bool[] cellFlags = new bool[_borderSize * _borderSize];

    }
    private List<Rect> SplitRect(Rect startRect, int minSize)
    {
        //Split whole map into small rooms
        List<Rect> rects = new List<Rect>();
        Queue<Rect> rectsToSplit = new Queue<Rect>();
        rectsToSplit.Enqueue(startRect);
        while (rectsToSplit.Count > 0)
        {
            Rect rect = rectsToSplit.Dequeue();
            bool canSplitH = rect.Size.X >= minSize * 2;
            bool canSplitV = rect.Size.Y >= minSize * 2;
            if (canSplitH == false && canSplitV == false) { rects.Add(rect); continue; }
            if (canSplitH && canSplitV)
            {
                int flip = _rng.RandiRange(0, 1);
                canSplitH = flip == 0;
                canSplitV = flip == 1;
            }
            if (canSplitH)
            {
                int splitAt = _rng.RandiRange(minSize, rect.Size.X - minSize);
                Rect splited1 = new Rect(rect.StartPos, new Vector2I(splitAt, rect.Size.Y));
                Rect splited2 = new Rect(new Vector2I(rect.StartPos.X + splitAt, rect.StartPos.Y), new Vector2I(rect.Size.X - splitAt, rect.Size.Y));
                rectsToSplit.Enqueue(splited1);
                rectsToSplit.Enqueue(splited2);
            }
            if (canSplitV)
            {
                int splitAt = _rng.RandiRange(minSize, rect.Size.Y - minSize);
                Rect splited1 = new Rect(rect.StartPos, new Vector2I(rect.Size.X, splitAt));
                Rect splited2 = new Rect(new Vector2I(rect.StartPos.X, rect.StartPos.Y + splitAt), new Vector2I(rect.Size.X, rect.Size.Y - splitAt));
                rectsToSplit.Enqueue(splited1);
                rectsToSplit.Enqueue(splited2);
            }
        }
        return rects;
    }
}