using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Snake : Node2D
{
    private Vector2 _direction = Vector2.Right;
    private Vector2 _directionCache;
    private Vector2[] _points;
    private Line2D _body;
    private Node2D _face;
    private Tween _tween;
    private Fruit _fruit;
    private GameController _controller;

    private int _gridSize = 32;
    private float _moveDelay = 0.45f;
    private bool _eating = false;

    public Vector2[] Points { get { return _points; } }

    public override void _Ready()
    {
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();

        _body = GetNode<Line2D>("Body");
        _points = _body.Points;
        _face = GetNode<Node2D>("Face");
        _tween = GetNode<Tween>("Tween");

        _directionCache = _direction;
        MoveSnake();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsPressed())
        {
            if (Input.IsActionPressed("ui_up") && _direction != Vector2.Down) _directionCache = Vector2.Up;
            if (Input.IsActionPressed("ui_right") && _direction != Vector2.Left) _directionCache = Vector2.Right;
            if (Input.IsActionPressed("ui_left") && _direction != Vector2.Right) _directionCache = Vector2.Left;
            if (Input.IsActionPressed("ui_down") && _direction != Vector2.Up) _directionCache = Vector2.Down;
        }
    }

    private void MoveSnake()
    {
        _direction = _directionCache;
        _body.AddPoint(_points[0] + _direction * _gridSize, 0);
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, _moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
    }

    private void MoveTween(float argv)
    {
        Vector2 newPos = _points[0] + _direction * new Vector2(_gridSize * argv, _gridSize * argv);
        _body.SetPointPosition(0, newPos);

        if (!_eating)
            _body.SetPointPosition(_body.Points.Length - 1, _points[_points.Length - 1] + (_points[_points.Length - 2] - _points[_points.Length - 1]) * argv);
        
        _face.Position = _body.Points[0];
        _face.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));
    }

    private void _on_Tween_tween_all_completed()
    {
        if(!_eating)
            _body.RemovePoint(_points.Length);

        _points = _body.Points;
        CheckFruitCollision();

        if (IsGameOver())
        {
            _controller.OnGameFinished();
        }
        else
        {
            MoveSnake();
        }
    }

    private void CheckFruitCollision()
    {
        if (_body.Points[0] == _fruit.Position)
        {
            _eating = true;
            _fruit.RandomizePosition();
            IncreaseSpeed();
            _controller.UpdateScore();
            GD.Print("Frucht gefressen!");
        }
        else
        {
            _eating = false;
        }
    }

    private void IncreaseSpeed()
    {
        _moveDelay = Math.Max(0.06f, _moveDelay - 0.04f);
    }

    private bool IsGameOver()
    {
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[0] == obstacle.RectGlobalPosition)
            {
                GD.Print("Game Over. Schlange hat ein Hindernis getroffen!");
                return true;
            }
        }
        if (_points.Length >= 3)
        {
            for (int i = 1; i < _points.Length; i++)
            {
                if (_points[0] == _points[i])
                {
                    GD.Print("Game Over. Schlange hat sich selbst gefressen.");
                    return true;
                }
            }
        }
        return false;
    }
}
