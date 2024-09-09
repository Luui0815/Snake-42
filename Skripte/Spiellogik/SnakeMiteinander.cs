using Godot;
using System;
using System.Linq;

public class SnakeMiteinander : Snake
{
    private Node2D _face1;
    private Node2D _face2;

    private Vector2 _directionCachePlayer1;
    private Vector2 _directionCachePlayer2;
    private Vector2 _currentDirection;
    private bool _isPlayerOneTurn;

    public override void _Ready()
    {
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();

        _body = GetNode<Line2D>("Body");
        _points = _body.Points;
        _face1 = GetNode<Node2D>("Face1");
        _face2 = GetNode<Node2D>("Face2");
        _tween = GetNode<Tween>("Tween");

        _directionCachePlayer1 = Vector2.Right;
        _directionCachePlayer2 = Vector2.Left;
        _currentDirection = _directionCachePlayer1;
        _isPlayerOneTurn = true;

        MoveSnake();
    }

    public override void _Input(InputEvent @event)
    {
        if (_isPlayerOneTurn)
        {
            if (Input.IsActionPressed("ui_up") && _currentDirection != Vector2.Down) _directionCachePlayer1 = Vector2.Up;
            if (Input.IsActionPressed("ui_right") && _currentDirection != Vector2.Left) _directionCachePlayer1 = Vector2.Right;
            if (Input.IsActionPressed("ui_left") && _currentDirection != Vector2.Right) _directionCachePlayer1 = Vector2.Left;
            if (Input.IsActionPressed("ui_down") && _currentDirection != Vector2.Up) _directionCachePlayer1 = Vector2.Down;
        }
        else
        {
            if (Input.IsActionPressed("move_up") && _currentDirection != Vector2.Down) _directionCachePlayer2 = Vector2.Up;
            if (Input.IsActionPressed("move_right") && _currentDirection != Vector2.Left) _directionCachePlayer2 = Vector2.Right;
            if (Input.IsActionPressed("move_left") && _currentDirection != Vector2.Right) _directionCachePlayer2 = Vector2.Left;
            if (Input.IsActionPressed("move_down") && _currentDirection != Vector2.Up) _directionCachePlayer2 = Vector2.Down;
        }
    }

    public override void MoveSnake()
    {
        _currentDirection = _isPlayerOneTurn ? _directionCachePlayer1 : _directionCachePlayer2;
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
    }

    protected override void MoveTween(float argv)
    {
        if (_Merker == false)
        {
            for (int i = 0; i < _body.Points.Count(); i++)
            {
                Vector2 newPos, diff = Vector2.Zero;

                Vector2 currentDirection = _isPlayerOneTurn ? _directionCachePlayer1 : _directionCachePlayer2;

                if (i == 0)
                {
                    newPos = _points[i] + currentDirection * _gridSize * argv;
                }
                else
                {
                    if (!(_growing && i == _body.Points.Count() - 1))
                    {
                        diff = (_points[i - 1] - _points[i]) / _gridSize;
                        newPos = _points[i] + diff * _gridSize * argv;
                    }
                    else
                    {
                        newPos = _body.GetPointPosition(i);
                    }
                }
                _body.SetPointPosition(i, newPos);
            }

            if (_eating == true)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count() - 1));
                _growing = true;
                _points = _body.Points;
                _eating = false;
            }

            _face1.Position = _body.Points[0];
            _face2.Position = _body.Points[_body.Points.Count() - 1];

            _face1.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer1.AngleTo(Vector2.Right));
            _face2.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer2.AngleTo(Vector2.Left));

            if (argv == 1)
            {
                _Merker = true;
                _points = _body.Points;

                CheckFruitCollision();

                if (_growing == true)
                    _growing = false;

                if (IsGameOver())
                {
                    _controller.OnGameFinished();
                }
                else
                {
                    SwapControl();
                }
            }
        }

        if (argv != 1)
        {
            _Merker = false;
        }
    }

    private void SwapControl()
    {
        _isPlayerOneTurn = !_isPlayerOneTurn;
        Array.Reverse(_points); 

        _currentDirection = _isPlayerOneTurn ? _directionCachePlayer1 : _directionCachePlayer2;

        Vector2 temp = _face1.Position;
        _face1.Position = _face2.Position;
        _face2.Position = temp;

        _face1.RotationDegrees = _currentDirection.AngleTo(Vector2.Right);
        _face2.RotationDegrees = (_points[_points.Length - 2] - _points[_points.Length - 1]).AngleTo(Vector2.Left);
    }
}
