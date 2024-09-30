using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public class Fruit : Node2D
{
    private BaseSnake _snake1;
    private BaseSnake _snake2;
    private BaseSnake _snake3;
    private GameController _controller;
    private AnimationPlayer _player;
    private int _cellSize = 32;
    public void Init()
    {
        GD.Print("Frucht initialisierung");
        _snake1 = GetParent().GetNode<BaseSnake>("Snake1");
        _snake2 = GetParent().GetNodeOrNull<BaseSnake>("Snake2");
        _snake3 = GetParent().GetNodeOrNull<BaseSnake>("Snake3");

        _controller = GetParent<GameController>();
        _player = GetChild(0).GetNode<AnimationPlayer>("AnimationPlayer");
        _player.Play("default");
        _player.Connect("animation_finished", this, nameof(OnAnimationFinished));
    }

    private void OnAnimationFinished(string animationName)
    {
        _player.Play(animationName);
    }

    public void SetNewPosition(Vector2 newPos)
    {
        Position = newPos;
    }
    public void SetNewPosition(float x, float y)
    {
        Position = new Vector2(x, y);
    }

    public Vector2 RandomizePosition()
    {
        Vector2 position;
        Random random = new Random();

        do
        {
            position = GetRandomPos(random);
        }
        while (IsPositionOccupied(position));

        GD.Print("Frucht Position: " + position);
        return position + new Vector2(16,16);
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

        if (_snake1.Points.Contains(position))
        {
            return true;
        }

        if (_snake2 != null && _snake2.Points.Contains(position))
        {
            return true;
        }        

        if (_snake3 != null && _snake3.Points.Contains(position))
        {
            return true;
        }

        return false;
    }
}
