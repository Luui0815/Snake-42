using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

public class Fruit : Node2D
{
    private Snake _snake;
    private GameController _controller;
    private int _cellSize = 32;

    public override void _Ready()
    {
        _snake = GetParent().GetNode<Snake>("Snake");
        _controller = GetParent<GameController>();
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

        Position = position + new Vector2(16,16);
        GD.Print("Frucht Position: " + position);
    }

    private Vector2 GetRandomPos(Random random)
    {
        int xMax = _controller.GameField.GetLength(1) - 1;
        int yMax = _controller.GameField.GetLength(0) - 1;

        float xPos = random.Next(7, xMax) * _cellSize;
        float yPos = random.Next(3, yMax) * _cellSize;

        return new Vector2(xPos, yPos);
    }

    public bool IsPositionOccupied(Vector2 position)
    {
        int x = (int)(position.x / _cellSize);
        int y = (int)(position.y / _cellSize);

        if (x < 0 || x >= _controller.GameField.GetLength(1) || y < 0 || y >= _controller.GameField.GetLength(0))
        {
            return true;
        }

        if (_controller.GameField[y, x] == 1)
        {
            return true;
        }

        foreach (var segment in _snake.Points)
        {
            if (segment == position)
            {
                return true;
            }
        }

        return false;
    }
}
