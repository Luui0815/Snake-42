using Godot;
using System;
using System.Collections.Generic;

public class Snake : Node2D
{
    private Vector2 _direction = Vector2.Right;
    private float _speed = 100f;
    private List<Vector2> _segments = new List<Vector2>();

    public override void _Ready()
    {
        _segments.Add(Position);
    }

    public override void _Process(float delta)
    {
        if(Input.IsActionPressed("ui_up")) _direction = Vector2.Up;
        if(Input.IsActionPressed("ui_right")) _direction = Vector2.Right;
        if(Input.IsActionPressed("ui_left")) _direction = Vector2.Left;
        if(Input.IsActionPressed("ui_down")) _direction = Vector2.Down;

        Position += _direction * _speed * delta;
        _segments.Insert(0, Position);
        _segments.RemoveAt(_segments.Count - 1);
        Update();
    }

    public override void _Draw()
    {
        foreach (var segment in _segments) 
        {
            DrawRect(new Rect2(segment, new Vector2(10,10)), Colors.Green);
        }
    }
}
