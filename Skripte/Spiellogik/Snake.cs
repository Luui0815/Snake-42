using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

public class Snake : Node2D
{
    private Vector2 _direction = Vector2.Right;
    private Vector2 _nextDirection = Vector2.Right;
    private Vector2 _lastPosition;
    private Timer _moveTimer;
    private Fruit _fruit;
    private GameController _controller;
    
    public Vector2 GridSize = new Vector2(16, 16);
    public List<Vector2> SegmentPositions = new List<Vector2>();

    public override void _Ready()
    {
        SegmentPositions.Add(GlobalPosition);
        SegmentPositions.Add(GlobalPosition - _direction*GridSize*2);
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();
        _moveTimer = GetNode<Timer>("MoveTimer");
        _moveTimer.WaitTime = 0.5f;
        _moveTimer.Connect("timeout", this, nameof(OnTimerTimeout));
    }

    public override void _Process(float delta)
    {
        if(Input.IsActionPressed("ui_up") && _direction!= Vector2.Down) _nextDirection = Vector2.Up;
        if(Input.IsActionPressed("ui_right") && _direction != Vector2.Left) _nextDirection = Vector2.Right;
        if(Input.IsActionPressed("ui_left") && _direction != Vector2.Right) _nextDirection = Vector2.Left;
        if(Input.IsActionPressed("ui_down") && _direction != Vector2.Up) _nextDirection = Vector2.Down;
    }

    public override void _Draw()
    {
        foreach (var segment in SegmentPositions)
        {
            DrawRect(new Rect2(segment, new Vector2(32, 32)), Colors.Black);
        }
    }

    private void OnTimerTimeout()
    {
        if (IsGameOver())
        {
            GetTree().Paused = true;
        }
        else
        {
            MoveSnake();
        }

        if (IsFruitCollision())
        {
            GD.Print("Frucht gefressen!");
            _fruit.RandomizePosition();
            Grow();
            IncreaseSpeed();
        }
    }

    private bool IsGameOver()
    {
        foreach (var obstacle in _controller.Obstacles)
        {
            if (GlobalPosition == obstacle.RectGlobalPosition)
            {
                GD.Print("Game Over. Schlange hat ein Hindernis getroffen!");
                return true;
            }
        }
        if (SegmentPositions.Count >= 3)
        {
            for (int i = 1; i < SegmentPositions.Count; i++)
            {
                if (SegmentPositions[0] == SegmentPositions[i])
                {
                    GD.Print("Game Over. Schlange hat sich selbst gefressen.");
                    return true;
                }
            }
        }
        return false;
    }

    private void MoveSnake()
    {
        if ((_nextDirection + _direction).Length() != 0)
        {
            _direction = _nextDirection;
        }
        GlobalPosition += _direction * GridSize;
        SegmentPositions.Insert(0, GlobalPosition);
        _lastPosition = SegmentPositions[SegmentPositions.Count - 1];
        SegmentPositions.RemoveAt(SegmentPositions.Count - 1);
        Update();

        foreach(var segment in SegmentPositions)
        {
            GD.Print($"{segment.x}, {segment.y}");
        }
        GD.Print("--------------------------------");
    }

    private bool IsFruitCollision()
    {
        return SegmentPositions[0]*2 == _fruit.GlobalPosition;
    }

    private void Grow()
    {
        SegmentPositions.Add(_lastPosition);
    }

    private void IncreaseSpeed()
    {
        _moveTimer.WaitTime = Math.Max(0.15f, _moveTimer.WaitTime - 0.04f);
    }
}
