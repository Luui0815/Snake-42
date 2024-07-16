using Godot;
using System;
using System.Xml.Linq;

public class Fruit : Node2D
{
    private Snake _snake;
    private GameController _controller;

    public override void _Ready()
    {
        _snake = GetParent().GetNode<Snake>("Snake");
        _controller = GetParent<GameController>();
        RandomizePosition();
    }

    public void RandomizePosition()
    {
        Vector2 position = GetRandomPos();
        foreach(var obstacle in _controller.obstacles)
        {
            if(position == obstacle.RectGlobalPosition)
            {
                position = GetRandomPos();
                continue;
            }
        }
        foreach (var segmentPosition in _snake.SegmentPositions)
        {
            if (segmentPosition == position)
            {
                position = GetRandomPos();
                continue;
            }
            if (segmentPosition == _snake.SegmentPositions[_snake.SegmentPositions.Count - 1])
            {
                this.GlobalPosition = position;
                GD.Print("Frucht Position: " + position);
            }
        }
    }

    private Vector2 GetRandomPos()
    {
        Random random = new Random();
        float xPos = random.Next(8, 33) * 32;
        float yPos = random.Next(6, 20) * 32;

        return new Vector2(xPos, yPos);
    }
}
