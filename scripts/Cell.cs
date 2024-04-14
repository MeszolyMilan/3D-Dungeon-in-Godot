using Godot;
using System;

public partial class Cell : Node3D
{
	[Export] private Node3D _left;
	[Export] private Node3D _right;
	[Export] private Node3D _up;
	[Export] private Node3D _down;

	public struct Neighbours
	{
		public bool Left;
		public bool Right;
		public bool Up;
		public bool Down;
	}
	public void RemoveFaces(Neighbours neighbours)
	{
		if (neighbours.Left) { _left.QueueFree(); }
		if (neighbours.Right) { _right.QueueFree(); }
		if (neighbours.Up) { _up.QueueFree(); }
		if (neighbours.Down) { _down.QueueFree(); }
	}
}
