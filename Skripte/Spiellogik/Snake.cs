using Godot;
using System;
using System.Collections.Generic;
using System.IO;
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
    private Snake _otherSnake;

    private int _gridSize = 32;
    private float _moveDelay = 0.45f;
    private bool _eating = false;
    private bool _growing = false;
    private bool _isPlayerOne;
    private bool _Merker = false;

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

    public void SetPlayerSettings(bool isPlayerOne)
    {
        _isPlayerOne = isPlayerOne;
        if (!isPlayerOne)
        {
            _otherSnake = GetParent().GetNode<Snake>("Snake1");
            _body.DefaultColor = new Color(255, 255, 0, 1);
            for(int i = 0; i < _points.Length; i++)
            {
                _points[i] += new Vector2(0, 2*_gridSize);
                _body.SetPointPosition(i, _points[i]);
            }
        }
        else
        {
            _otherSnake = GetParent().GetNodeOrNull<Snake>("Snake2");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsPressed())
        {
            if (_isPlayerOne)
            {
                if (Input.IsActionPressed("ui_up") && _direction != Vector2.Down) _directionCache = Vector2.Up;
                if (Input.IsActionPressed("ui_right") && _direction != Vector2.Left) _directionCache = Vector2.Right;
                if (Input.IsActionPressed("ui_left") && _direction != Vector2.Right) _directionCache = Vector2.Left;
                if (Input.IsActionPressed("ui_down") && _direction != Vector2.Up) _directionCache = Vector2.Down;
            }
            else
            {
                if (Input.IsActionPressed("move_right") && _direction != Vector2.Left) _directionCache = Vector2.Right;
                if (Input.IsActionPressed("move_left") && _direction != Vector2.Right) _directionCache = Vector2.Left;
                if (Input.IsActionPressed("move_up") && _direction != Vector2.Down) _directionCache = Vector2.Up;
                if (Input.IsActionPressed("move_down") && _direction != Vector2.Up) _directionCache = Vector2.Down;
            }
        }
    }

    private void MoveSnake()
    {
        _direction = _directionCache;
        //_body.AddPoint(_points[0], 0);
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, _moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
    }

    private void MoveTween(float argv)
    {
        if(_Merker == false)
        {
            int i = 0;
            foreach(Vector2 pos in _body.Points)
            {
                Vector2 newPos,diff=Vector2.Zero;
                if(i == 0)
                    newPos =  _points[i] + _direction * new Vector2(_gridSize * argv, _gridSize * argv);
                /*
                else if(i == _body.Points.Count() - 1)
                {
                    diff = Vector2.Zero;
                    if(_points[i-1].x - _points[i].x != 0)
                        diff.x = (_points[i-1].x - _points[i].x) / _gridSize;
                    if(_points[i-1].y - _points[i].y != 0)
                        diff.y = (_points[i-1].y - _points[i].y) / _gridSize;
                    
                    newPos =  _points[i] + diff * new Vector2(_gridSize * argv, _gridSize * argv);
                }
                */
                else
                {
                    if(!(_growing == true && i == _body.Points.Count() -1))
                    {
                        diff = Vector2.Zero;
                        if(_points[i-1].x - _points[i].x != 0)
                            diff.x = (_points[i-1].x - _points[i].x) / _gridSize;
                        if(_points[i-1].y - _points[i].y != 0)
                            diff.y = (_points[i-1].y - _points[i].y) / _gridSize;
                    
                        newPos =  _points[i] + diff * new Vector2(_gridSize * argv, _gridSize * argv);
                    }
                    else
                    {
                        // letztes Körperteil darf nicht bewegt werden!
                        newPos = _body.GetPointPosition(i);
                    }
                }
                _body.SetPointPosition(i, newPos);
                i++;
            }

            if(_eating == true)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count()-1));
                _growing = true;
                _points = _body.Points;
                _eating = false;
            }

            _face.Position = _body.Points[0];
            _face.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));

            // wenn argv = 1 dann ist eine Schleife durch
            if(argv == 1)
            {
                _Merker = true;
                _points = _body.Points;
                CheckFruitCollision();

                if(_growing == true)
                    _growing = false;

                if (IsGameOver())
                {
                   _controller.OnGameFinished();
                }
                else
                {
                    _direction = _directionCache;
                } 
            }
        }

        if(argv != 1)
            _Merker = false;
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
            GD.Print($"{Name} hat Frucht gefressen!");
        }
        else
        {
            //_eating = false;
        }
    }

    private void IncreaseSpeed()
    {
        //_moveDelay = Math.Max(0.06f, _moveDelay - 0.04f);
        _moveDelay *= 0.9f;
        GD.Print(_moveDelay.ToString());
    }

    private bool IsGameOver()
    {
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[0] == obstacle.RectGlobalPosition)
            {
                GD.Print($"Game Over fuer {Name}. hat ein Hindernis getroffen!");
                return true;
            }
        }

        if (_otherSnake != null)
        {
            if (_otherSnake.Points.Contains(_body.Points[0]))
            {
                if (_body.Points[0] == _otherSnake.Points[0])
                {
                    GD.Print($"Unentschieden. {Name} und {_otherSnake.Name} sind kollidiert.");
                    return true;
                }
                else
                {
                    GD.Print($"Game Over fuer {Name}. Ist mit {_otherSnake.Name} kollidiert!");
                    return true;
                }

            }
        }

        if (_points.Length >= 3)
        {
            for (int i = 1; i < _points.Length; i++)
            {
                if (_points[0] == _points[i])
                {
                    GD.Print($"Game Over fuer {Name}. Hat sich selbst gefressen!");
                    return true;
                }
            }
        }
        return false;
    }
}
