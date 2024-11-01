using Godot;
using System;
using System.Linq;

public abstract class BaseSnake : Node2D
{
    protected Vector2 _direction = Vector2.Right;
    protected Vector2 _directionCache;
    protected AudioStreamPlayer2D _audioPlayer;
    protected Vector2[] _points;
    protected Line2D _body;
    protected Node2D _face;
    protected Tween _tween;
    protected Fruit _fruit;
    protected GameController _controller;
    protected BaseSnake _otherSnake;

    protected int _gridSize = 32;
    public float MoveDelay;
    protected bool _eating = false;
    protected bool _growing = false;
    protected bool _isPlayerOne;
    protected bool _merker = false;

    protected bool _isServer;
    protected bool _isSnake1;

    public Vector2[] Points { get { return _points; } }

    public override void _Ready()
    {
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();
        _audioPlayer = GetNode<AudioStreamPlayer2D>("Eating");
        _body = GetNode<Line2D>("Body");
        _points = _body.Points;
        _face = GetNode<Node2D>("Face");
        _tween = GetNode<Tween>("Tween");

        _directionCache = _direction;
    }

    public abstract void SetPlayerSettings(bool isServer, bool isSnake1, BaseSnake otherSnake);
    public virtual void SetPlayerSettings(bool isServer){}

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
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, MoveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
    }

    protected virtual void MoveTween(float argv)
    {
        if (_merker == false)
        {
            int i = 0;
            foreach (Vector2 pos in _body.Points)
            {
                Vector2 newPos, diff = Vector2.Zero;
                if (i == 0)
                    newPos = _points[i] + _direction * new Vector2(_gridSize * argv, _gridSize * argv);
                else
                {
                    if (!(_growing == true && i == _body.Points.Count() - 1))
                    {
                        diff = Vector2.Zero;
                        if (_points[i - 1].x - _points[i].x != 0)
                            diff.x = (_points[i - 1].x - _points[i].x) / _gridSize;
                        if (_points[i - 1].y - _points[i].y != 0)
                            diff.y = (_points[i - 1].y - _points[i].y) / _gridSize;

                        newPos = _points[i] + diff * new Vector2(_gridSize * argv, _gridSize * argv);
                    }
                    else
                    {
                        newPos = _body.GetPointPosition(i);
                    }
                }
                _body.SetPointPosition(i, newPos);
                i++;
            }

            if (_eating == true)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count() - 1));
                _growing = true;
                _points = _body.Points;
                _eating = false;
            }

            _face.Position = _body.Points[0];
            _face.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));

            // wenn argv = 1 dann ist eine Schleife durch
            if (argv == 1)
            {
                _merker = true;
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
                    _direction = _directionCache;
                }
            }
        }

        if (argv != 1)
            _merker = false;
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
        MoveDelay *= 0.95f;
        GD.Print(MoveDelay.ToString());
    }

    protected virtual bool IsGameOver()
    {
        //Hinderniskollision
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[0] == obstacle)
            {
                _controller.LoseMessage = ($"Game Over fuer {Name}.\nHat ein Hindernis getroffen!");
                return true;
            }
        }

        //Kollision mit Gegner
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

        //Selbstkollision
        if (_points.Length >= 3)
        {
            for (int i = 1; i < _points.Length; i++)
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

