using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public abstract class BaseSnake : Node2D
{
    protected Vector2 _direction = Vector2.Right;
    protected Vector2 _directionCache;
    protected AudioStreamPlayer2D _audioPlayer;
    protected List<Vector2> _points = new List<Vector2>();
    protected Line2D _body;
    protected Node2D _face;
    protected Tween _tween;
    protected Fruit _fruit;
    protected GameController _controller;
    protected BaseSnake _otherSnake;

    protected int _gridSize = 32;
    public float moveDelay;
    protected bool _eating = false;
    protected bool _growing = false;
    protected bool _isPlayerOne;
    protected bool _Merker = false;

    protected bool _isServer;
    protected bool _isSnake1;
    protected bool _RoatationFinished = true;
    protected Vector2 _AltDirection;
    public List<Vector2> Points { get { return _points; } }

    public override void _Ready()
    {
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();
        _audioPlayer = GetNode<AudioStreamPlayer2D>("Eating");
        _body = GetNode<Line2D>("Body");

        _points = new List<Vector2>();
        for(int i = 0; i < _body.GetPointCount(); i++)
        {
            _points.Add(new Vector2(_body.GetPointPosition(i)));
        }

        _face = GetNode<Node2D>("Face");
        _tween = GetNode<Tween>("Tween");

        _directionCache = _direction;
    }

    public override void _Process(float delta)
    {
        // nochmal das ganze interpoliren damit es besser aussieht
        // hier eilt die Schlange den Punkten _points hinterher
        // für Singelplayer kann man meinen ist es übertrieben, aber die bewegung ist jetzt nochmal angenehmer
        // und im Online Spiel lassen sich so Aktualisierungsruckler verbergen (Hoff ich)
        for (int i = 0; i < _body.Points.Length; i++)
        {
            _body.SetPointPosition(i, _body.Points[i].LinearInterpolate(_points[i], 0.2f));
        }

        RotateHead();
    }

    protected void RotateHead()
    {
        _face.Position = _body.Points[0];
        float targetRotation = 0;
        
        // bestimmen nach wohin rotiert werden muss
        targetRotation = _face.RotationDegrees - Mathf.Rad2Deg(_direction.AngleTo(_AltDirection));

        if(targetRotation != _face.RotationDegrees)
            _RoatationFinished = false;

        float weight = 10;
        bool AltFaceRotationLowerThanTargetRotation = _face.RotationDegrees < targetRotation ? true : false;

        // checken ob bei der Differenz von Ziel und Akt Roation nach das Gewicht dazwischen passt, wenn nicht setzte kopf auf zielpos, rotation ist fertig
        if(Math.Abs(targetRotation - _face.RotationDegrees) < weight)
        {
            // Rotation ist fertig
            _RoatationFinished = true;
            _face.RotationDegrees = targetRotation;
        }

        if(!_RoatationFinished)
        {
            if(_face.RotationDegrees < targetRotation)
            {
                _face.RotationDegrees += weight;
            }
            else
            {
                _face.RotationDegrees -= weight;
            }
        }
    }

    public abstract void SetPlayerSettings(bool isServer, bool isSnake1, BaseSnake otherSnake);

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

    public virtual void MoveSnake()
    {
        _direction = _directionCache;
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
    }
    protected List<Vector2> AltPosition = new List<Vector2>();
    protected virtual void MoveTween(float argv)
    {
        // initialisierung der alten Punkte
        if(argv == 0)
        {
            AltPosition.Clear();
            // aktuelle Punkte auf alte Punkte kopieren! keine Referenzen!
            foreach(Vector2 vec in _points)
            {
                AltPosition.Add(new Vector2(vec));
            }
        }

        if (_Merker == false)
        {
            for(int i = 0; i < _points.Count; i++)
            {
                Vector2 newPos, diff = Vector2.Zero;
                if (i == 0)
                    newPos = new Vector2(AltPosition[i] + _direction * new Vector2(_gridSize * argv, _gridSize * argv));
                else
                {
                    if (!(_growing == true && i == _points.Count - 1))
                    {
                        diff = Vector2.Zero;
                        if (AltPosition[i - 1].x - AltPosition[i].x != 0)
                            diff.x = (AltPosition[i - 1].x - AltPosition[i].x) / _gridSize;
                        if (AltPosition[i - 1].y - AltPosition[i].y != 0)
                            diff.y = (AltPosition[i - 1].y - AltPosition[i].y) / _gridSize;

                        newPos = new Vector2(AltPosition[i] + diff * new Vector2(_gridSize * argv, _gridSize * argv));
                    }
                    else
                    {
                        newPos = new Vector2(AltPosition[i]);
                    }
                }
                _points[i] = new Vector2(newPos);
            }

            if (_eating == true)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count() - 1));
                _points.Add(new Vector2(_points[_points.Count() - 1]));
                AltPosition.Add(new Vector2(_points[_points.Count() - 1]));
                _growing = true;
                // _points = _body.Points;
                _eating = false;
            }

            // wenn argv = 1 dann ist eine Schleife durch
            if (argv == 1)
            {
                _Merker = true;
                // _points = _body.Points;
                CheckFruitCollision();

                if (_growing == true)
                    _growing = false;

                if (IsGameOver())
                {
                    _controller.OnGameFinished();
                }
                else
                {
                    _AltDirection = _direction;
                    _direction = _directionCache;
                    if(_direction != _directionCache)
                    {
                        _RoatationFinished = false;
                    }
                }
            }
        }

        if (argv != 1)
            _Merker = false;
    }



    protected virtual void CheckFruitCollision()
    {
        if (_body.Points[0] == _fruit.Position)
        {
            _tween.StopAll();
            _audioPlayer.Play();
            _eating = true;
            _fruit.SetNewPosition(_fruit.RandomizePosition());            
            //IncreaseSpeed();
            _controller.UpdateScore();
            GD.Print($"{Name} hat Frucht gefressen!");
            MoveSnake();
        }
    }


    protected void IncreaseSpeed()
    {
        moveDelay *= 0.95f;
        GD.Print(moveDelay.ToString());
    }

    protected virtual bool IsGameOver()
    {
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[0] == obstacle.RectGlobalPosition)
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

        if (_points.Count >= 3)
        {
            for (int i = 1; i < _points.Count; i++)
            {
                if (_points[0] == _points[i])
                {
                    _controller.LoseMessage = $"Game Over fuer {Name}. Hat sich selbst gefressen!";
                    return true;
                }
            }
        }
        return false;
    }
}