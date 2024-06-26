using System.Collections.Generic;
using Godot;

public class Building
{
    public class Door
    {
        public Vector2I Position { get; set; }
        public Vector2I Direction { get; private set; }
        public Door(Vector2I position, Vector2I direction)
        {
            Position = position;
            Direction = direction;
        }
    }
    public string DebugName { get; private set; }
    public Rect2I[] Rooms { get; private set; }
    public Rect2I Rect { get; private set; }
    public Door[] Doors { get; private set; }
    private Vector2I _offset;

    public Building(Vector2I position, int maxSize, string name)
    {
        DebugName = name;
        GenerateRooms(position, maxSize);
    }
    public void WriteTo(TileTypes[,] tileMap)
    {
        foreach (var room in Rooms)
        {
            DungGeneration.GetTileMapXY(room.Position, out int X, out int Y);
            for (int x = 0; x <= room.Size.X; x++)
            {
                for (int y = 0; y <= room.Size.Y; y++)
                {
                    if (x == 0 || y == 0 || x == room.Size.X || y == room.Size.Y)
                    {
                        tileMap[X + x, Y + y] = TileTypes.WALL;
                    }
                    else
                    {
                        tileMap[X + x, Y + y] = TileTypes.FLOOR;
                    }
                }
            }
        }
        foreach (var door in Doors)
        {
            DungGeneration.GetTileMapXY(door.Position, out int X, out int Y);
            tileMap[X, Y] = TileTypes.FLOOR;
        }
    }
    public void Replace()
    {
        if (_offset == Rect.Position) { return; }
        _offset = Rect.Position - _offset;
        for (int i = 0; i < Rooms.Length; i++)
        {
            Rect2I room = Rooms[i];
            Rooms[i] = new Rect2I(room.Position + _offset, room.Size);
            Doors[i].Position += _offset;
        }
    }
    public void Move(Vector2I direction)
    {
        Rect = new Rect2I(Rect.Position + direction, Rect.Size);
    }
    private void GenerateRooms(Vector2I position, int maxSize)
    {
        //http://chongyangma.com/publications/gl/2014_gl_preprint.pdf i should implement this instead of this shit xd
        Rect2I mainRoom = new Rect2I(position, GetRandomSize(6, maxSize));
        int tryCount = 33;
        while (tryCount > 0)
        {
            Rooms = new Rect2I[1 + DungGeneration.RNG.RandiRange(0, 3)];
            Rooms[0] = mainRoom;
            List<Vector2I> directions = new List<Vector2I> { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
            Doors = new Door[Rooms.Length];

            for (int i = 1; i < Rooms.Length; i++)
            {
                var directionsCopy = new List<Vector2I>(directions);
                Vector2I direction = directionsCopy[DungGeneration.RNG.RandiRange(0, directionsCopy.Count - 1)];
                directionsCopy.Remove(direction);
                Vector2I size = GetRandomSize(4, maxSize);
                Vector2I connectPoint = GetConnectPoint(mainRoom, size, direction);
                if (direction == Vector2I.Up)
                {
                    Rooms[i] = new Rect2I(new Vector2I(connectPoint.X - size.X / 2, connectPoint.Y - size.Y), size);
                }
                else if (direction == Vector2I.Down)
                {
                    Rooms[i] = new Rect2I(new Vector2I(connectPoint.X - size.X / 2, connectPoint.Y), size);
                }
                else if (direction == Vector2I.Left)
                {
                    Rooms[i] = new Rect2I(new Vector2I(connectPoint.X - size.X, connectPoint.Y - size.Y / 2), size);
                }
                else if (direction == Vector2I.Right)
                {
                    Rooms[i] = new Rect2I(new Vector2I(connectPoint.X, connectPoint.Y - size.Y / 2), size);
                }
                directions = directionsCopy;
                Doors[i] = new Door(connectPoint, direction);
            }
            Vector2I mainDoorDirection = directions.Count == 1 ? directions[0] : directions[DungGeneration.RNG.RandiRange(0, directions.Count - 1)];
            Doors[0] = new Door(GetConnectPoint(mainRoom, mainRoom.Size, mainDoorDirection), mainDoorDirection);
            if (Rooms.Length == 1) { break; }

            bool overlap = false;
            for (int i = 0; i < Rooms.Length - 1; i++)
            {
                Rect2I iRoom = Rooms[i];
                foreach (var room in Rooms)
                {
                    if (iRoom == room) { continue; }
                    if (Rooms[i].Intersects(room)) { overlap = true; break; }
                }
                if (overlap) { break; }
            }
            if (overlap == false) { break; }
            tryCount--;
        }
        Rect = Rooms[0];
        if (Rooms.Length > 1)
        {
            for (int i = 1; i < Rooms.Length; i++)
            {
                Rect = Rect.Merge(Rooms[i]);
            }
        }
        //Adding random extra size for hallways dont go next to walls
        Vector2I extraSize = GetRandomSize(2, 4);
        Rect = new Rect2I(Rect.Position - extraSize / 2, Rect.Size + extraSize);
        _offset = Rect.Position;
    }
    private Vector2I GetConnectPoint(Rect2I mainRoom, Vector2I size, Vector2I direction)
    {
        Vector2I connectPoint = Vector2I.Zero;
        if (direction == Vector2I.Up)
        {
            connectPoint = new Vector2I(mainRoom.Position.X + DungGeneration.RNG.RandiRange(1, mainRoom.Size.X - 1), mainRoom.Position.Y);
        }
        else if (direction == Vector2I.Down)
        {
            connectPoint = new Vector2I(mainRoom.Position.X + DungGeneration.RNG.RandiRange(1, mainRoom.Size.X - 1), mainRoom.End.Y);
        }
        else if (direction == Vector2I.Left)
        {
            connectPoint = new Vector2I(mainRoom.Position.X, mainRoom.Position.Y + DungGeneration.RNG.RandiRange(1, mainRoom.Size.Y - 1));
        }
        else if (direction == Vector2I.Right)
        {
            connectPoint = new Vector2I(mainRoom.End.X, mainRoom.Position.Y + DungGeneration.RNG.RandiRange(1, mainRoom.Size.Y - 1));
        }
        return connectPoint;
    }
    private Vector2I GetRandomSize(int minSize, int maxSize)
    {
        int x = DungGeneration.RNG.RandiRange(minSize, maxSize);
        int maxY = Mathf.Min(maxSize, Mathf.RoundToInt(x * 1.5));
        int minY = Mathf.Max(minSize, Mathf.RoundToInt(x * 0.5));
        int y = DungGeneration.RNG.RandiRange(minY, maxY);
        return new Vector2I(x, y);
    }


}