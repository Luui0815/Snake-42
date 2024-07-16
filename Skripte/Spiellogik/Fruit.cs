using Godot;
using System;
using System.Xml.Linq;

public class Fruit : Node2D
{
    private Snake _snake;

    public override void _Ready()
    {
        _snake = GetParent().GetNode<Snake>("Snake");
        RandomizePosition();
    }

    public void RandomizePosition()
    {
        Vector2 position = GetRandomPos();
        foreach (var segmentPosition in _snake.SegmentPositions)
        {
            if (segmentPosition == position)
            {
                position = GetRandomPos();
                continue;
            }
            if (segmentPosition == _snake.SegmentPositions[_snake.SegmentPositions.Count - 1])
            {
                this.Position = position;
                GD.Print("Frucht Position: " + position);
            }
        }
    }

    private Vector2 GetRandomPos()
    {
        Random random = new Random();
        float xPos = random.Next(4, 38) * 32;
        float yPos = random.Next(4, 21) * 32;

        return new Vector2(xPos, yPos);
    }
}
