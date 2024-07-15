using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

public class Snake : Node2D
{
    private Vector2 _direction = Vector2.Right;
    private Vector2 _windowBorder;
    private Vector2 _lastPosition;
    private Timer _moveTimer;
    private Fruit _fruit;    
    
    public Vector2 GridSize = new Vector2(16, 16);
    public List<Vector2> SegmentPositions = new List<Vector2>();

    public override void _Ready()
    {
        _windowBorder = OS.WindowSize;
        SegmentPositions.Add(Position);
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _moveTimer = GetNode<Timer>("MoveTimer");
        _moveTimer.WaitTime = 0.5f;
        _moveTimer.Connect("timeout", this, nameof(OnTimerTimeout));
    }

    public override void _Process(float delta)
    {
        if(Input.IsActionPressed("ui_up") && _direction!= Vector2.Down) _direction = Vector2.Up;
        if(Input.IsActionPressed("ui_right") && _direction != Vector2.Left) _direction = Vector2.Right;
        if(Input.IsActionPressed("ui_left") && _direction != Vector2.Right) _direction = Vector2.Left;
        if(Input.IsActionPressed("ui_down") && _direction != Vector2.Up) _direction = Vector2.Down;
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
        GD.Print($"{SegmentPositions[SegmentPositions.Count-1].x}, {SegmentPositions[SegmentPositions.Count - 1].y}");
        if (SegmentPositions[0].x < 32 || SegmentPositions[0].x > 592)
        {
            GD.Print("Game Over. Schlange hat linkes/rechtes Ende erreicht");
            return true;
        }
        else if (SegmentPositions[0].y < 32 || SegmentPositions[0].y > 320)
        {
            GD.Print("Game Over. Schlange hat oberes/unteres Ende erreicht");
            return true;
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
        Position += _direction * GridSize;
        SegmentPositions.Insert(0, Position);
        _lastPosition = SegmentPositions[SegmentPositions.Count - 1];
        SegmentPositions.RemoveAt(SegmentPositions.Count - 1);
        Update();
    }

    private bool IsFruitCollision()
    {
        return SegmentPositions[0]*2 == _fruit.Position;
    }

    private void Grow()
    {
        SegmentPositions.Add(_lastPosition);
    }

    private void IncreaseSpeed()
    {
        _moveTimer.WaitTime = Math.Max(0.1f, _moveTimer.WaitTime - 0.05f);
    }
}
