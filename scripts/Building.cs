using System.Collections.Generic;
using Godot;

public class Building
{
    public struct Door
    {
        public Vector2I Position;
        public Vector2I Direction;
        public Door(Vector2I position, Vector2I direction)
        {
            Position = position;
            Direction = direction;
        }
    }
    public Rect2I[] Rooms { get; private set; }
    public Rect2I Rect { get; private set; }
    public Door[] Doors { get; private set; }
    private Vector2I _offset;

    public Building(Vector2I position, int maxSize)
    {
        GenerateRooms(position, maxSize);
    }
    // public TileTypes[] GetTiles()
    // {
    //     TileTypes[] tiles = new TileTypes[MainRoom.Size.X * MainRoom.Size.Y];
    //     for (int x = 0; x < MainRoom.Size.X; x++)
    //     {
    //         for (int y = 0; y < MainRoom.Size.Y; y++)
    //         {
    //             int idx = x * MainRoom.Size.Y + y;
    //             if (x == 0 || y == 0 || x == MainRoom.Size.X - 1 || y == MainRoom.Size.Y - 1)
    //             {
    //                 tiles[idx] = TileTypes.WALL;
    //             }
    //             else
    //             {
    //                 tiles[idx] = TileTypes.FLOOR;
    //             }
    //         }
    //     }
    //     return tiles;
    // }
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
        Rect2I mainRoom = new Rect2I(position, GetRandomSize(maxSize));
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
                Vector2I size = GetRandomSize(maxSize);
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
    private Vector2I GetRandomSize(int maxSize)
    {
        int x = DungGeneration.RNG.RandiRange(3, maxSize);
        int maxY = Mathf.Min(maxSize, Mathf.RoundToInt(x * 1.5));
        int minY = Mathf.Max(3, Mathf.RoundToInt(x * 0.5));
        int y = DungGeneration.RNG.RandiRange(minY, maxY);
        return new Vector2I(x, y);
    }


}