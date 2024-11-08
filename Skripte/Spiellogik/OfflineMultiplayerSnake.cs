using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class OfflineMultiplayerSnake : BaseSnake
{
    private Node2D _face1;
    private Node2D _face2;

    private Vector2 _directionCachePlayer1;
    private Vector2 _directionCachePlayer2;
    private Vector2 _currentDirection;
    private bool _isPlayerOneTurn;
    private int _NewTailIndex;

    public override void _Ready()
    {
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();
        _audioPlayer = GetNode<AudioStreamPlayer2D>("Eating");
        _body = GetNode<Line2D>("Body");
        _points = _body.Points;
        _face1 = GetNode<Node2D>("Face1");
        _face2 = GetNode<Node2D>("Face2");
        _tween = GetNode<Tween>("Tween");

        _directionCachePlayer1 = Vector2.Right;
        _directionCachePlayer2 = Vector2.Left;
        _currentDirection = _directionCachePlayer1;
        _isPlayerOneTurn = true;
    }

    public override void SetPlayerSettings(bool isServer, bool isSnake1, BaseSnake otherSnake)
    {
        // Server hat beide Schlangen, steuert aktiv aber nur die 1.
        // Bei jeder Bewegungsaenderung sendet er es an den 2.Spieler
        // Der 2. Spieler sendet nur Richtungsaenderungen an den Server(Spieler 1!)
        _isServer = isServer;
        _isPlayerOneTurn = true;
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

        GD.Print($"Cache 1{_directionCachePlayer1} Cache 2{_directionCachePlayer2}\nRichtung: {_currentDirection}");
    }

    public override void MoveSnake()
    {
        _currentDirection = _isPlayerOneTurn ? _directionCachePlayer1 : _directionCachePlayer2;
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, MoveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
    }

    protected override void MoveTween(float argv)
    {
        if (!_merker)
        {
            Vector2 newPos = Vector2.Zero;

            if(_isPlayerOneTurn)
            {
                for (int i = 0; i < _body.GetPointCount(); i++)
                {
                    if (i == 0)
                    {
                        newPos = _points[i] + _currentDirection * _gridSize * argv;
                    }
                    else if(i == _NewTailIndex && _growing)
                    {
                        // wenn das Vieh wächst darf das mittlere Element nicht bewegt werden!
                        newPos = _points[i];
                    }
                    else 
                    {
                        Vector2 diff = (_points[i - 1] - _points[i]) / _gridSize;
                        newPos = _points[i] + diff * _gridSize * argv;
                    }

                    _body.SetPointPosition(i, newPos);
                }
            }
            else
            {
                for (int i = _body.GetPointCount() - 1; i >= 0; i--)
                {
                    if (i == _body.GetPointCount() - 1)
                    {
                        newPos = _points[i] + _currentDirection * _gridSize * argv;
                    }
                    else if(i == _NewTailIndex && _growing)
                    {
                        // wenn das Vieh wächst darf das mittlere Element nicht bewegt werden!
                        newPos = _points[i];
                    }
                    else 
                    {
                        Vector2 diff = (_points[i + 1] - _points[i]) / _gridSize;
                        newPos = _points[i] + diff * _gridSize * argv;
                    }

                    _body.SetPointPosition(i, newPos);
                }
            }

            if (argv == 1)
            {
                _tween.StopAll();
                _merker = true;
                _points = _body.Points;

                CheckFruitCollision();

                if (_growing)
                    _growing = false;

                if (IsGameOver())
                {
                    _controller.OnGameFinished();
                }
                else if (_eating)
                {
                    SwapControl();
                }
                MoveSnake();
            }
        }

        if (argv != 1)
        {
            _merker = false;
        }

        RotateAndMoveFace();
    }


    protected override void CheckFruitCollision()
    {
        if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == _fruit.Position)
        {
            _audioPlayer.Play();
            _controller.UpdateScore();
            GD.Print($"{Name} hat Frucht gefressen!");
            
            SwapControl();

            AddNewSnakePoint();

            _points = _body.Points; // Punkte auf das neue Array aktualisieren
            _growing = true; // Setze Wachstum auf aktiv

            _fruit.SetNewPosition(_fruit.RandomizePosition());
        }
    }

    private void RotateAndMoveFace()
    {
        _face1.Position = _body.Points[0];
        _face2.Position = _body.Points[_body.Points.Count() - 1];

        Vector2 lookDirection;
        if (_isPlayerOneTurn)
        {
            // genau in die entgegengesetzte Richtung des an ihm befindlichen Körperteil setzten
            lookDirection = ((_points[_points.Count() - 1] - _points[_points.Count() - 2]) / _gridSize) * -1;
            _face1.RotationDegrees = -Mathf.Rad2Deg(_currentDirection.AngleTo(Vector2.Right));
            _face2.RotationDegrees = -Mathf.Rad2Deg(lookDirection.AngleTo(Vector2.Left));
        }
        else
        {
            // -1 mehr da durch CheckFruit Collision der letzte Punkte dupliziert wurde. d.h. 2 mal gleiche Punktkoordinaten!
            lookDirection = ((_points[1] - _points[0]) / _gridSize) * -1;
            _face2.RotationDegrees = -Mathf.Rad2Deg(_currentDirection.AngleTo(Vector2.Left));
            _face1.RotationDegrees = -Mathf.Rad2Deg(lookDirection.AngleTo(Vector2.Right));
        }
    }

    protected override bool IsGameOver()
    {
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == obstacle)
            {
                _controller.LoseMessage = ($"Game Over fuer {Name}.\nHat ein Hindernis getroffen!");
                return true;
            }
        }

        if (_otherSnake != null && IsInstanceValid(_otherSnake))
        {
            if (_otherSnake.Points.Contains(_body.Points[0]))
            {
                if (_body.Points[0] == _otherSnake.Points[0])
                {
                    _controller.LoseMessage = ($"Unentschieden.\n{Name} und {_otherSnake.Name} sind kollidiert.");
                    return true;
                }
                else
                {
                    _controller.LoseMessage = ($"Game Over fuer {Name}.\nIst mit {_otherSnake.Name} kollidiert!");
                    return true;
                }

            }
        }

        if (_points.Length >= 3)
        {
            int startIndex = _isPlayerOneTurn ? 1 : _points.Length - 2;
            int endIndex = _isPlayerOneTurn ? _points.Length : 0;
            int step = _isPlayerOneTurn ? 1 : -1;

            for (int i = startIndex; (step == 1 ? i < endIndex : i >= endIndex); i += step)
            {
                if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == _points[i])
                {
                    GD.Print($"Game Over fuer {Name}. Hat sich selbst gefressen!");
                    return true;
                }
            }
        }
        return false;
    }

    private void SwapControl()
    {
        _isPlayerOneTurn = !_isPlayerOneTurn;

        // jetzt _current direction so setzten das die Schlange nicht gleich in den eigenen Körper läuft
        Vector2 direction;
        if(_isPlayerOneTurn)
        {
            direction = _body.Points[0] - _body.Points[1];
        }
        else
        {
            direction = _body.Points[_body.Points.Length - 1] - _body.Points[_body.Points.Length - 2];
        }
        _currentDirection = direction.Normalized();
        if(_isPlayerOneTurn)
            _directionCachePlayer1 = _currentDirection;
        else
            _directionCachePlayer2 = _currentDirection; 
    }

    private void AddNewSnakePoint()
    {
        List<Vector2> newPoints = _body.Points.ToList();;
        if(!_isPlayerOneTurn)
        {
            newPoints.Insert(0, newPoints[0]);
            _NewTailIndex = 0;
        }
        else
        {
            _NewTailIndex = _body.Points.Length;
            newPoints.Insert(_body.Points.Length - 1, newPoints[_body.Points.Length - 1]);
        }
        _body.Points = newPoints.ToArray();
    }
}
