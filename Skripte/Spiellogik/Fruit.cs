using Godot;
using System;
using System.Collections.Generic;
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
        Vector2 position;
        Random random = new Random();

        do
        {
            position = GetRandomPos(random);
        }
        while (IsPositionOccupied(position));

        GlobalPosition = position;
        GD.Print("Frucht Position: " + position);
    }

    private Vector2 GetRandomPos(Random random)
    {
        float xPos = random.Next(0, 40) * 32;
        float yPos = random.Next(0, 23) * 32;
        return new Vector2(xPos, yPos);
    }

    public bool IsPositionOccupied(Vector2 position)
    {
        foreach (var obstacle in _controller.Obstacles)
        {
            if (obstacle.RectGlobalPosition == position)
            {
                return true;
            }
        }

        foreach (var segment in _snake.SegmentPositions)
        {
            if (segment == position)
            {
                return true;
            }
        }

        return false;
    }
}
